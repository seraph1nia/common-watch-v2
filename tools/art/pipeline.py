"""Deterministic PNG crops -> staged catalog -> installed assets. Never reads archives."""
from __future__ import annotations

import argparse
import hashlib
import json
import math
from pathlib import Path
import shutil

from PIL import Image, ImageChops, ImageDraw, ImageFilter

ROOT = Path(__file__).resolve().parents[2]
STAGE = ROOT / 'art/processed'


def bounded_path(root: Path, relative: str) -> Path:
    path = (root / relative).resolve()
    if Path(relative).is_absolute() or not path.is_relative_to(root.resolve()):
        raise ValueError(f'Path must remain inside {root}: {relative}')
    return path


def slice_margins(size, margins):
    if set(margins) != {'left', 'top', 'right', 'bottom'}:
        raise ValueError('Four named slice margins required')
    l, t, r, b = (margins[k] for k in ('left', 'top', 'right', 'bottom'))
    if any(type(v) is not int or v <= 0 for v in (l, t, r, b)) or l+r >= size[0] or t+b >= size[1]:
        raise ValueError('Slice margins must leave a positive center')
    return l, t, r, b


def alpha_stats(image):
    a = image.getchannel('A')
    hist = a.histogram()
    return {'extrema': list(a.getextrema()), 'visible_bbox': a.getbbox(),
            'transparent_pixels': hist[0], 'opaque_pixels': hist[255],
            'partial_pixels': sum(hist[1:255])}


def clean_alpha(image, settings, entry):
    """Preserve foreground color and soft alpha. Optional explicit mask or edge flood only."""
    image = image.convert('RGBA')
    alpha = image.getchannel('A')
    floor, ceiling = settings.get('alpha_floor', 0), settings.get('alpha_ceiling', 255)
    if not 0 <= floor < ceiling <= 255:
        raise ValueError('Invalid alpha noise limits')
    alpha = alpha.point([0 if a <= floor else 255 if a >= ceiling else a for a in range(256)])
    if 'mask_polygon' in entry:
        mask = Image.new('L', image.size)
        ImageDraw.Draw(mask).polygon([tuple(p) for p in entry['mask_polygon']], fill=255)
        feather = entry.get('mask_feather', 0)
        if not 0 <= feather <= 2:
            raise ValueError('Mask feather must be between zero and two pixels')
        if feather:
            mask = mask.filter(ImageFilter.GaussianBlur(feather))
        alpha = ImageChops.multiply(alpha, mask)
    # An opt-in RGB cleanup floods only the boundary-connected, near-identical background.
    # No global foreground color test, and never enabled for the supplied painted sheets.
    if 'boundary_color' in entry:
        color = entry['boundary_color']
        tolerance = entry.get('boundary_tolerance', 8)
        if len(color) != 3 or not 0 <= tolerance <= 24:
            raise ValueError('Conservative boundary cleanup requires RGB and tolerance 0..24')
        rgb = image.load(); ap = alpha.load(); w, h = image.size
        pending = [(x, y) for x in range(w) for y in (0, h-1)] + [(x, y) for y in range(h) for x in (0, w-1)]
        seen = set()
        while pending:
            x, y = pending.pop()
            if (x, y) in seen or not (0 <= x < w and 0 <= y < h):
                continue
            seen.add((x, y))
            if max(abs(rgb[x, y][c] - color[c]) for c in range(3)) > tolerance:
                continue
            ap[x, y] = 0
            pending.extend(((x-1,y),(x+1,y),(x,y-1),(x,y+1)))
    image.putalpha(alpha)
    return image


