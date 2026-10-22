"""Build the local review from fresh, logged production screenshots and real assets."""
from pathlib import Path
import hashlib
import html
import json
import re
import shutil
from PIL import Image

ROOT = Path(__file__).resolve().parents[1]
OUT = ROOT / 'artifacts/painted-forest-hud'
SOURCE = ROOT / 'art/source/painted_forest_hud'


def logged(log):
    return {Path(p.strip()).relative_to(ROOT / 'artifacts/captures/gameplay/forest').as_posix()
            for p in re.findall(r'GODOT_GAMEPLAY_CAPTURE (.+)', (OUT / log).read_text())}


def build():
    fresh = {'before': logged('baseline-capture.log'), 'after': logged('capture-matched-last.log')}
    inventory = []
    for side in ('before', 'after'):
        for path in sorted((OUT / side).rglob('*.png')):
            relative = path.relative_to(OUT / side).as_posix()
            if relative.startswith('gameplay/') and relative.removeprefix('gameplay/') not in fresh[side]:
                continue
            with Image.open(path) as image:
                image.verify()
            with Image.open(path) as image:
                size = list(image.size)
            inventory.append(dict(path=path.relative_to(OUT).as_posix(), side=side, dimensions=size,
                sha256=hashlib.sha256(path.read_bytes()).hexdigest(), capture='Actual Godot production interface',
                environment='Off' if relative.startswith('gameplay/') else 'Network worker default'))
    (OUT / 'screenshot-inventory.json').write_text(json.dumps(inventory, indent=2)+'\n')
    indexed = {entry['path']: entry for entry in inventory}
    names = ['hud-idle', 'hud-build', 'hud-build-focus-hover', 'hud-locked', 'hud-occupied',
        'hud-military', 'build-production', 'build-military', 'build-defense', 'build-trade', 'build-utility',
        'build-gold-recovery-selected', 'market-trading', 'town-hall', 'army-roster', 'army-deployment',
        'worldtree', 'food-shortage', 'hud-combat', 'reward-draft', 'victory', 'defeat', 'menu-over-settlement']
    pairs = []
    for width, height in ((1280, 720), (1920, 1080)):
        for name in names + ['preparation-1', 'preparation-2', 'preparation-3']:
            suffix = ('1920/' if width == 1920 else '') + name + '.png'
            before, after = 'before/gameplay/'+suffix, 'after/gameplay/'+suffix
            if after not in indexed or indexed[after]['dimensions'] != [width, height]:
                continue
            if before not in indexed or indexed[before]['dimensions'] != [width, height]:
                before = None
            pairs.append(dict(name=name, width=width, before=before, after=after))
    for name in ('pending', 'rejected', 'disconnected'):
        for width in (1280, 1920):
            path = f'after/network/{name}-{width}.png'
            if path in indexed:
                pairs.append(dict(name=name+' · network', width=width, before=None, after=path))
    for name, path in [('reference.png', SOURCE/'references/reference_hud.png'),
            ('contact-sheet.jpg', SOURCE/'contact-sheet.jpg'), ('proof-comparison.jpg', SOURCE/'proof/reference-comparison.jpg')]:
        shutil.copyfile(path, OUT/name)
    preview = OUT/'asset-preview'; preview.mkdir(exist_ok=True)
    assets = json.loads((SOURCE/'manifest.json').read_text())['assets']
    rows = []
    for asset in assets:
        path = ROOT/asset['runtime']; shutil.copyfile(path, preview/path.name)
        rows.append(f'<tr><td><img loading="lazy" src="asset-preview/{html.escape(path.name)}" alt="{asset["id"]}"></td>'
            f'<td><strong>{asset["id"]}</strong><br>{asset["state"]}</td><td>{asset["dimensions"]}<br>slice {asset["nine_slice"]}</td>'
            f'<td>{asset["attachment_anchor"]}<br>padding {asset["transparent_padding"]}</td><td><code>{asset["sha256"]}</code></td></tr>')
    data = json.dumps(pairs)
    page = '''<!doctype html><html lang="en"><head><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1">
<title>Common Watch · Painted forest HUD</title><style>
:root{color-scheme:dark;--bg:#101c20;--panel:#18292b;--text:#f6e8ca;--gold:#efd478;--muted:#b7ceba;--line:#4a5142}
*{box-sizing:border-box}body{margin:0;background:var(--bg);color:var(--text);font:16px/1.5 system-ui,sans-serif}main{max-width:1800px;margin:auto;padding:28px}h1{font:38px/1.1 Georgia,serif;color:var(--gold);margin:10px 0}h2{font:26px Georgia,serif;color:var(--gold);margin:34px 0 12px}p{max-width:1000px}small,.muted{color:var(--muted)}a{color:#80e4df}nav,.controls{display:flex;gap:12px;flex-wrap:wrap;align-items:center;margin:20px 0}nav a{color:var(--gold)}select,button{font:inherit;color:var(--text);border:1px solid #967447;background:#263b35;border-radius:10px;padding:10px;min-height:44px}input{accent-color:#56c99b}.gallery{display:grid;grid-template-columns:1fr;gap:18px}.gallery.split{grid-template-columns:1fr 1fr}.shot{min-width:0;background:var(--panel);border:1px solid var(--line);border-radius:12px;overflow:hidden}.shot header{padding:10px 14px;color:var(--gold)}.viewport{overflow:auto;max-height:85vh}.viewport img{display:block;width:100%;height:auto}.native .viewport img{width:auto;max-width:none}.shot[hidden]{display:none}.note{padding:14px;background:#2b3328;border-left:3px solid var(--gold);margin:16px 0}.media{display:block;max-width:100%;height:auto;background:#122023;border:1px solid var(--line);border-radius:10px}.tablewrap{overflow:auto;max-height:650px}table{width:100%;border-collapse:collapse;text-align:left}th,td{padding:12px;border-bottom:1px solid var(--line);vertical-align:top}th{position:sticky;top:0;background:var(--panel);z-index:1}td img{width:130px;height:90px;object-fit:contain;background:linear-gradient(90deg,#ddd5c5 50%,#102123 50%);border-radius:6px}td code{display:block;max-width:220px;overflow-wrap:anywhere;font-size:12px}details{border:1px solid var(--line);border-radius:10px;padding:16px;margin:16px 0}summary{cursor:pointer;color:var(--gold)}.checks td:first-child{min-width:220px}footer{margin:36px 0;color:var(--muted)}@media(max-width:900px){main{padding:16px}h1{font-size:30px}.gallery.split{grid-template-columns:1fr}}
</style></head><body><main>
<small>THE COMMON WATCH V2 · IMPLEMENTED IN GODOT · 21 OCTOBER 2026</small>
<h1>Painted forest HUD</h1><p>The production game now uses illustrated bark, woodland characters, honey construction plaques and a separate resource ledger around the preserved settlement. Text and commands remain native Godot controls.</p>
<nav><a href="#game">Actual game</a><a href="#proof">Quality proof</a><a href="#assets">48 exports</a><a href="#checks">Verification</a></nav>
<section id="game"><h2>Fresh before / after captures</h2><p class="muted">Same legal production scenarios, matched resolutions, angled sprite world, debug overlay off and environment effects Off. Network and newly added preparation captures have no baseline and are labeled after-only. No gameplay screenshot is a composite.</p>
<div class="controls"><label>Resolution <select id="resolution"><option value="1280">1280 × 720</option><option value="1920">1920 × 1080</option></select></label><label>State <select id="state"></select></label><label>View <select id="mode"><option value="after">After</option><option value="split">Side by side</option><option value="before">Before</option></select></label><label><input id="native" type="checkbox"> Native pixels / scroll</label></div>
<p id="caption" class="muted"></p><div id="gallery" class="gallery"><article id="before" class="shot"><header>Before · fresh baseline</header><div class="viewport"><a target="_blank"><img alt="Baseline production Godot screenshot"></a></div></article><article id="after" class="shot"><header>After · actual production game</header><div class="viewport"><a target="_blank"><img alt="Current production Godot screenshot"></a></div></article></div>
<p><a href="screenshot-inventory.json">Complete screenshot dimensions, hashes and provenance</a></p></section>
<section id="proof"><h2>The component quality proof</h2><p>The branch frame, squirrel, mushrooms, blue clock and emerald/violet blanks are separate components. The development scene renders its text natively. This labeled comparison is a component review, not a gameplay capture.</p><a href="proof-comparison.jpg" target="_blank"><img class="media" loading="lazy" src="proof-comparison.jpg" alt="Reference and native component proof at both target sizes"></a><details><summary>Original reference</summary><img class="media" loading="lazy" src="reference.png" alt="User supplied original reference"></details></section>
<section id="assets"><h2>Reusable painted assets</h2><p>20 original generated RGBA masters produce 44 PNG exports plus four technical SVGs. Registered states share identical alpha and geometry. Selection is gold; focus is an independent cyan contour. No layered painting files were produced.</p><a href="contact-sheet.jpg" target="_blank"><img class="media" loading="lazy" src="contact-sheet.jpg" alt="Actual exports on dark and light backgrounds"></a><details><summary>Asset dimensions, slices, anchors, padding and SHA-256</summary><div class="tablewrap"><table><thead><tr><th>Export</th><th>ID / state</th><th>Pixels / slices</th><th>Anchor / padding</th><th>SHA-256</th></tr></thead><tbody>__ASSET_ROWS__</tbody></table></div></details></section>
<section id="checks"><h2>Verification and preservation</h2><p>470 recorded protected source/runtime files retain their original hashes. Core, Shared, world art, original ForestHud, Worldtree, audio and provenance assets are preserved. Existing uncommitted session-exit work was retained.</p><div class="tablewrap"><table class="checks"><thead><tr><th>Check</th><th>Result / evidence</th></tr></thead><tbody>
<tr><td>Asset exports</td><td><a href="asset-validation.log">48 passed</a>: RGBA, edge padding, hashes, registered state alpha, anchors, slices and true vector contents.</td></tr>
<tr><td>Build / full test</td><td><a href="build.log">Build: zero warnings/errors</a>. <a href="test.log">Full test passed</a>: 46 Core, 88 Shared, 70 session/Steam/animation logic, six process tests, 19 art tests and rendered battle-frame checks.</td></tr>
<tr><td>Gameplay / input</td><td><a href="test-gameplay-ui.log">Illustrated and plain passed</a>; native Ready through three stages, pointer Pause, controller Resume, independent focus/selection, confirmation, retained scroll and menu flows.</td></tr>
<tr><td>Captures</td><td><a href="capture-matched-last.log">Final 720p/1080p production captures</a>; <a href="capture-art.log">art</a>, <a href="capture-worldtree.log">Worldtree</a>, <a href="capture-settlement-illustrated.log">nine clearings at five sizes, fixed camera and native input</a>.</td></tr>
<tr><td>Network</td><td><a href="test-multiplayer.log">One to six players passed</a>; <a href="test-reconnect.log">reconnect/faults passed</a>; <a href="capture-network-illustrated.log">illustrated pending/rejected/disconnected and allied inspection</a>.</td></tr>
<tr><td>Preservation</td><td><a href="preservation-receipt.json">470 files checked; zero changed</a>. No world, Core, Shared, protocol or private-credential changes.</td></tr>
</tbody></table></div><div class="note">One intermediate Esc/details capture assertion failed during resize and later passes succeeded. A late Shared repeat stalled and was stopped; the earlier full suite and a bounded diagnostic rerun passed all 88. These attempts remain recorded separately.</div><p>Existing CI at the starting commit failed confirmation focus restoration and logged missing <code>rg</code>; <a href="https://github.com/seraph1nia/common-watch-v2/actions/runs/39656535912">that run</a> was inspected, not replaced. No fresh CI run, release export, live Steam, native Windows/macOS or physical GPU/controller/audio qualification is claimed. The known intermittent native .NET/JIT risk remains.</p></section>
<footer>Review design source: the project's ForestTheme colors and actual painted HUD assets, so the review matches the game. All screenshot links open the unedited PNG. Proof and contact-sheet views are labeled separately.</footer>
</main><script>
const captures=__CAPTURE_DATA__;
const resolution=document.querySelector('#resolution'), state=document.querySelector('#state'), mode=document.querySelector('#mode'), native=document.querySelector('#native'), gallery=document.querySelector('#gallery');
function options(){let previous=state.value;state.replaceChildren();for(const item of captures.filter(x=>x.width===Number(resolution.value))){let option=new Option(item.name.replaceAll('-',' ')+(item.before?'':' · after only'),item.name);state.add(option)}if([...state.options].some(x=>x.value===previous))state.value=previous;render()}
function render(){let item=captures.find(x=>x.width===Number(resolution.value)&&x.name===state.value);if(!item)return;let split=mode.value==='split'&&item.before;gallery.classList.toggle('split',Boolean(split));gallery.classList.toggle('native',native.checked);for(const side of ['before','after']){let article=document.getElementById(side);article.hidden=side==='before'?(!item.before||mode.value==='after'):mode.value==='before'&&Boolean(item.before);if(item[side]){article.querySelector('img').src=item[side];article.querySelector('a').href=item[side]}}document.getElementById('caption').textContent=item.before?'Matched legal state · '+resolution.value+'px wide · click an image to open its original PNG.':'Additional verified state · after-only · '+resolution.value+'px wide.'}
resolution.addEventListener('change',options);state.addEventListener('change',render);mode.addEventListener('change',render);native.addEventListener('change',render);options();
</script></body></html>'''
    page = page.replace('__ASSET_ROWS__', ''.join(rows)).replace('__CAPTURE_DATA__', data)
    (OUT/'index.html').write_text(page)
    script = page.split('<script>')[1].split('</script>')[0]
    (OUT/'review-script.js').write_text(script)
    print(f'Review: {len(pairs)} views, {len(inventory)} fresh actual frames, {len(assets)} exports')


if __name__ == '__main__':
    build()
