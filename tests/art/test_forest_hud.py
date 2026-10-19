"""Validate the supplied HUD parts against their preserved delivery contract."""
import hashlib
import json
from pathlib import Path
import re
import unittest
import xml.etree.ElementTree as ET
from PIL import Image

ROOT = Path(__file__).resolve().parents[2]
SOURCE = ROOT / 'art/source/forest_hud'


class ForestHudTests(unittest.TestCase):
    def test_supplied_resources_and_editable_sources_are_unchanged(self):
        checked = 0
        for line in (SOURCE / 'SHA256SUMS.txt').read_text().splitlines():
            digest, name = line.split('  ', 1)
            if name.startswith('src/'):
                path = ROOT / name
            elif name.startswith(('source/', 'metadata/')) or name in ('README.md', 'HUD_LOGIC.md', 'PROVENANCE.md'):
                path = SOURCE / name
            else:
                continue  # Mockups and optional authoring scripts are not runtime inputs.
            with self.subTest(name=name):
                self.assertEqual(digest, hashlib.sha256(path.read_bytes()).hexdigest())
            checked += 1
        self.assertGreater(checked, 160)

    def test_png_dimensions_transparency_and_slice_bounds(self):
        manifest = json.loads((SOURCE / 'metadata/hud_manifest.json').read_text())
        self.assertEqual(78, len(manifest['assets']))
        for asset in manifest['assets']:
            with self.subTest(asset=asset['id']), Image.open(ROOT / asset['path']) as image:
                self.assertEqual(tuple(asset['size']), image.size)
                self.assertEqual('RGBA', image.mode)
                low, high = image.getchannel('A').getextrema()
                self.assertEqual(0, low)
                self.assertGreater(high, 0)
                if margins := asset.get('slice'):
                    self.assertLess(margins['left'] + margins['right'], image.width)
                    self.assertLess(margins['top'] + margins['bottom'], image.height)
                svg = ET.parse(SOURCE / asset['svg'])
                self.assertFalse(any(node.tag.rsplit('}', 1)[-1] in ('image', 'script', 'foreignObject') for node in svg.iter()))

    def test_native_theme_references_only_supplied_parts(self):
        theme = ROOT / 'src/Game.Godot/UI/ForestHud/ForestTheme.tres'
        paths = re.findall(r'path="res://([^"]+)"', theme.read_text())
        self.assertEqual(39, len(paths))
        for path in paths:
            self.assertTrue(path.startswith('UI/ForestHud/'))
            self.assertTrue((ROOT / 'src/Game.Godot' / path).is_file(), path)
        self.assertNotIn('previews/', theme.read_text())
