#!/usr/bin/env python3
"""Build a local review of actual application frames. Never draws or alters game images."""
import json
from pathlib import Path
import shutil

root = Path(__file__).resolve().parent.parent
out = root / 'artifacts/battle-review/index.html'
out.parent.mkdir(parents=True, exist_ok=True)
frames = {}
for engine in ('godot', 'monogame'):
    for mode in ('debug', 'sprites'):
        source = root / f'artifacts/captures/{engine}/battle/{mode}'
        frames[engine + '/' + mode] = []
        for png in sorted(source.rglob('*.png')):
            relative = png.relative_to(source)
            target = out.parent / 'frames' / engine / mode / relative
            target.parent.mkdir(parents=True, exist_ok=True)
            shutil.copy2(png, target)
            metadata = json.loads(png.with_suffix('.json').read_text())
            frames[engine + '/' + mode].append(dict(name=str(relative.with_suffix('')), src=str(target.relative_to(out.parent)), metadata=metadata))
    for name in ('missing-sprites', 'camera-angled-fallback'):
        source = root / f'artifacts/captures/{engine}/battle/{name}.png'
        if source.exists():
            target = out.parent / 'frames' / engine / (name + '.png')
            target.parent.mkdir(parents=True, exist_ok=True)
            shutil.copy2(source, target)
            frames[engine + '/sprites'].append(dict(name=name, src=str(target.relative_to(out.parent)), metadata=json.loads(source.with_suffix(".json").read_text())))
profiles = {}
for engine in ('godot', 'monogame'):
    source = root / f'artifacts/profiles/{engine}-animation.json'
    if source.exists():
        profiles[engine] = json.loads(source.read_text())