def process_image(source, entry, settings):
    crop = entry['crop']
    if len(crop) != 4 or any(type(v) is not int for v in crop):
        raise ValueError('Crop must contain four integer coordinates')
    l, t, r, b = crop
    if not 0 <= l < r <= source.width or not 0 <= t < b <= source.height:
        raise ValueError('Crop lies outside the source or is empty')
    cropped = source.crop(crop).convert('RGBA')
    initial = alpha_stats(cropped)
    image = clean_alpha(cropped, settings, entry)
    bbox = image.getchannel('A').getbbox()
    if bbox is None:
        raise ValueError('Crop has no visible pixels')
    if 'slice' in entry:
        slice_margins(image.size, entry['slice'])
        scale = entry.get('border_scale', 1)
        if not math.isfinite(scale) or not 0 < scale <= 4:
            raise ValueError('Invalid HUD border scale')
        # Keep the crop intact: trimming would invalidate the authored slice coordinates.
        return image, {'source_alpha': initial, 'content_bbox': bbox, 'resize_scale': 1}
    canvas = settings['canvases'][entry['id'].split('/')[0]]
    if len(canvas) != 2 or any(type(v) is not int or v < 1 or v > 4096 for v in canvas):
        raise ValueError('Invalid canvas')
    anchor = entry.get('anchor', [.5, .92])
    if len(anchor) != 2 or any(not math.isfinite(v) or not 0 <= v <= 1 for v in anchor):
        raise ValueError('Normalized anchor required')
    padding = settings.get('padding', 8)
    if type(padding) is not int or padding < 0 or padding*2 >= min(canvas):
        raise ValueError('Invalid padding')
    content = image.crop(bbox)
    # The bottom of world objects is their ground anchor; centered terrain/icons use center.
    content_anchor = entry.get('content_anchor', [.5, 1 if anchor[1] > .5 else .5])
    if len(content_anchor) != 2 or any(not math.isfinite(v) or not 0 <= v <= 1 for v in content_anchor):
        raise ValueError('Invalid content anchor')
    limits = []
    for extent, total, target, pivot in zip(content.size, canvas, anchor, content_anchor):
        if pivot > 0:
            limits.append((total*target-padding)/(extent*pivot))
        if pivot < 1:
            limits.append((total*(1-target)-padding)/(extent*(1-pivot)))
    scale = min(limits)
    if scale <= 0:
        raise ValueError('Anchor leaves no room for content')
    size = tuple(max(1, round(v*scale)) for v in content.size)
    content = content.resize(size, Image.Resampling.LANCZOS)
    offset = tuple(round(total*target - extent*pivot) for total, target, extent, pivot in zip(canvas, anchor, size, content_anchor))
    output = Image.new('RGBA', tuple(canvas))
    output.alpha_composite(content, offset)
    return output, {'source_alpha': initial, 'content_bbox': bbox, 'resize_scale': scale,
                    'content_size': size, 'content_offset': offset, 'content_anchor': content_anchor}


def read_manifests():
    extraction = json.loads((ROOT/'art/manifests/extraction.json').read_text())
    settings = json.loads((ROOT/'art/manifests/processing.json').read_text())
    if extraction['version'] != 1 or settings['version'] != 1:
        raise ValueError('Unsupported manifest version')
    ids, paths = set(), set()
    for e in extraction['assets']:
        parts = e['id'].split('/')
        if len(parts) != 2 or parts[0] not in ('building','unit','terrain','resource','hud') or not all(c.islower() or c.isdigit() or c == '_' for c in parts[1]):
            raise ValueError(f'Invalid asset ID: {e["id"]}')
        if e['id'] in ids or e['output'] in paths:
            raise ValueError('Duplicate asset ID or output')
        ids.add(e['id']); paths.add(e['output'])
        bounded_path(ROOT, e['source']); bounded_path(STAGE, e['output'])
        if not math.isfinite(e.get('display_size', 1)) or e.get('display_size', 1) <= 0:
            raise ValueError('Invalid display size')
    return extraction['assets'], settings


def process():
    entries, settings = read_manifests()
    records = []
    for e in entries:
        source_path = bounded_path(ROOT, e['source'])
        with Image.open(source_path) as source:
            image, info = process_image(source, e, settings)
            info['source_size'] = source.size
        target = bounded_path(STAGE, e['output'])
        target.parent.mkdir(parents=True, exist_ok=True)
        image.save(target, compress_level=9)
        record = dict(e, path=e['output'], size=image.size, alpha=alpha_stats(image), **info)
        record['source_sha256'] = hashlib.sha256(source_path.read_bytes()).hexdigest()
        record['sha256'] = hashlib.sha256(target.read_bytes()).hexdigest()
        records.append(record)
    (STAGE/'catalog.json').write_text(json.dumps({'version':1, 'assets':records}, indent=2)+'\n')
    previews(records)
    print(f'Processed {len(records)} supplied illustrations into {STAGE.relative_to(ROOT)}')


