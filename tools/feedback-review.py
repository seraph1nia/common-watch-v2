#!/usr/bin/env python3
"""Review actual native audio exports; no game audio assets are authored here."""
import array
import hashlib
import html
import json
import math
from pathlib import Path
import wave

ROOT = Path(__file__).resolve().parent.parent
DIRECTORY = ROOT / 'artifacts/feedback/audio'
receipt = json.loads((DIRECTORY / 'receipt.json').read_text())
assert receipt['musicMuted'] and receipt['voices'] == 6

def inspect(path):
    with wave.open(str(path), 'rb') as stream:
        assert stream.getsampwidth() == 2
        channels, rate, frames = stream.getnchannels(), stream.getframerate(), stream.getnframes()
        pcm = array.array('h', stream.readframes(frames))
    peak = max(abs(x) for x in pcm)
    rms = math.sqrt(sum(x*x for x in pcm) / len(pcm))
    assert 0 < peak < 32767
    buckets = []
    step = max(1, len(pcm) // 400)
    for start in range(0, len(pcm), step):
        sample = pcm[start:start+step]
        buckets.append((min(sample), max(sample)))
    def db(value):
        return round(20 * math.log10(max(1, value) / 32768), 2)
    stats = dict(file=path.name, channels=channels, rate=rate, seconds=frames/rate,
                 peak=peak, peak_dbfs=db(peak), rms_dbfs=db(rms),
                 sha256=hashlib.sha256(path.read_bytes()).hexdigest())
    points = ' '.join(f'{i*400/len(buckets):.2f},{50-v[1]/32768*46:.2f}' for i,v in enumerate(buckets))
    points += ' ' + ' '.join(f'{i*400/len(buckets):.2f},{50-v[0]/32768*46:.2f}' for i,v in reversed(list(enumerate(buckets))))
    graph = f'<svg viewBox="0 0 400 100" role="img" aria-label="Recorded PCM amplitude"><path d="M0 50 H400" stroke="#637474"/><polygon points="{points}" fill="#a7d2c2"/></svg>'
    return stats, graph, pcm

cards, measurements, hashes = [], [], set()
for item in receipt['palette']:
    stats, graph, pcm = inspect(DIRECTORY / item['file'])
    assert stats['channels'] == 1 and stats['rate'] == 44100
    assert abs(stats['seconds'] - item['seconds']) < 1/44100 + 1e-9
    assert pcm[0] == 0 and abs(pcm[-1]) <= 4
    hashes.add(stats['sha256'])
    measurements.append(stats)
    cards.append(f'<article><h2>{html.escape(item["kind"])}</h2><p>{item["bus"]} · {stats["seconds"]:.3f}s · peak {stats["peak_dbfs"]} dBFS · RMS {stats["rms_dbfs"]} dBFS</p>{graph}<audio controls preload="none" src="audio/{item["file"]}"></audio></article>')
assert len(hashes) == len(receipt['palette']) == 16
mixed, graph, _ = inspect(DIRECTORY / 'native-mix.wav')
assert mixed['channels'] == 2 and mixed['peak'] == receipt['peak']
measurements.append(mixed)
report = dict(native=receipt, measured=measurements,
              review='Local measured PCM review; human/physical listening is unexecuted and is not inferred from Dummy-driver playback.')
(DIRECTORY.parent / 'review.json').write_text(json.dumps(report, indent=2))
page = f'''<!doctype html><meta charset="utf-8"><meta name="viewport" content="width=device-width"><title>Common Watch feedback audition</title>
<style>body{{background:#102321;color:#efe9d7;font:16px system-ui;max-width:1160px;margin:30px auto;padding:0 18px}}section{{display:grid;grid-template-columns:repeat(auto-fit,minmax(280px,1fr));gap:16px}}article{{border:1px solid #647e73;padding:16px}}h2{{font-size:22px}}audio,svg{{width:100%}}p{{line-height:1.6}}</style>
<h1>Native feedback audition</h1><p>The palette below is exported from the actual cached Godot streams. The mixed clip is recorded from Godot's Master bus with music muted. Driver: {html.escape(receipt['driver'])}; six retained voices. These controls let a listener audition the results; this file does not assert a human listening review.</p>
<article><h2>Actual native mixed sequence</h2><p>Peak {mixed['peak_dbfs']} dBFS · RMS {mixed['rms_dbfs']} dBFS. No clipping in this recording.</p>{graph}<audio controls preload="none" src="audio/native-mix.wav"></audio></article>
<p>Technical earcons have distinct durations/envelopes/notes, limited repetition and separate UI/SFX routing. Source music and authored loop markers remain unchanged. Automated PCM/functional evidence is separate from unexecuted physical listening. The user accepted local evidence for the roadmap.</p><section>{''.join(cards)}</section>'''
(DIRECTORY.parent / 'index.html').write_text(page)
print(f'FEEDBACK_PCM_REVIEW_PASS {len(hashes)} distinct native tone exports, muted-music Master mix peak={mixed["peak_dbfs"]}dBFS; human listening unexecuted')