page = r'''<!doctype html><html lang="en"><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1">
<title>The Common Watch · Actual animation evidence</title>
<style>
:root{color-scheme:dark;--bg:#101820;--panel:#192431;--ink:#e7edf2;--muted:#a9bcc7;--gold:#efd478;--line:#3f555d}*{box-sizing:border-box}body{margin:0;background:var(--bg);color:var(--ink);font:16px/1.6 system-ui,sans-serif}main{max-width:1440px;margin:auto;padding:38px 28px 90px}h1{font-size:clamp(30px,4vw,52px);line-height:1.1;max-width:1050px;font-weight:650;letter-spacing:-1.3px}h2{font-size:24px}p{max-width:88ch}small,.muted{color:var(--muted)}.eyebrow{color:var(--gold);letter-spacing:.16em;font-size:12px;text-transform:uppercase}section{margin:32px 0;border-top:1px solid var(--line);padding-top:24px}button,select{background:var(--panel);border:1px solid var(--line);color:var(--ink);border-radius:6px;padding:10px 14px;font:inherit}button:hover,button:focus{border-color:var(--gold)}button.active{background:var(--gold);color:#182331}.controls{display:flex;gap:10px;align-items:center;flex-wrap:wrap;margin-bottom:15px}select{max-width:100%}label{display:flex;gap:8px;align-items:center}.screen{overflow:auto;background:#0b1118;border:1px solid var(--line);border-radius:8px}.screen img{width:100%;display:block}.screen.native img{width:1280px;max-width:none}.receipt{padding:12px 0;color:var(--gold);font:13px/1.7 ui-monospace,monospace}input[type=range]{flex:1;min-width:150px;accent-color:var(--gold)}.columns{display:grid;grid-template-columns:repeat(2,minmax(0,1fr));gap:30px}.note{padding:20px;background:var(--panel);border-left:3px solid var(--gold)}a{color:var(--gold)}pre{white-space:pre-wrap;overflow-wrap:anywhere;background:var(--panel);padding:20px;font-size:12px}details{margin:12px 0}summary{cursor:pointer}li{margin:8px 0}@media(max-width:750px){.columns{grid-template-columns:1fr}main{padding:24px 16px}.controls>*{max-width:100%}}
</style><main>
<div class="eyebrow">The Common Watch V2 · Approach A · Linux / Mesa</div>
<h1>One painted figure.<br>A complete combat exchange.</h1>
<p>Both clients animate the existing static swordsman and raider PNGs from authoritative combat playback. These are real application captures. The sequence player changes between recorded frames; it never interpolates, redraws, or generates game imagery.</p>
<p class="note">The experiment demonstrates readable tabletop combat. Fixed limbs and weapon angles remain visible at close zoom, and crowded squads still overlap at normal zoom. These results support this presentation style without establishing that one static image will satisfy every final-art requirement.</p>
<section aria-label="Actual frames"><div class="controls">
<button id="godot" class="active" onclick="engine='godot';reset()">Godot 2D</button><button id="monogame" onclick="engine='monogame';reset()">MonoGame</button>
<label>Artwork <select id="mode" onchange="reset()"><option value="sprites">Existing PNGs</option><option value="debug">Debug figures</option></select></label>
<label><input type="checkbox" id="native" onchange="document.querySelector('.screen').classList.toggle('native',this.checked)">Original 1280px frame</label>
</div><div class="controls"><label>Capture <select id="phase" onchange="stop();show(this.value)"></select></label><button id="play" onclick="togglePlay()">Play ordered sequence</button><input aria-label="Sequence frame" id="scrub" type="range" min="0" max="31" value="0" oninput="stop();showSequence(+this.value)"></div>
<div class="screen"><img id="frame" alt="Actual rendered combat frame" src="frames/godot/sprites/swordsman-idle.png"></div><div id="receipt" class="receipt"></div>
<details><summary>Capture receipt and authoritative unit state</summary><pre id="metadata"></pre></details>
</section>
<section><h2>What the frames show</h2><div class="columns"><article><h3>Godot 2D</h3><p>Retained Node2D figures use ground-anchored transforms and a native CanvasItem hit-flash shader. Canvas drawing supplies arcs, shots, health bars, damage fragments and status particles. Anticipation, lunge and recovery are sampled from the unit's attack deadlines.</p><p>At normal zoom the hit reaction and attack direction are readable. The idle sway is restrained. At close zoom, unchanged knees, sword and shield reveal the static illustration. The crowded opening exchange remains visually dense.</p></article><article><h3>MonoGame</h3><p>SpriteBatch draws anchored PNGs and geometric figures with explicit sampled poses. A native BasicEffect fog shader blends hit figures toward white; ordinary tinting supplies the optional fallback. Programmatic lines, rings and particles distinguish physical shots, magic and statuses.</p><p>At normal zoom, the same attack stages and health changes are readable. The ordered frames show walking lift and lean, though the fixed leg pose still feels like a tabletop piece being moved. Close-up captures make this limitation clear.</p></article></div></section>
<section><h2>Scenes earned under normal rules</h2><ul>
<li><strong>A · Swordsmen and raiders:</strong> the existing paid, fixed-seed four-versus-four first wave. It uses exactly two character images. The benchmark preserves the real battle roster.</li>
<li><strong>B · Ranged and magic:</strong> legal progression to wave four. Unillustrated unit types visibly use geometric figures.</li>
<li><strong>C · Statuses:</strong> poison is earned through Venom research; separate Arcanist runs earn Fire and Frost. Captures show the authoritative applications and expiry deadlines.</li>
<li><strong>C · Structures:</strong> a paid Arrow Tower and no recruited army produce a real losing battle, including watchpost and keep damage/destruction. No victory or damage was fabricated.</li>
</ul><p class="muted">Each PNG has a JSON receipt in the capture directory. Native screenshots are retained unchanged. Artwork comes from the existing supplied sheets; no character art, animation frames or projectile images were generated.</p></section>
<section><h2>Timing and cleanup</h2><p>Damage flashes and numbers begin only on drained Hit events with positive damage. Distinct event sequences remain distinct; repeated snapshots do not replay effects. Baselines clear the visual store, and dead bodies finish at Core's lifecycle boundary. Terminal cosmetic effects use the paused, connected playback clock so the final structure burst can finish after the last simulation tick.</p><details><summary>Native repeated-battle measurements</summary><p class="muted">Nine runs per client cover requested rendering rates of 30, 60 and 120 FPS and all supported speeds. Values describe this software-rendered Linux environment. Submission time measures application work, not a GPU benchmark.</p><pre id="profiles"></pre></details></section>
<section><h2>Verification and remaining limits</h2><p>The final full suite passed 151 .NET tests and 14 Pillow tests, including native graphical checks, actual multiplayer battles in both clients and disconnect/reconnect during a real paused death. Separate animation checks and full capture tasks passed. Each engine has 19 named captures and 32 ordered frames in each art mode, plus fallback and camera checks.</p><p>An earlier Godot host crashed before combat in a native .NET tiered-JIT stack. The focused reconnect rerun and final full suite passed; the intermittent runtime failure remains a stability limitation. Verification used Linux with Mesa software rendering. Physical GPU, Windows and live Steam-account qualification were not executed.</p><p>Only the swordsman and raider have suitable supplied character images. Ranged and Mage figures use geometric fallbacks, so their effects are demonstrated without asserting the quality of unavailable artwork.</p></section>
<section><h2>Reproduce locally</h2><p><code>mise run battle-godot-assets</code><br><code>mise run battle-monogame-assets</code><br><code>mise run capture-godot-battle</code><br><code>mise run capture-monogame-battle</code></p><p class="muted">Space pauses; Backspace restarts; Tab cycles speed; T changes art; O toggles diagnostics; V changes ground projection. Scenario controls are native in each client.</p><p class="muted">Review styling follows the repository's dark blue-gray surfaces, pale text and gold accents. This page is a review of actual captures, not a recreated game client.</p></section>
<script>
const all=FRAMES, profiles=PROFILES;let engine='godot',timer=null,index=0;const $=id=>document.getElementById(id);function group(){return all[engine+'/'+$('mode').value]}function sequences(){return group().filter(f=>f.name.startsWith('sequence/'))}function stop(){clearInterval(timer);timer=null;$('play').textContent='Play ordered sequence'}function show(name){const f=group().find(x=>x.name===name)||group()[0];if(!f)return;$('frame').src=f.src;$('receipt').textContent=engine+' · '+f.name+' · '+f.metadata.scenario+' · authoritative tick '+f.metadata.authoritativeTick+' · presentation tick '+Number(f.metadata.presentationTick).toFixed(2)+' · event '+f.metadata.eventSequence;$('metadata').textContent=JSON.stringify(f.metadata,null,2)}function showSequence(i){index=i;const list=sequences();if(list[i]){show(list[i].name);$('scrub').value=i}}function togglePlay(){if(timer){stop();return}const list=sequences();list.forEach(f=>{let image=new Image();image.src=f.src});showSequence(0);$('play').textContent='Pause sequence';timer=setInterval(()=>showSequence(index=(index+1)%list.length),200)}function reset(){stop();for(const id of ['godot','monogame'])$(id).classList.toggle('active',id===engine);$('phase').replaceChildren(...group().filter(f=>!f.name.startsWith('sequence/')).map(f=>new Option(f.name.replaceAll('-',' '),f.name)));$('phase').value='swordsman-idle';$('scrub').max=sequences().length-1;index=0;$('scrub').value=0;show('swordsman-idle');$('profiles').textContent=JSON.stringify(profiles[engine]||'Profile has not been generated.',null,2)}reset();
</script></main></html>'''
page = page.replace('FRAMES', json.dumps(frames).replace('</', '<\\/')).replace('PROFILES', json.dumps(profiles))
out.write_text(page)
print(out)
