import io
import sys
import unittest
from pathlib import Path
from PIL import Image, ImageDraw

sys.path.insert(0, str(Path(__file__).resolve().parents[2]/'tools/art'))
from pipeline import process_image, slice_margins, nine_slice, bounded_path, read_manifests


class ProcessingTests(unittest.TestCase):
    def setUp(self):
        self.settings={'canvases':{'unit':[256,256]},'padding':8,'alpha_floor':2,'alpha_ceiling':250}
        self.entry={'id':'unit/test','crop':[0,0,80,60],'anchor':[.5,.92]}
        self.source=Image.new('RGBA',(80,60))
        ImageDraw.Draw(self.source).rectangle((10,10,49,29),fill=(100,150,200,255))

    def test_explicit_crop_and_visible_bounds(self):
        self.entry['crop']=[5,5,60,40]
        output,metadata=process_image(self.source,self.entry,self.settings)
        self.assertEqual((5,5,45,25),metadata['content_bbox'])
        self.assertEqual((256,256),output.size)
        self.assertEqual('RGBA',output.mode)

    def test_rgb_is_converted_to_rgba(self):
        output,_=process_image(Image.new('RGB',(80,60),'red'),self.entry,self.settings)
        self.assertEqual('RGBA',output.mode)
        self.assertEqual((0,255),output.getchannel('A').getextrema())

    def test_transparent_padding_aspect_ratio_and_ground_anchor(self):
        output,metadata=process_image(self.source,self.entry,self.settings)
        self.assertEqual((240,120),metadata['content_size'])
        bbox=output.getchannel('A').getbbox()
        self.assertEqual(128,(bbox[0]+bbox[2])/2)
        self.assertAlmostEqual(256*.92,bbox[3],delta=1)
        self.assertEqual(0,output.getpixel((0,0))[3])

    def test_centered_canvas(self):
        self.entry['anchor']=[.5,.5]
        output,_=process_image(self.source,self.entry,self.settings)
        bbox=output.getchannel('A').getbbox()
        self.assertEqual(128,(bbox[1]+bbox[3])/2)

    def test_alpha_cleanup_preserves_foreground_and_soft_edges(self):
        self.source.putpixel((10,10),(1,1,1,130))
        self.source.putpixel((0,0),(200,200,200,1))
        self.entry['slice']={'left':5,'top':5,'right':5,'bottom':5}
        output,_=process_image(self.source,self.entry,self.settings)
        self.assertEqual((1,1,1,130),output.getpixel((10,10)))
        self.assertEqual(0,output.getpixel((0,0))[3])
        self.assertEqual((80,60),output.size)

    def test_boundary_cleanup_only_removes_connected_background(self):
        source=Image.new('RGB',(80,60),'white')
        ImageDraw.Draw(source).rectangle((10,10,50,40),fill='black')
        ImageDraw.Draw(source).rectangle((20,20,30,30),fill='white')
        self.entry.update(boundary_color=[255,255,255],slice={'left':5,'top':5,'right':5,'bottom':5})
        output,_=process_image(source,self.entry,self.settings)
        self.assertEqual(0,output.getpixel((0,0))[3])
        self.assertEqual(255,output.getpixel((25,25))[3])
        self.assertEqual(255,output.getpixel((15,15))[3])

    def test_explicit_mask_is_local(self):
        self.entry.update(mask_polygon=[[5,5],[60,5],[60,40],[5,40]],mask_feather=1)
        output,_=process_image(self.source,self.entry,self.settings)
        self.assertIsNotNone(output.getchannel('A').getbbox())

    def test_invalid_crops_rejected(self):
        for crop in [[-1,0,80,60],[0,0,81,60],[0,0,0,60],[0,0,80,61],[0,0,1.2,30]]:
            with self.subTest(crop=crop),self.assertRaises(ValueError):
                process_image(self.source,dict(self.entry,crop=crop),self.settings)

    def test_invalid_anchor_padding_and_empty_crop_rejected(self):
        with self.assertRaises(ValueError): process_image(self.source,dict(self.entry,anchor=[2,.5]),self.settings)
        with self.assertRaises(ValueError): process_image(self.source,self.entry,dict(self.settings,padding=128))
        with self.assertRaises(ValueError): process_image(Image.new('RGBA',(80,60)),self.entry,self.settings)

    def test_deterministic_png_bytes(self):
        def encoded():
            output,_=process_image(self.source,self.entry,self.settings)
            data=io.BytesIO();output.save(data,format='PNG',compress_level=9);return data.getvalue()
        self.assertEqual(encoded(),encoded())

    def test_ground_uv_tracks_trimming_padding_and_resize(self):
        self.settings['canvases']['terrain']=[256,256]
        entry=dict(self.entry,id='terrain/test',anchor=[.5,.5],
                   ground_polygon=[[45,12],[45,27],[30,28],[12,27],[12,12],[30,11]])
        output,metadata=process_image(self.source,entry,self.settings)
        for point,uv in zip(entry['ground_polygon'],metadata['ground_uv']):
            x,y=(round(v*256) for v in uv)
            self.assertEqual(255,output.getpixel((x,y))[3])
        with self.assertRaises(ValueError):
            process_image(self.source,dict(entry,ground_polygon=[[0,0]]),self.settings)
        with self.assertRaises(ValueError):
            process_image(self.source,dict(entry,id='unit/test'),self.settings)

    def test_path_escape_rejected(self):
        with self.assertRaises(ValueError): bounded_path(Path('/tmp/catalog'),'../escape.png')
        with self.assertRaises(ValueError): bounded_path(Path('/tmp/catalog'),'/etc/passwd')

    def test_installed_manifest_ids_are_complete(self):
        entries,_=read_manifests()
        original={'building/arcanum','building/lumbermill','building/farm','building/barracks','unit/swordsman','unit/enemy_swordsman','terrain/woodland_hex','resource/gold','resource/wood','resource/stone','hud/panel','hud/card_frame','hud/button','hud/tooltip'}
        detailed={'building/'+name for name in ['mine','metalmine','archeryrange','researchtower','arrowtower','catapulttower','stonecutter','weaver','market','townhall']}
        detailed.update('unit/'+name for name in ['berserker','crossbowman','mage','enemy_crossbowman','enemy_mage'])
        self.assertEqual(original | detailed, {e['id'] for e in entries})
        self.assertEqual(29,len(entries))

    def test_authored_ground_points_survive_trim_and_resize(self):
        entries,settings=read_manifests()
        root=Path(__file__).resolve().parents[2]
        for entry in entries:
            if 'ground_point' not in entry:
                continue
            with self.subTest(asset=entry['id']), Image.open(root/entry['source']) as source:
                output,metadata=process_image(source,entry,settings)
                bbox=metadata['content_bbox']
                for axis in range(2):
                    transformed=(entry['ground_point'][axis]-bbox[axis])*metadata['content_size'][axis]/(bbox[axis+2]-bbox[axis])+metadata['content_offset'][axis]
                    self.assertAlmostEqual(output.size[axis]*entry['anchor'][axis],transformed,delta=1)
                visible=output.getchannel('A').getbbox()
                self.assertGreater(visible[0],0)
                self.assertGreater(visible[1],0)
                self.assertLess(visible[2],output.width)
                self.assertLess(visible[3],output.height)


