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
OUTPUT = ROOT / os.getenv('COMMON_WATCH_COOP_OUTPUT', 'artifacts/captures/cooperation')
OUTPUT.mkdir(parents=True, exist_ok=True)
receipts = []
terminal_campaign = os.getenv('COMMON_WATCH_COOP_TERMINAL') == '1'
signal.signal(signal.SIGTERM, lambda *_: (_ for _ in ()).throw(SystemExit(143)))

for count in ((2,) if terminal_campaign or os.getenv('COMMON_WATCH_COOP_PLAYERS') == '2' else range(2, 7)):
    processes, logs = [], []
    serial = 0
    capture_seconds = 0.0
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
                       '--gameplay-family', ('frontline' if terminal_campaign else 'limited') if index == 0 else 'idle',
                       '--hold-on-fall', 'no' if terminal_campaign else 'yes', '--hold-on-low-keep', 'no' if terminal_campaign else 'yes']
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
            previous = status(i)['processedRequests']
            serial += 1
            path = work / str(i) / 'control' / f'request-{serial:06}.json'
            path.with_suffix('.tmp').write_text(json.dumps(dict(kind=kind, **values)))
            path.with_suffix('.tmp').replace(path)
            wait(lambda: status(i)['processedRequests'] > previous, kind + ' processed')
            if kind in ('team-pointer', 'team-activate-native', 'watch-ally', 'combat-metrics'):
                wait(lambda: not status(i)['nativeBusy'], kind + ' completed')
                assert not status(i)['nativeError'], status(i)['nativeError']

        def send(i, action, **values):
            old = status(i)['result']
            sequence = old['Sequence'] if old else 0
            control(i, 'send', action=action, **values)
            def acknowledged():
                current = status(i)
                result = current['result']
                return result if result and result['Sequence'] > sequence and not current['pending'] else None
            result = wait(acknowledged, action)
            assert result['Accepted'], result

        def city(i):
            return next(p for p in status(i)['state']['Players'] if p['Id'] == status(i)['player'])

        def capture(name, i=0, sizes=((1280, 720), (1920, 1080))):
            global capture_seconds
            capture_started = time.monotonic()
            for width, height in sizes:
                control(i, 'resize', width=width, height=height)
                wait(lambda: status(i)['viewportWidth'] == width and status(i)['teamTargetsFit'], name + ' fitting native targets')
                path = OUTPUT / f'{count}p-{name}-{width}.png'
                path.unlink(missing_ok=True)
                control(i, 'capture', path=str(path))
                wait(lambda: path.exists(), name + ' screenshot')
                current = status(i)
                metadata = {key: current.get(key) for key in ('player', 'viewed', 'teamHeading', 'teamRows', 'teamWarnings', 'teamRect', 'cameraMode', 'cameraZoom', 'cameraPan', 'hudMoment', 'readyText', 'readyVisible')}
                metadata.update(players=count, phase=current['state']['Phase'], wave=current['state']['Wave'], turn=current['state']['Turn'], tick=current['state']['Tick'], paused=current['state']['Paused'], width=width, height=height,
                                qualification='Actual scripted native session; captures and accelerated setup are not human decision/waiting time.')
                path.with_suffix('.json').write_text(json.dumps(metadata, indent=2))
                if name in ('fallen-observer', 'terminal'):
                    assert not set(status(i)['hudRegions']) & {'ledger', 'threat', 'guide', 'selected-plot', 'preparation-track'}
                    assert not status(i)['readyVisible']
            control(i, 'resize', width=1280, height=720)
            capture_seconds += time.monotonic() - capture_started

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
            if terminal_campaign:
                started = time.monotonic()
                for i in range(count): control(i, 'policy', enabled='yes')
                wait(lambda: status(0)['state']['Phase'] == 'Victory', 'paid full uneven group campaign reaches Victory', seconds=240)
                wait(lambda: all(status(i)['hudMoment'] == 'Terminal' and not status(i)['finishingCombat'] for i in range(count)), 'living/fallen group terminal playback settles')
                for i in range(count):
                    current = status(i)
                    assert current['state']['Wave'] == 20 and not current['teamObserver']
                    assert not current['readyVisible'] and not current['draftVisible'] and not current['detailsVisible']
                    assert not set(current['hudRegions']) & {'ledger', 'threat', 'guide', 'selected-plot', 'preparation-track'}
                    capture(f"victory-p{current['player']}-" + ('living' if not city(i)['Eliminated'] else 'fallen'), i)
                receipts.append(dict(players=count, result='Victory', wave=20, acceleratedWallSecondsIncludingCaptures=round(time.monotonic()-started, 3),
                                     artificialCaptureSeconds=round(capture_seconds, 3), snapshots=[status(i)['state'] for i in range(count)],
                                     qualification='Paid accelerated uneven group campaign, no injected state; terminal captures are not ordinary viewing or human timing.'))
                print('GODOT_COOP_VICTORY_PASS real uneven W20 terminal both local owners/sizes no new draft or management', flush=True)
                continue
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
            started = time.monotonic(); captured_before_wait = capture_seconds
            send(0, 'ready')
            wait(lambda: 'Ready' in status(count - 1)['teamRows'][0] and 'Preparing' in status(count - 1)['teamRows'][-1], 'visible split readiness')
            capture('ready-waiting', count - 1)
            capture('local-ready-waiting', 0)
            receipts.append(dict(players=count, readyWaitSeconds=round(time.monotonic() - started, 3), artificialCaptureSeconds=round(capture_seconds-captured_before_wait, 3), qualification='Scripted waiting includes native input and artificial captures; not human timing.', rows=status(count - 1)['teamRows']))
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
            control(0, 'hold-cross-sector')
            for i in range(2):
                control(i, 'team-city', owner=i + 1)
                control(i, 'policy', enabled='yes')
            seen = set()
            fallen = None
            fall = None
            paused_after_fall = 0.0
            for boundary in range(4):
                wait(lambda: status(0)['state']['Paused'] or status(0)['state']['Phase'] in ('Victory', 'Defeat'), 'legal exception boundary', seconds=150)
                if status(0)['state']['Phase'] in ('Victory', 'Defeat'):
                    break
                pause_started = time.monotonic()
                stops = status(0)['captureStops']
                if stops['crossSector'] and 'cross' not in seen:
                    def crossed():
                        return next((u for u in status(1)['observedUnits'] if u['owner'] > 0 and u['sector'] > 0 and u['owner'] != u['sector']), None)
                    actor = wait(crossed, 'playback reaches actual cross-sector cell')
                    control(1, 'team-pointer', owner=actor['sector'])
                    wait(lambda: status(1)['viewed'] == actor['sector'] and status(1)['cameraFocusCity'] == actor['sector'] and status(1)['cameraMode'] == 'Automatic', 'focus actual observed sector')
                    control(1, 'inspect-combat', unitId=actor['id'])
                    wait(lambda: status(1)['liveUnit'] == actor['id'] and f"Location: P{actor['sector']} sector" in status(1)['liveText'], 'live location follows accepted playback cell')
                    assert f"P{actor['owner']}" in status(1)['liveText']
                    capture('cross-sector-owner', 1)
                    receipts.append(dict(players=2, crossSectorUnit=actor, liveText=status(1)['liveText'],
                                         qualification='Actual accepted playback; inspector selected through development inspection API. Native team/camera input is separate evidence.'))
                    control(1, 'close-combat')
                    for i in range(2): control(i, 'team-city', owner=i + 1)
                    seen.add('cross')
                if stops['lowKeep'] and 'low' not in seen:
                    assert 'LOW KEEP' in status(0)['teamWarnings']
                    capture('endangered')
                    seen.add('low')
                if stops['fallen'] and fallen is None:
                    wait(lambda: any(city(i)['Eliminated'] for i in range(2)), 'fallen owner receives authoritative snapshot')
                    fallen = next(i for i in range(2) if city(i)['Eliminated'])
                    fall = pause_started = time.monotonic()
                    fall_wave = status(0)['state']['Wave']
                    wait(lambda: status(fallen)['teamObserver'] and not status(fallen)['readyVisible'], 'fallen observer has no Ready')
                    capture('fallen-observer', fallen)
                    control(fallen, 'watch-ally')
                    wait(lambda: status(fallen)['viewed'] != status(fallen)['player'] and 'read-only' in status(fallen)['readOnly'], 'watch living ally')
                    capture('watch-living-ally', fallen)
                    for width, height in ((1280, 720), (1920, 1080)):
                        control(fallen, 'resize', width=width, height=height)
                        wait(lambda: status(fallen)['viewportWidth'] == width, 'fallen manual viewport')
                        control(fallen, 'team-activate-native', owner=status(fallen)['player'], input='controller')
                        wait(lambda: status(fallen)['viewed'] == status(fallen)['player'] and status(fallen)['cameraMode'] == 'Automatic', 'explicit fallen own-sector choice')
                        control(fallen, 'camera-wheel-native')
                        wait(lambda: status(fallen)['cameraMode'] == 'Manual', 'fallen manual camera')
                        pose = (status(fallen)['cameraZoom'], status(fallen)['cameraPan'])
                        send(0, 'speed-2x')
                        wait(lambda: status(fallen)['state']['CombatSpeed'] == 'Two', 'fallen receives unrelated speed update')
                        send(0, 'speed-1x')
                        wait(lambda: status(fallen)['state']['CombatSpeed'] == 'One', 'fallen receives second unrelated update')
                        assert status(fallen)['viewed'] == status(fallen)['player']
                        assert (status(fallen)['cameraZoom'], status(fallen)['cameraPan']) == pose
                        capture('fallen-explicit-manual', fallen, sizes=((width, height),))
                    control(fallen, 'team-pointer', owner=1 if status(fallen)['player'] == 2 else 2)
                    wait(lambda: status(fallen)['viewed'] != status(fallen)['player'] and status(fallen)['cameraMode'] == 'Automatic', 'fallen deliberately returns to living ally')
                    seen.add('fallen')
                if fall is not None:
                    paused_after_fall += time.monotonic() - pause_started
                send(0, 'resume')
            wait(lambda: status(0)['state']['Phase'] in ('Victory', 'Defeat'), 'uneven group terminal outcome', seconds=150)
            assert seen == {'cross', 'low', 'fallen'}, ('legal replay did not cover all real exceptions', seen)
            assert fallen is not None and fall is not None
            wait(lambda: status(fallen)['state']['Phase'] == status(0)['state']['Phase'], 'observer terminal snapshot')
            core_terminal = time.monotonic()
            wait(lambda: not status(fallen)['finishingCombat'] and status(fallen)['hudMoment'] == 'Terminal', 'terminal presentation completes without a false future boundary')
            receipts.append(dict(players=2, fallWave=fall_wave, spectatorWallSecondsIncludingCapturePause=round(core_terminal-fall, 3),
                                 capturePauseSeconds=round(paused_after_fall, 3), acceleratedActiveSpectatorSeconds=round(core_terminal-fall-paused_after_fall, 3),
                                 presentationFinishDelaySeconds=round(time.monotonic()-core_terminal, 3),
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
