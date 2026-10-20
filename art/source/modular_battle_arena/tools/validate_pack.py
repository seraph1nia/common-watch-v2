#!/usr/bin/env python3
"""Validate pack images and geometry assumptions; does not launch or test Godot."""
from __future__ import annotations
import argparse,hashlib,json,math
from pathlib import Path
from PIL import Image,ImageChops
ROOT=Path(__file__).resolve().parents[1]

def same(a,b):
    return ImageChops.difference(a,b).getbbox() is None

def verify():
    manifest=json.loads((ROOT/'metadata/assets.json').read_text())
    images={};failures=[]
    for e in manifest['assets']:
        p=(ROOT/e['path']).resolve()
        if not p.is_relative_to(ROOT.resolve()):raise ValueError('Escaping asset path')
        if not p.is_file(): failures.append('Missing '+e['path']);continue
        digest=hashlib.sha256(p.read_bytes()).hexdigest()
        if digest!=e['sha256']:failures.append('Hash differs: '+e['path'])
        with Image.open(p) as raw:
            raw.load()
            if list(raw.size)!=e['size'] or raw.mode!=e['mode']:failures.append('Dimensions/mode differ: '+e['path'])
            if raw.mode=='RGBA' and list(raw.getchannel('A').getextrema())!=e['alpha_extrema']:failures.append('Alpha differs: '+e['path'])
            images[e['path']]=raw.convert('RGBA')
    pairs=0
    for folder,prefix in [('ground','ground_repeat'),('backdrops','backdrop_repeat'),('assembled','arena_mid')]:
        family=[images[f'assets/{folder}/{prefix}_{i:02d}.png'] for i in range(1,4)]
        for ai,a in enumerate(family):
            for bi,b in enumerate(family):
                pairs+=1
                if not same(a.crop((a.width-1,0,a.width,a.height)),b.crop((0,0,1,b.height))):
                    failures.append(f'Seam mismatch {folder} {ai+1}->{bi+1}')
        if folder=='ground':
            for a in family:
                for b in family:
                    pairs+=1
                    if not same(a.crop((0,a.height-1,a.width,a.height)),b.crop((0,0,b.width,1))):failures.append('Ground y seam mismatch')
    a=images['assets/edges/foreground_repeat.png']
    if not same(a.crop((0,0,1,a.height)),a.crop((a.width-1,0,a.width,a.height))):failures.append('Foreground seam mismatch')
    cutout_checks=[]
    for name in ['keep','defense_tower']:
        a=images[f'assets/structures/{name}.png'];alpha=a.getchannel('A');bounds=alpha.getbbox()
        clear=all(alpha.getpixel(p)==0 for p in [(0,0),(511,0),(0,511),(511,511)])
        margin=bounds is not None and bounds[0]>0 and bounds[1]>0 and bounds[2]<512 and bounds[3]<512
        if not clear or not margin:failures.append('Structure canvas edge/padding: '+name)
        cutout_checks.append({'name':name,'clear_corners':clear,'nonzero_alpha_bbox':bounds,'clear_canvas_margin':margin})
    geometry=[]
    R=30.;C=.72;h=math.sqrt(3)*R
    for cols in (6,7):
        for n in range(1,7):
            positions=[(h*(col+.5*(row&1)),1.5*R*C*row) for row in range(9) for col in range(n*cols)]
            if len(set(positions))!=n*cols*9:failures.append('Duplicate projected cells')
            seams=0
            for sec in range(n-1):
                for row in range(9):
                    left=(sec+1)*cols-1;right=left+1
                    ql=left-(row-(row&1))//2;qr=right-(row-(row&1))//2
                    if qr-ql!=1:failures.append('Disconnected seam geometry')
                    seams+=1
            expected_width=(n*cols+.5)*h
            actual_width=max(p[0] for p in positions)-min(p[0] for p in positions)+h
            if not math.isclose(expected_width,actual_width,abs_tol=1e-8):failures.append('Bounding geometry mismatch')
            geometry.append({'columns':cols,'rows':9,'sectors':n,'cells':len(positions),'adjacent_cross_seam_pairs':seams,'pitch':cols*h})
    report={'status':'passed' if not failures else 'failed','runtime_png_count':len(manifest['assets']),
        'rgba_memory_all_assets_mib':round(sum(i.width*i.height*4 for i in images.values())/1024**2,2),
        'exact_boundary_pair_checks':pairs+1,'all_asset_hashes_dimensions_modes_checked':True,
        'structure_alpha_checks':cutout_checks,'geometry_cases':geometry,
        'not_executed':['Godot rendering','GPU sampler seam check','authoritative gameplay or legal match captures','multiplayer','runtime asset loader integration'],
        'limitations':['Boundary equality does not replace a visual GPU filtering/zoom test.','Previews are offline composites, not gameplay screenshots.'],
        'failures':failures}
    return report

def main():
    p=argparse.ArgumentParser(description=__doc__);p.add_argument('--write-report',action='store_true');a=p.parse_args()
    report=verify()
    if a.write_report:(ROOT/'metadata/validation_report.json').write_text(json.dumps(report,indent=2)+'\n')
    print(json.dumps(report,indent=2))
    if report['failures']:raise SystemExit(1)
if __name__=='__main__':main()
