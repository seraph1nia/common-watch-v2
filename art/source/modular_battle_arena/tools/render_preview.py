#!/usr/bin/env python3
"""Offline composition check. These images are NOT Godot captures or legal match states."""
from __future__ import annotations
import argparse, math
from pathlib import Path
from PIL import Image, ImageDraw, ImageFont

ROOT=Path(__file__).resolve().parents[1]
ASSETS=ROOT/'assets'

def font(size:int):
    for p in ['/usr/share/fonts/truetype/dejavu/DejaVuSans.ttf','/usr/share/fonts/truetype/liberation2/LiberationSans-Regular.ttf']:
        if Path(p).exists(): return ImageFont.truetype(p,size)
    return ImageFont.load_default()

def open_rgba(relative:str): return Image.open(ASSETS/relative).convert('RGBA')

def compose(count:int, columns:int=7, *, grid:bool=True, structures:bool=True):
    if not 1<=count<=6 or columns not in (6,7): raise ValueError('Use 1..6 sectors and 6 or 7 columns.')
    pitch=1024;capw=352;H=1536;compression=.72;rows=9
    r=pitch/(columns*math.sqrt(3));hexw=math.sqrt(3)*r
    width=round(2*capw+count*pitch+hexw*.5)
    canvas=Image.new('RGBA',(width,H),(14,16,23,255))
    variants=[0,1,2,1,0,2]
    # Full rectangular plates continue past the last cell center to cover odd-row stagger.
    for i in range(-1,count+1):
        im=open_rgba(f'assembled/arena_mid_{variants[i%len(variants)]+1:02d}.png')
        canvas.alpha_composite(im,(capw+i*pitch,0))
    # Crop right overscan before decorative cap. No walls between sectors.
    xend=round(capw+count*pitch+hexw*.5)
    # Continue the same ground/sky beneath the alpha endcaps; never leave a black gutter.
    l=open_rgba('edges/arena_left_cap.png'); rr=open_rgba('edges/arena_right_cap.png')
    canvas.alpha_composite(l,(0,128));canvas.alpha_composite(rr,(xend,128))
    top=370 if columns==7 else 276  # requested 6x9 view needs a taller ground footprint
    def point(col,row):
        return capw+hexw/2+(col+.5*(row&1))*hexw, top+r*compression+row*1.5*r*compression
    if grid:
        lines=Image.new('RGBA',canvas.size);d=ImageDraw.Draw(lines);edges=set()
        for row in range(rows):
            for col in range(count*columns):
                x,y=point(col,row)
                corners=[(round(x+r*math.cos(math.radians(60*k-30)),3),round(y+compression*r*math.sin(math.radians(60*k-30)),3)) for k in range(6)]
                for j in range(6):
                    a,b=corners[j],corners[(j+1)%6];edges.add(tuple(sorted([a,b])))
        for a,b in edges: d.line((a,b),fill=(182,193,200,106),width=2)
        canvas=Image.alpha_composite(canvas,lines)
    if structures:
        objects=[]
        for sec in range(count):
            objects.extend([('keep',sec*columns+columns//2,8,310),('defense_tower',sec*columns,5,270),('defense_tower',sec*columns+columns-1,5,270)])
        for name,col,row,size in sorted(objects,key=lambda a:point(a[1],a[2])[1]):
            p=point(col,row);im=open_rgba(f'structures/{name}.png').resize((size,size),Image.Resampling.LANCZOS)
            canvas.alpha_composite(im,(round(p[0]-.5*size),round(p[1]-(430/512)*size)))
    return canvas.convert('RGB')

def labelled(im:Image.Image,title:str,subtitle:str,width:int):
    im=im.copy();im.thumbnail((width,1600),Image.Resampling.LANCZOS)
    result=Image.new('RGB',(im.width,im.height+106),(13,17,23))
    d=ImageDraw.Draw(result);d.text((24,14),title,font=font(24),fill=(228,231,228))
    d.text((24,50),subtitle,font=font(15),fill=(160,177,187));result.paste(im,(0,90))
    return result

def main():
    p=argparse.ArgumentParser(description=__doc__)
    p.add_argument('--segments',type=int,choices=range(1,7),default=3)
    p.add_argument('--columns',type=int,choices=(6,7),default=7)
    p.add_argument('--width',type=int,default=1800)
    p.add_argument('--no-grid',action='store_true')
    p.add_argument('--output',type=Path)
    a=p.parse_args()
    if not 400<=a.width<=8192: p.error('width must be in 400..8192')
    out=a.output or ROOT/'previews'/f'arena_{a.segments}_segments_{a.columns}x9.png'
    out.parent.mkdir(parents=True,exist_ok=True)
    im=compose(a.segments,a.columns,grid=not a.no_grid)
    title=f'RAIN / EMBER  |  {a.segments} SECTOR'+('S' if a.segments!=1 else '')+f'  |  {a.columns} x 9 PER SECTOR'
    subtitle='OFFLINE ART COMPOSITION - not a Godot screenshot. Grid and structures are separate overlays.'
    labelled(im,title,subtitle,a.width).save(out,optimize=True)
    print(out)
if __name__=='__main__':main()
