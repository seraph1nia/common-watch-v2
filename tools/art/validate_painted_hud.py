"""Validate the additive painted delivery and registered state exports."""
import hashlib
import json
from pathlib import Path
import xml.etree.ElementTree as ET
from PIL import Image, ImageChops

ROOT = Path(__file__).resolve().parents[2]
SOURCE = ROOT / 'art/source/painted_forest_hud'


def validate():
    manifest = json.loads((SOURCE / 'manifest.json').read_text())
    assets = manifest['assets']; identifiers = {a['id'] for a in assets}
    required = {'panel', 'inset', 'portrait', 'tooltip', 'interior', 'separator', 'owl', 'rabbit', 'squirrel',
        'mushrooms-left', 'mushrooms-right', 'mushrooms-small', 'ivy', 'sprig', 'crystals', 'lantern', 'lantern-glow',
        'crescent', 'clock', 'badge', 'focus', 'phase-upcoming', 'phase-active', 'phase-complete'}
    families = ('ready', 'pause', 'plaque', 'tab', 'details', 'menu')
    required.update(f'{f}_{s}' for f in families for s in ('normal', 'hover', 'pressed', 'disabled'))
    assert required == identifiers, f'Missing/extra registered assets: {required ^ identifiers}'
    images = {}
    for asset in assets:
        for key, checksum in [('source', 'source_sha256'), ('runtime', 'sha256')]:
            path = (ROOT / asset[key]).resolve()
            assert path.is_relative_to(ROOT) and path.is_file(), f'Invalid path: {path}'
            assert hashlib.sha256(path.read_bytes()).hexdigest() == asset[checksum], f'Checksum: {path}'
        path = ROOT / asset['runtime']
        if path.suffix == '.svg':
            tree = ET.parse(path)
            assert all(node.tag.split('}')[-1] not in ('image', 'script', 'foreignObject') for node in tree.iter())
        else:
            image = Image.open(path); image.load()
            assert image.mode == 'RGBA' and list(image.size) == asset['dimensions'], f'Dimensions/RGBA: {path}'
            alpha = image.getchannel('A'); images[asset['id']] = image
            assert alpha.getextrema()[0] == 0, f'No transparency: {path}'
            if asset['id'] != 'interior':
                for edge in [(0, 0, image.width, 1), (0, image.height-1, image.width, image.height), (0, 0, 1, image.height), (image.width-1, 0, image.width, image.height)]:
                    assert alpha.crop(edge).getextrema()[1] <= 2, f'Clipped alpha at edge: {path}'
            if asset['attachment_anchor'] is not None:
                assert all(0 <= a <= 1 for a in asset['attachment_anchor'])
        if (m := asset['nine_slice']) is not None:
            w, h = asset['dimensions']; assert 0 < m[0]+m[2] < w and 0 < m[1]+m[3] < h
    for family in families:
        normal = images[family+'_normal']
        for state in ('hover', 'pressed', 'disabled'):
            other = images[family+'_'+state]
            assert normal.size == other.size and ImageChops.difference(normal.getchannel('A'), other.getchannel('A')).getbbox() is None, f'State registration: {family}/{state}'
            assert ImageChops.difference(normal.convert('RGB'), other.convert('RGB')).getbbox() is not None, f'Identical state: {family}/{state}'
    print(f'PAINTED_HUD_ASSETS_PASS {len(assets)} exports; RGBA, edges, hashes, vectors, anchors, slices, six registered state families')


if __name__ == '__main__':
    validate()
