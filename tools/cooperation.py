#!/usr/bin/env python3
"""Actual 2–6-client team layouts and an uneven default-rules elimination session."""
import json
import os
from pathlib import Path
import signal
import socket
import subprocess
import tempfile
import time

ROOT = Path(__file__).resolve().parent.parent
OUTPUT = ROOT / 'artifacts/captures/cooperation'
OUTPUT.mkdir(parents=True, exist_ok=True)
receipts = []
signal.signal(signal.SIGTERM, lambda *_: (_ for _ in ()).throw(SystemExit(143)))

for count in range(2, 7):
    processes, logs = [], []
    serial = 0
    with tempfile.TemporaryDirectory(prefix='common-watch-team-') as temporary:
        work = Path(temporary)
        with socket.socket(socket.AF_INET, socket.SOCK_DGRAM) as sock:
            sock.bind(('127.0.0.1', 0))
            port = sock.getsockname()[1]

        def launch(index):
            directory = work / str(index)
            directory.mkdir(exist_ok=True)
            (directory / 'control').mkdir(exist_ok=True)
            log = open(work / f'{index}-{len(logs)}.log', 'w+')
            logs.append(log)
            command = ['bash', 'tools/client.sh', '--settings-path', str(directory / 'settings.json'),
                       '--data-path', str(directory), '--control-dir', str(directory / 'control'),
                       '--test-fast-combat', 'yes', '--gameplay-stop-wave', '21',
                       '--gameplay-family', 'limited' if index == 0 else 'idle', '--hold-on-fall', 'yes', '--hold-on-low-keep', 'yes']
            command += ['--host', str(port)] if index == 0 else ['--join', f'127.0.0.1:{port}']
            processes.append(subprocess.Popen(command, cwd=ROOT, stdout=log, stderr=subprocess.STDOUT, start_new_session=True))

        def status(i):
            path = work / str(i) / 'control/status.json'
            return json.loads(path.read_text()) if path.exists() else None

        def wait(predicate, message, seconds=45):
            deadline = time.monotonic() + seconds
            while time.monotonic() < deadline:
                value = predicate()
                if value:
                    return value
                if any(p.poll() not in (None, 0) for p in processes):
                    raise RuntimeError('Godot process failed: ' + message)
                time.sleep(.06)
            raise TimeoutError(message + ': ' + repr([(status(i) or {}).get('teamRows') for i in range(count)]))

        def control(i, kind, **values):
            global serial
            serial += 1
            path = work / str(i) / 'control' / f'request-{serial:06}.json'
            path.with_suffix('.tmp').write_text(json.dumps(dict(kind=kind, **values)))
            path.with_suffix('.tmp').replace(path)

        def send(i, action, **values):
            old = status(i)['result']
            sequence = old['Sequence'] if old else 0
            control(i, 'send', action=action, **values)
            result = wait(lambda: status(i)['result'] if status(i)['result'] and status(i)['result']['Sequence'] > sequence and not status(i)['pending'] else None, action)
            assert result['Accepted'], result

        def city(i):
            return next(p for p in status(i)['state']['Players'] if p['Id'] == status(i)['player'])

        def capture(name, i=0):
            for width, height in ((1280, 720), (1920, 1080)):
                control(i, 'resize', width=width, height=height)
                wait(lambda: status(i)['viewportWidth'] == width and status(i)['teamTargetsFit'], name + ' fitting native targets')
                path = OUTPUT / f'{count}p-{name}-{width}.png'
                path.unlink(missing_ok=True)
                control(i, 'capture', path=str(path))
                wait(lambda: path.exists(), name + ' screenshot')
                if name in ('fallen-observer', 'terminal'):
                    assert not set(status(i)['hudRegions']) & {'ledger', 'threat', 'guide', 'selected-plot', 'preparation-track'}
                    assert not status(i)['readyVisible']
            control(i, 'resize', width=1280, height=720)

        try:
            launch(0)
            wait(lambda: status(0) and status(0)['connected'], 'host')
            for index in range(1, count):
                launch(index)
                wait(lambda: status(index) and status(index)['connected'], 'guest')
            wait(lambda: len(status(0)['state']['Players']) == count, 'complete roster')
            for i in range(count):
                send(i, 'doctrine:warden')
            send(0, 'start')
            wait(lambda: all(status(i)['teamVisible'] and len(status(i)['teamRows']) == count for i in range(count)), 'persistent team')
            capture('preparing')
            for width, height in ((1280, 720), (1920, 1080)):
                control(0, 'resize', width=width, height=height)
                wait(lambda: status(0)['viewportWidth'] == width, 'pointer viewport')
                control(0, 'team-pointer', owner=count)
                wait(lambda: status(0)['viewed'] == count and 'read-only' in status(0)['readOnly'], 'pointer allied inspection')
                assert status(0)['player'] == 1 and not city(0)['Ready']
                control(0, 'ready-pointer')
                wait(lambda: city(0)['Ready'] and 'Ready' in status(0)['teamRows'][0], 'Ready still belongs to local city')
                assert not next(p for p in status(0)['state']['Players'] if p['Id'] == count)['Ready']
                send(0, 'unready')
            started = time.monotonic()
            send(0, 'ready')
            wait(lambda: 'Ready' in status(count - 1)['teamRows'][0] and 'Preparing' in status(count - 1)['teamRows'][-1], 'visible split readiness')
            capture('ready-waiting', count - 1)
            receipts.append(dict(players=count, readyWaitSeconds=round(time.monotonic() - started, 3), rows=status(count - 1)['teamRows']))
            send(0, 'unready')
            if count != 2:
                print(f'GODOT_COOP_LAYOUT_PASS {count}p native pointer allied read-only local Ready at 720p/1080p', flush=True)
                continue
            send(0, 'pause')
            wait(lambda: 'PAUSED' in status(1)['teamHeading'], 'paused team')
            capture('paused', 1)
            send(0, 'resume')
            # A genuine disconnect, then the existing private resume path.
            revision = status(0)['state']['Revision']
            control(1, 'leave')
            wait(lambda: status(1)['state'] is None, 'guest returns to menu')
            wait(lambda: 'Away' in status(0)['teamRows'][1] and status(0)['state']['Revision'] > revision, 'away teammate')
            capture('away')
            control(1, 'reconnect')
            wait(lambda: status(1)['connected'] and 'Preparing' in status(0)['teamRows'][1], 'same city reconnect')
            assert status(1)['player'] == 2
            for i in range(2):
                control(i, 'team-city', owner=i + 1)
                control(i, 'policy', enabled='yes')
            wait(lambda: status(0)['state']['Paused'] and any('LOW KEEP' in row for row in status(0)['teamRows']), 'endangered city during real combat', seconds=120)
            capture('endangered')
            send(0, 'resume')
            wait(lambda: status(0)['state']['Paused'] and any(p['Eliminated'] for p in status(0)['state']['Players']), 'legal early elimination', seconds=120)
            wait(lambda: any(city(i)['Eliminated'] for i in range(2)), 'fallen owner receives authoritative snapshot')
            fallen = next(i for i in range(2) if city(i)['Eliminated'])
            fall = time.monotonic()
            wait(lambda: status(fallen)['teamObserver'] and not status(fallen)['readyVisible'], 'fallen observer has no Ready')
            capture('fallen-observer', fallen)
            control(fallen, 'watch-ally')
            wait(lambda: status(fallen)['viewed'] != status(fallen)['player'] and 'read-only' in status(fallen)['readOnly'], 'watch living ally')
            capture('watch-living-ally', fallen)
            fall_wave = status(0)['state']['Wave']
            capture_pause = time.monotonic() - fall
            send(0, 'resume')
            resumed = time.monotonic()
            wait(lambda: status(0)['state']['Phase'] in ('Victory', 'Defeat'), 'uneven group terminal outcome', seconds=150)
            wait(lambda: status(fallen)['state']['Phase'] == status(0)['state']['Phase'], 'observer terminal snapshot')
            receipts.append(dict(players=2, fallWave=fall_wave, spectatorWallSecondsIncludingCapturePause=round(time.monotonic()-fall, 3),
                                 capturePauseSeconds=round(capture_pause, 3), acceleratedActiveSpectatorSeconds=round(time.monotonic()-resumed, 3),
                                 finalWave=status(0)['state']['Wave'], result=status(0)['state']['Phase'], team=status(fallen)['teamRows']))
            capture('terminal', fallen)
            print('GODOT_COOP_ELIMINATION_PASS', receipts[-1], flush=True)
        finally:
            for process in reversed(processes):
                if process.poll() is None:
                    os.killpg(process.pid, signal.SIGTERM)
                    try:
                        process.wait(timeout=5)
                    except subprocess.TimeoutExpired:
                        os.killpg(process.pid, signal.SIGKILL)
                        process.wait(timeout=5)
            diagnostics = []
            for index, log in enumerate(logs):
                log.seek(0)
                transcript = log.read()
                path = ROOT / f'artifacts/test-logs/cooperation-{count}p-{index}.log'
                path.parent.mkdir(parents=True, exist_ok=True)
                path.write_text(transcript)
                if 'ERROR:' in transcript or 'Unhandled exception' in transcript:
                    diagnostics.append(transcript[-4000:])
                log.close()
            if diagnostics:
                raise RuntimeError('\n'.join(diagnostics))
            (OUTPUT / 'receipts.json').write_text(json.dumps(receipts, indent=2))
    print(f'GODOT_COOP_LAYOUT_PASS {count}p native pointer allied read-only local Ready at 720p/1080p', flush=True)
