#!/usr/bin/env python3
"""Local review of actual application frames; never synthesizes game images."""
import html
import json
from pathlib import Path
import shutil

root = Path(__file__).resolve().parents[1]
source = root / 'artifacts/captures/godot/environment'
output = root / '.lavish/dynamic-environment'
(output / 'frames').mkdir(parents=True, exist_ok=True)
frames = [p for p in source.glob('*.png') if '-1280-' in p.name or p.name.startswith('menu-') or p.name.startswith('isolated-')]
if not frames:
    raise SystemExit('Run mise run test-environment before building this review.')
for frame in frames:
    shutil.copyfile(frame, output / 'frames' / frame.name)
rows = ''
performance = source / 'performance.json'
if performance.exists():
    for row in json.loads(performance.read_text()):
        rows += '<tr><td>{}</td><td>{}</td><td>{:.2f}</td><td>{:.2f}</td><td>{:,}</td></tr>'.format(
            html.escape(row['scene']), html.escape(row['quality']), row['medianMs'], row['p95Ms'], row['allocatedBytesPerFrame'])
(output / 'index.html').write_text('''<!doctype html>
<html lang="en"><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1">
<title>Common Watch · Environment verification</title>
<style>
:root{color-scheme:dark;--bg:#0b1117;--panel:#14212c;--line:#3f555d;--gold:#efd478;--text:#e7edf2}
*{box-sizing:border-box}body{margin:0;background:var(--bg);color:var(--text);font:16px/1.55 system-ui,sans-serif}main{max-width:1440px;margin:auto;padding:32px}h1{font-size:clamp(28px,4vw,44px);font-weight:550;margin:0 0 12px}h2{font-size:24px;font-weight:550}p{max-width:85ch;color:#bdcbd2}small,.eyebrow{color:var(--gold)}section{border:1px solid var(--line);border-radius:5px;padding:22px;margin-top:24px;background:var(--panel)}.controls{display:flex;flex-wrap:wrap;align-items:end;gap:12px;margin-bottom:16px}label{display:grid;gap:5px;font-size:14px}select,button,input{font:inherit;color:var(--text);background:#20313e;border:1px solid var(--line);border-radius:4px;padding:9px}button{cursor:pointer}button:focus-visible,select:focus-visible{outline:2px solid var(--gold)}.pair{display:grid;grid-template-columns:repeat(2,minmax(0,1fr));gap:18px}figure{margin:0;min-width:0}img{display:block;width:100%;height:auto;background:#080d12;border-radius:4px}figcaption{color:var(--gold);margin:8px 0}.scroll{overflow:auto}table{width:100%;border-collapse:collapse;font-size:14px}th,td{text-align:left;border-bottom:1px solid var(--line);padding:9px}code{color:var(--gold);overflow-wrap:anywhere}summary{cursor:pointer;color:var(--gold)}@media(max-width:850px){main{padding:16px}.pair{grid-template-columns:1fr}section{padding:14px}}
</style><main>
<div class="eyebrow">THE COMMON WATCH · ACTUAL GODOT FRAMES</div>
<h1>Animated atmosphere on the existing artwork</h1>
<p>Compare Off, Subtle and Full against the same paused, legally earned battle. The two time samples below show different ambient positions while combat stays fixed. These are discrete captured frames, not a video or interpolated preview.</p>
<section><h2>Arena: quality and motion</h2><div class="controls">
<label>Players<select id="players"><option>1</option><option>6</option></select></label>
<label>Projection<select id="projection"><option>Angled</option><option>Topdown</option></select></label>
<label>Camera<select id="camera"><option value="local">Local sector</option><option value="overview">Whole field</option></select></label>
<label>Quality<select id="quality"><option value="full-t2">Full · t=2s</option><option value="full-t8">Full · t=8s</option><option value="subtle">Subtle · t=2s</option></select></label>
<button id="toggle">Switch time sample</button></div>
<div class="pair"><figure><img id="off" alt="Original arena rendering with effects off"><figcaption>Off · original static rain</figcaption></figure><figure><img id="active" alt="Same arena with enabled dynamic environment"><figcaption id="caption">Full · sampled ambient time</figcaption></figure></div>
<p>Native camera, sector boundaries, picking and authoritative combat remain unchanged. These arena captures use Ember; see the <a href="../../docs/DYNAMIC_ENVIRONMENT.md">dynamic environment guide</a> for preset effects and quality behavior.</p></section>
<section><h2>Settlement: restrained atmosphere</h2><div class="controls"><label>Preset<select id="preset"><option>Clear</option><option>Mist</option><option>Rain</option><option>Night</option><option>Enchanted</option><option>Ember</option><option>Snow</option></select></label></div><div class="pair"><figure><img id="settlement2" alt="Settlement at ambient time 2 seconds"><figcaption>t=2 seconds</figcaption></figure><figure><img id="settlement12" alt="Same settlement at ambient time 12 seconds"><figcaption>t=12 seconds</figcaption></figure></div><p>The flattened painting is unchanged. Local building effects use foundation-relative anchors; all nine clearings keep their existing ellipse picking. HUD text and buttons remain outside atmospheric composition.</p></section>
<section><h2>Menu: quiet motion behind controls</h2><div class="pair"><figure><img src="frames/menu-t2.png" alt="Menu atmosphere at 2 seconds"><figcaption>t=2 seconds</figcaption></figure><figure><img src="frames/menu-t12.png" alt="Menu atmosphere at 12 seconds"><figcaption>t=12 seconds</figcaption></figure></div></section>
<section><h2>Measured frame intervals</h2><p>Linux Xvfb / Mesa llvmpipe, 30 measured frames after warmup. Values include the whole client, frame scheduling and process-wide managed allocations; they are not isolated GPU costs. No physical GPU, native Windows or live Steam qualification is claimed.</p><details><summary>Inspect Off / Full measurements</summary><div class="scroll"><table><thead><tr><th>Scene</th><th>Quality</th><th>Median ms</th><th>p95 ms</th><th>Managed bytes/frame</th></tr></thead><tbody>''' + rows + '''</tbody></table></div></details></section>
<section><h2>Use and boundaries</h2><p>Enable <strong>Settings → Environment effects → Subtle / Full</strong>, or launch with <code>--environment-effects full --environment-preset rain</code>. Off is the default. All effect time comes from one local presentation clock and freezes with pause or focus loss.</p><p>Water ripple, anchored foliage/banner sway and selective texture blur are reusable shaders verified with technical swatches. They are not applied to the flattened painting: qualified water/depth/foliage/emission masks are still needed. No artwork was generated or replaced. The known native .NET/JIT crash occurred during expanded verification; a passing retry does not resolve it.</p><small>Review styling matches the project’s WatchTheme dark surfaces, pale text and gold focus accents.</small></section>
</main><script>
const byId=id=>document.getElementById(id);
function arena(){const p=`arena-${byId('players').value}-1280-${byId('projection').value}-${byId('camera').value}`;byId('off').src=`frames/${p}-off.png`;byId('active').src=`frames/${p}-${byId('quality').value}.png`;byId('caption').textContent=byId('quality').selectedOptions[0].text;}
for(const id of ['players','projection','camera','quality'])byId(id).addEventListener('change',arena);
byId('toggle').onclick=()=>{byId('quality').value=byId('quality').value==='full-t2'?'full-t8':'full-t2';arena();};
function settlement(){for(const t of [2,12])byId('settlement'+t).src=`frames/settlement-1280-${byId('preset').value}-t${t}.png`;}
byId('preset').addEventListener('change',settlement);arena();settlement();
</script></html>''')
print(output / 'index.html')