class NineSliceTests(unittest.TestCase):
    def test_margin_validation(self):
        for margins in [{'left':0,'top':3,'right':3,'bottom':3}, {'left':8,'top':3,'right':4,'bottom':3}, {'left':3,'top':3,'right':3}, {'left':3.2,'top':3,'right':3,'bottom':3}]:
            with self.subTest(margins=margins),self.assertRaises(ValueError): slice_margins((12,12),margins)

    def test_corners_preserved_at_multiple_sizes(self):
        source=Image.new('RGBA',(12,12),'gray')
        draw=ImageDraw.Draw(source)
        for rect,color in [((0,0,2,2),'red'),((9,0,11,2),'blue'),((0,9,2,11),'green'),((9,9,11,11),'yellow')]: draw.rectangle(rect,fill=color)
        margins=dict(left=3,top=3,right=3,bottom=3)
        for size in [(160,48),(480,160),(640,420),(260,390)]:
            output=nine_slice(source,margins,size)
            self.assertEqual(source.crop((0,0,3,3)).tobytes(),output.crop((0,0,3,3)).tobytes())
            self.assertEqual(source.getpixel((11,11)),output.getpixel((size[0]-1,size[1]-1)))
            self.assertEqual(255,output.getchannel('A').getextrema()[0])
        with self.assertRaises(ValueError): nine_slice(source,margins,(5,5))

if __name__=='__main__': unittest.main()
