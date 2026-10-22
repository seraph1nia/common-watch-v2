"""Technical exports of authored HUD paintings; never reads reference crops.

Run with the pinned .venv-art Python. Masters remain byte-for-byte originals.
States share alpha and geometry. This is export processing, not a painting tool.
"""
from pathlib import Path
import hashlib
import json
import shutil
import xml.etree.ElementTree as ET
from PIL import Image, ImageEnhance, ImageOps, ImageDraw, ImageFilter

ROOT = Path(__file__).resolve().parents[2]
SOURCE = ROOT / 'art/source/painted_forest_hud'
RUNTIME = ROOT / 'src/Game.Godot/UI/PaintedForestHud/textures'


def digest(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


def clean(image):
    image = image.convert('RGBA')
    alpha = image.getchannel('A').point(lambda a: 0 if a < 12 else 255 if a > 245 else a)
    image.putalpha(alpha)
    image = image.crop(alpha.getbbox())
    return ImageOps.expand(image, border=16, fill=(0, 0, 0, 0))


def export():
    RUNTIME.mkdir(parents=True, exist_ok=True)
    records = []

    def save(identifier, image, source, *, slices=None, content=None, anchor=None, state='master', family=None):
        image = ImageOps.expand(image, border=4, fill=(0, 0, 0, 0))
        target = RUNTIME / (identifier + '.png')
        image.save(target)
        alpha = image.getchannel('A')
        bbox = alpha.getbbox()
        records.append(dict(id=identifier, role=family or identifier, source=str(source.relative_to(ROOT)),
            runtime=str(target.relative_to(ROOT)), source_dimensions=list(Image.open(source).size), dimensions=list(image.size),
            transparent_padding=[bbox[0], bbox[1], image.width-bbox[2], image.height-bbox[3]],
            nine_slice=slices, native_content_margins=content, attachment_anchor=anchor,
            state=state, registration=family,
            intended_display_range={'minimum': [44, 44], 'maximum': [180, 180]} if anchor else
                {'minimum': [176, 104], 'maximum': [400, 160]} if family == 'plaque' else
                {'minimum': [44, 44], 'maximum': [500, 180]} if family else
                {'minimum': [180, 96], 'maximum': [1800, 1044]},
            source_sha256=digest(source), sha256=digest(target),
            provenance='Technical soft light authored by tools/art/painted_hud.py; lantern master is its style reference' if identifier == 'lantern-glow'
                else 'OpenAI built-in image_gen, user-authorized HUD-only painting; deterministic Pillow export'))

    for source in sorted((SOURCE / 'masters').glob('*.png')):
        name = source.stem
        image = clean(Image.open(source))
        if name == 'panel':
            # Fixed corners; only bark rails and quiet aperture stretch.
            for identifier, size, margins in [('panel', (320, 200), [40, 40, 40, 40]),
                    ('inset', (256, 160), [32, 32, 32, 32]), ('tooltip', (256, 160), [32, 32, 32, 32]),
                    ('portrait', (192, 192), [32, 32, 32, 32])]:
                save(identifier, image.resize(size, Image.Resampling.LANCZOS), source,
                     slices=margins, content=[36, 44, 36, 32])
            save('interior', image.crop((image.width//3, image.height//3, 2*image.width//3, 2*image.height//3)), source)
        elif name in ('ready', 'pause', 'plaque', 'tab', 'details', 'menu'):
            size = (220, 96) if name == 'plaque' else (144, 48)
            image = image.resize(size, Image.Resampling.LANCZOS)
            for state, light, saturation in [('normal', 1, 1), ('hover', 1.16, 1.08), ('pressed', .80, 1.12), ('disabled', .65, .35)]:
                if name == 'plaque' and state == 'disabled':
                    light = .95
                rgb = ImageEnhance.Color(image.convert('RGB')).enhance(saturation)
                rgb = ImageEnhance.Brightness(rgb).enhance(light).convert('RGBA')
                rgb.putalpha(image.getchannel('A'))
                save(name+'_'+state, rgb, source, slices=[28, 16, 28, 16] if name == 'plaque' else [22, 16, 22, 16], content=[12, 6, 12, 6], state=state, family=name)
        else:
            image.thumbnail((512, 512), Image.Resampling.LANCZOS)
            save(name, image, source, anchor=[.50, .95] if name in ('owl', 'rabbit', 'squirrel') else [.5, .9])
            if name == 'sprig':
                save('separator', image, source, anchor=[.5, .5])
            if name == 'lantern':
                # Separate soft technical light for the authored glass; no painted geometry.
                light = Image.new('RGBA', (256, 256), (255, 161, 47, 0))
                mask = Image.new('L', light.size); painter = ImageDraw.Draw(mask)
                painter.ellipse((82, 70, 174, 186), fill=115)
                mask = mask.filter(ImageFilter.GaussianBlur(24)); light.putalpha(mask)
                save('lantern-glow', light, source, anchor=[.5, .5])

    for source in sorted((SOURCE / 'vectors').glob('*.svg')):
        target = RUNTIME / source.name; shutil.copyfile(source, target)
        root = ET.parse(source).getroot()
        records.append(dict(id=source.stem, role='independent keyboard focus' if source.stem == 'focus' else 'authoritative phase marker',
            source=str(source.relative_to(ROOT)), runtime=str(target.relative_to(ROOT)),
            dimensions=[int(root.attrib['width']), int(root.attrib['height'])],
            transparent_padding=[2]*4 if source.stem in ('focus', 'phase-active') else [5]*4,
            nine_slice=[22]*4 if source.stem == 'focus' else None, native_content_margins=[0]*4,
            attachment_anchor=None, state=source.stem.removeprefix('phase-'), registration='phase' if source.stem.startswith('phase-') else 'focus',
            intended_display_range=[22, 48], source_sha256=digest(source), sha256=digest(target), provenance='Authored technical SVG geometry, GPL-3.0-only'))

    (SOURCE / 'manifest.json').write_text(json.dumps(dict(version=1, assets=records,
        runtime_usages={
            'ledger_inspector_phase': {'frame': 'panel', 'native_content_margins': [36, 44, 36, 32]},
            'construction_palette': {'frame': 'panel', 'native_content_margins': [20, 20, 20, 22]},
            'construction_card': {'frame': 'plaque', 'native_content_margins': [10, 20, 10, 18], 'thumbnail_box': [40, 52], 'portrait_frame': '40x40 fixed aspect', 'thumbnail': '32x32 fixed aspect'},
            'gold_recovery': {'frame': 'plaque', 'native_content_margins': [12, 6, 12, 6], 'minimum_height': 92},
            'perch_anchors': {'owl': [.5, .95], 'rabbit': [.5, .95], 'squirrel': [.5, .95]},
            'decoration_scaling': 'Uniform aspect-preserving fit; 1.25x above 1600 viewport pixels. Decorations never participate in Container minimum sizes.'},
        layered_sources=False, source_note='Original generated RGBA paintings; no editable layers were produced.'), indent=2)+'\n')
    # Contact sheet shows actual exports over alternating light and dark surfaces.
    width, cellw, cellh = 1200, 240, 230
    sheet = Image.new('RGB', (width, ((len(records)+4)//5)*cellh), '#111b24')
    draw = ImageDraw.Draw(sheet)
    for i, record in enumerate(r for r in records if r['runtime'].endswith('.png')):
        x, y = i%5*cellw, i//5*cellh
        draw.rectangle((x, y, x+cellw, y+cellh), fill='#e0d9cc' if i%2 else '#111b24')
        preview = Image.open(ROOT / record['runtime']); preview.thumbnail((220, 184), Image.Resampling.LANCZOS)
        sheet.paste(preview, (x+(cellw-preview.width)//2, y+8+(184-preview.height)//2), preview)
        draw.text((x+8, y+202), record['id'], fill='#1c191c' if i%2 else '#f2dfb8')
    sheet.save(SOURCE / 'contact-sheet.jpg', quality=92)
    print(f'Exported {len(records)} registered assets')


if __name__ == '__main__':
    export()