def validate(base=STAGE):
    entries, _ = read_manifests()
    catalog = json.loads((base/'catalog.json').read_text())
    if {e['id'] for e in entries} != {e['id'] for e in catalog['assets']}:
        raise ValueError('Catalog does not match source manifest')
    for e in catalog['assets']:
        path = bounded_path(base, e['path'])
        if hashlib.sha256(path.read_bytes()).hexdigest() != e['sha256']:
            raise ValueError(f'Output changed: {path}')
        source = bounded_path(ROOT, e['source'])
        if hashlib.sha256(source.read_bytes()).hexdigest() != e['source_sha256']:
            raise ValueError('Source changed: process again')
        with Image.open(path) as image:
            image.load()
            if image.mode != 'RGBA' or list(image.size) != e['size'] or image.getchannel('A').getbbox() is None:
                raise ValueError(f'Invalid RGBA image: {path}')
            if 'slice' in e:
                slice_margins(image.size, e['slice'])
    print(f'Validated {len(catalog["assets"])} RGBA assets, hashes, dimensions and slice margins')


def install():
    validate()
    catalog = json.loads((STAGE/'catalog.json').read_text())
    for e in catalog['assets']:
        target = bounded_path(ROOT/'assets', e['path'])
        target.parent.mkdir(parents=True, exist_ok=True)
        shutil.copyfile(STAGE/e['path'], target)
    shutil.copyfile(STAGE/'catalog.json', ROOT/'assets/catalog.json')
    validate(ROOT/'assets')
    print('Installed PNGs and filesystem catalog under assets/')


def nine_slice(image, margins, size, border_scale=1):
    l, t, r, b = slice_margins(image.size, margins)
    dl, dt, dr, db = (round(v*border_scale) for v in (l,t,r,b))
    if size[0] <= dl+dr or size[1] <= dt+db:
        raise ValueError('Destination smaller than fixed corners')
    sx, sy = [0,l,image.width-r,image.width], [0,t,image.height-b,image.height]
    dx, dy = [0,dl,size[0]-dr,size[0]], [0,dt,size[1]-db,size[1]]
    result = Image.new('RGBA', size)
    for y in range(3):
        for x in range(3):
            piece = image.crop((sx[x],sy[y],sx[x+1],sy[y+1]))
            piece = piece.resize((dx[x+1]-dx[x],dy[y+1]-dy[y]),Image.Resampling.LANCZOS)
            result.alpha_composite(piece,(dx[x],dy[y]))
    return result


def previews(records):
    dest = ROOT/'art/previews'; dest.mkdir(exist_ok=True)
    sheet = Image.new('RGB',(1040, math.ceil(len(records)/4)*240),(25,36,46)); draw = ImageDraw.Draw(sheet)
    for i,e in enumerate(records):
        x,y = (i%4)*260,(i//4)*240
        with Image.open(STAGE/e['path']) as image:
            image.thumbnail((230,190))
            sheet.paste(image,(x+(260-image.width)//2,y+20),image)
        draw.text((x+10,y+216),e['id'],fill='white')
    sheet.save(dest/'contact-sheet.png')
    sheet=Image.new('RGB',(1260,940),(25,36,46));draw=ImageDraw.Draw(sheet)
    tests=[('hud/button',(160,48),(24,40),'Dynamic button'),
           ('hud/panel',(480,160),(24,150),'Gold 12   Wood 3   Stone 2'),
           ('hud/panel',(640,420),(24,370),'Building inspector / dynamic text'),
           ('hud/card_frame',(260,390),(800,40),'Reward / dynamic text'),
           ('hud/tooltip',(330,100),(800,560),'Inspect a plot')]
    for identifier,size,position,label in tests:
        e=next(r for r in records if r['id']==identifier)
        with Image.open(STAGE/e['path']) as source:
            patch=nine_slice(source,e['slice'],size,e['border_scale'])
        sheet.paste(patch,position,patch)
        draw.text((position[0]+35,position[1]+size[1]//2),label,fill='white' if identifier=='hud/button' else (40,29,20))
        draw.text((position[0],position[1]-18),f'{identifier} {size[0]} x {size[1]}',fill='white')
    sheet.save(dest/'nine-slice-preview.png')


def main():
    parser=argparse.ArgumentParser(description=__doc__)
    parser.add_argument('operation',choices=['process','validate','install','preview'])
    args=parser.parse_args()
    if args.operation=='process': process()
    elif args.operation=='validate': validate()
    elif args.operation=='install': install()
    else: previews(json.loads((STAGE/'catalog.json').read_text())['assets'])

if __name__ == '__main__': main()
