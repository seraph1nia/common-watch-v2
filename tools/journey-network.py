#!/usr/bin/env python3
"""Three continuous legal waves in real ENet clients, using native input for every normal action."""
import argparse
import json
import os
from pathlib import Path
import signal
import socket
import subprocess
import sys
import tempfile
import time

parser = argparse.ArgumentParser()
parser.add_argument('--players', type=int, choices=(2, 4, 6), default=2)
parser.add_argument('--width', type=int, choices=(1280, 1920), default=1280)
parser.add_argument('--debug', action='store_true')
parser.add_argument('--cooperative-focus', action='store_true')
parser.add_argument('--output', type=Path, required=True)
args = parser.parse_args()
ROOT = Path(__file__).resolve().parent.parent
sys.path.insert(0, str(ROOT / 'tools/preparation-review'))
from timing import PreparationTiming
timing = PreparationTiming()
args.output.mkdir(parents=True, exist_ok=True)
processes, logs, receipts = [], [], []
counts = [0] * args.players
serial = 0
signal.signal(signal.SIGTERM, lambda *_: (_ for _ in ()).throw(SystemExit(143)))
with tempfile.TemporaryDirectory(prefix='common-watch-journey-') as temporary:
    work = Path(temporary)
    with socket.socket(socket.AF_INET, socket.SOCK_DGRAM) as sock:
        sock.bind(('127.0.0.1', 0))
        port = sock.getsockname()[1]

    def status(i):
        path = work / str(i) / 'control/status.json'
        result = json.loads(path.read_text()) if path.exists() else None
        if result is not None:
            timing.observe(i, result)
        return result

    def wait(predicate, reason, seconds=60):
        deadline = time.monotonic() + seconds
        while time.monotonic() < deadline:
            result = predicate()
            if result:
                return result
            for i, process in enumerate(processes):
                if process.poll() is not None:
                    raise RuntimeError(f'Client {i} stopped: {reason}')
                current = status(i)
                if current and current.get('nativeError'):
                    raise RuntimeError(current['nativeError'])
            time.sleep(.04)
        raise TimeoutError(reason + ': ' + repr([(status(i) or {}).get('hudMoment') for i in range(len(processes))]))

    def request(i, kind, complete=True, **values):
        global serial
        serial += 1
        counts[i] += 1
        path = work / str(i) / 'control' / f'request-{serial:06}.json'
        path.with_suffix('.tmp').write_text(json.dumps(dict(kind=kind, **values)))
        path.with_suffix('.tmp').replace(path)
        wait(lambda: status(i) and status(i)['processedRequests'] >= counts[i], kind)
        if complete and kind in ('opening-step-native', 'doctrine-native', 'start-native', 'journey-overlay', 'team-details-native', 'focus-city-native', 'team-inspect-native', 'team-activate-native'):
            wait(lambda: not status(i)['nativeBusy'], kind + ' completed')
        assert not status(i).get('nativeError'), status(i).get('nativeError')

    def city(i):
        current = status(i)
        return next(p for p in current['state']['Players'] if p['Id'] == current['player'])

    def send(i, action):
        old = (status(i)['result'] or {}).get('Sequence', 0)
        request(i, 'send', action=action)
        def acknowledged():
            current = status(i)
            result = current['result']
            return result if result and result['Sequence'] > old and not current['pending'] else None
        result = wait(acknowledged, action)
        assert result['Accepted'], result

    def capture(name, i=0):
        with timing.measure('capture'):
            path = args.output.resolve() / f'{name}.png'
            path.unlink(missing_ok=True)
            request(i, 'capture', path=str(path))
            wait(path.exists, name)
            current = status(i)
            receipts.append(dict(name=name, client=i, match=current["state"]["MatchId"], seed=current["state"]["CombatSeed"], width=args.width, debug=args.debug, snapshot=current["state"], local=current['player'], viewed=current['viewed'],
                                 phase=current['state']['Phase'], moment=current['hudMoment'],
                                 receipt=current['rewardReceipt'], receiptCount=current['receiptCount'],
                                 stock=city(i)['Resources'], tick=current['state']['Tick'],
                                 speed=current['state']['CombatSpeed'], nativeActions=current['nativeActions'],
                                 teamHeading=current['teamHeading'], teamRows=current['teamRows'], teamWarnings=current.get('teamWarnings'), teamRect=current['teamRect'],
                                 cameraMode=current['cameraMode'], cameraFocusCity=current.get('cameraFocusCity'), cameraZoom=current['cameraZoom'], cameraPan=current['cameraPan'],
                                 qualification='Scripted native controls; paused captures and accelerated simulation are not human decision/waiting time.'))

    def camera_checks():
        # Real native gestures and distinct command/view/focus identities. No
        # resources, readiness or simulation state are edited by navigation.
        for i in sorted({0, 1, args.players - 1}):
            current = status(i); local = current['player']; target = 2 if local == 1 else 1
            stock = city(i)['Resources']; sequence = (current['result'] or {}).get('Sequence', 0)
            unit = next((u for u in city(i)['Soldiers'] if u['Health'] > 0 and u['Deployed']), None)
            if unit:
                request(i, 'inspect-combat', unitId=unit['Id'])
                wait(lambda: status(i)['liveVisible'], 'actual selected unit before city change')
            old_x = next(p['x'] for p in status(i)['cameraSectors'] if p['owner'] == local)
            request(i, 'team-pointer', owner=target)
            wait(lambda: status(i)['viewed'] == target and status(i)['cameraFocusCity'] == target and status(i)['cameraMode'] == 'Automatic', 'inspected ally and actual automatic focus agree')
            assert status(i)['player'] == local and 'read-only' in status(i)['readOnly'] and not status(i)['liveVisible']
            if local == 1:
                moved = next(p['x'] for p in status(i)['cameraSectors'] if p['owner'] == local)
                assert moved < old_x - 60, ('actual native canvas follows P2 rather than command owner P1', old_x, moved)
            assert city(i)['Resources'] == stock and (status(i)['result'] or {}).get('Sequence', 0) == sequence
            capture(f'focus-ally-local-{local}', i)
            request(i, 'camera-wheel-native'); request(i, 'camera-pan-native', dx=70, dy=30)
            wait(lambda: status(i)['cameraMode'] == 'Manual' and 'Manual' in status(i)['teamHeading'], 'manual mode is visible')
            pose = (status(i)['cameraZoom'], status(i)['cameraPan'])
            # Two genuine unrelated snapshots while paused, with the original
            # speed restored before any combat ticks resume.
            send(0, 'speed-2x'); send(0, 'speed-1x')
            wait(lambda: status(i)['state']['CombatSpeed'] == 'One', 'restored authoritative normal speed')
            assert status(i)['cameraMode'] == 'Manual' and (status(i)['cameraZoom'], status(i)['cameraPan']) == pose
            capture(f'manual-retained-local-{local}', i)
            request(i, 'focus-city-native')
            wait(lambda: status(i)['cameraMode'] == 'Automatic' and status(i)['cameraFocusCity'] == target, 'explicit Focus city restores inspected sector')
            auto = (status(i)['cameraZoom'], status(i)['cameraPan'])
            request(i, 'camera-native', key='F')
            wait(lambda: status(i)['cameraMode'] == 'Overview' and 'overview' in status(i)['teamHeading'], 'field overview explicitly named')
            capture(f'overview-local-{local}', i)
            request(i, 'camera-native', key='R')
            wait(lambda: status(i)['cameraMode'] == 'Automatic', 'native R restores current inspected sector')
            assert status(i)['cameraFocusCity'] == target and (status(i)['cameraZoom'], status(i)['cameraPan']) == auto
            request(i, 'team-details-native')
            wait(lambda: status(i)['teamExpanded'] and status(i)['teamTargetsFit'], 'bounded deliberate team details')
            capture(f'team-details-local-{local}', i)
            for page in ('Army', 'Worldtree'):
                request(i, 'team-inspect-native', page=page)
                wait(lambda: status(i)['detailsVisible'] and 'read-only' in status(i)['readOnly'], 'actual allied ' + page)
                if page == 'Army':
                    assert status(i)['homePlacementLocked'] and status(i)['homeTargetsFit']
                else:
                    assert status(i)['disabledResearch']
                capture(f'allied-{page.lower()}-local-{local}', i)
                request(i, 'cancel-native')
                wait(lambda: not status(i)['detailsVisible'], 'native allied inspector close')
            request(i, 'team-details-native')
            request(i, 'team-activate-native', owner=local, input='keyboard' if local == 1 else 'controller')
            wait(lambda: status(i)['viewed'] == local and status(i)['cameraFocusCity'] == local and status(i)['cameraMode'] == 'Automatic', 'native keyboard/controller return to local city')
            assert city(i)['Resources'] == stock
        print(f'GODOT_COOP_FOCUS_PASS players={args.players} width={args.width} local-ids=host/guest/final keyboard controller manual snapshot-retention overview reset read-only-inspectors', flush=True)

    def assert_reward_grant(i, before, quote):
        after = city(i)
        history = after['Run']['Rewards']
        assert len(history) == len(before['Run']['Rewards']) + 1
        receipt = history[-1]
        assert receipt['Wave'] == before['Run']['DraftWave'] and receipt['Id'] == quote['Id']
        assert receipt['Resources'] == quote['Resources']
        assert receipt['KeepHealing'] == quote['KeepHealing']
        assert receipt['SoldierHealing'] == sum(h['Amount'] for h in quote['Soldiers'])
        assert receipt['SoldierCount'] == len(quote['Soldiers'])
        assert receipt['ResourceCapped'] == quote['ResourceCapped']
        for resource in ('Gold', 'Wood', 'Food', 'Stone', 'Metal', 'Cloth'):
            assert after['Resources'][resource] - before['Resources'][resource] == quote['Resources'][resource]
        assert after['Health'] - before['Health'] == quote['KeepHealing']
        assert after['Research']['Points'] == before['Research']['Points'], 'chosen supplies do not grant Research'
        healing = {h['UnitId']: h['Amount'] for h in quote['Soldiers']}
        members = {u['Id']: u for u in after['Soldiers']}
        for member in before['Soldiers']:
            assert members[member['Id']]['Health'] == member['Health'] + healing.get(member['Id'], 0)
            assert members[member['Id']]['Profile'] == member['Profile'], 'consumed loot is not a permanent modifier'

    def choose(i):
        before = city(i)
        quote = before['Run']['Quotes'][0]
        old = status(i)['receiptCount']
        request(i, 'reward-pointer', index=0)
        wait(lambda: city(i)['Run']['Choice'] is not None and not status(i)['pending'], 'accepted reward state')
        wait(lambda: status(i)['receiptCount'] == old + 1, 'one observed reward receipt')
        assert not status(i)['draftVisible']
        assert_reward_grant(i, before, quote)
        return old + 1

    try:
        for i in range(args.players):
            directory = work / str(i)
            (directory / 'control').mkdir(parents=True)
            log = open(work / f'{i}.log', 'w+')
            logs.append(log)
            command = ['bash', 'tools/client.sh', '--settings-path', str(directory / 'settings.json'),
                       '--data-path', str(directory), '--control-dir', str(directory / 'control'),
                       '--test-fast-combat', 'yes', '--width', str(args.width), '--height', str(args.width * 9 // 16)]
            if args.debug:
                command += ['--world-art', 'debug', '--hud-art', 'plain', '--view', 'topdown', '--debug-overlay', 'off']
            command += ['--host', str(port)] if i == 0 else ['--join', f'127.0.0.1:{port}']
            processes.append(subprocess.Popen(command, cwd=ROOT, stdout=log, stderr=subprocess.STDOUT, start_new_session=True))
            wait(lambda: status(i) and status(i)['connected'], 'admission')
        wait(lambda: len(status(0)['state']['Players']) == args.players, 'complete roster')
        for i in range(args.players):
            request(i, 'doctrine-native', index=0)
        wait(lambda: not status(0)['doctrineStartDisabled'], 'all actual personal doctrine choices acknowledged')
        request(0, 'start-native')
        wait(lambda: all(status(i)['state']['Phase'] == 'Building' for i in range(args.players)), 'ordinary start')
        for wave in range(1, 4):
            for stage in range(1, 4):
                for i in range(args.players):
                    request(i, 'team-pointer', owner=status(i)['player'])
                    request(i, 'opening-step-native', complete=False)
                wait(lambda: all(not status(i)['nativeBusy'] and not status(i)['pending'] for i in range(args.players)), 'paid native preparation')
                capture(f'w{wave}-prep-{stage}')
                for i in range(args.players - 1):
                    request(i, 'ready-pointer')
                    wait(lambda: city(i)['Ready'] and not status(i)['pending'], 'local Ready acknowledgment')
                wait(lambda: status(0)['hudMoment'] == 'ReadyWaiting', 'ready waiting surface')
                if wave == 1 and stage == 1:
                    stock = city(0)['Resources']
                    request(0, 'ready-pointer')
                    wait(lambda: not city(0)['Ready'] and not status(0)['pending'], 'native Unready')
                    assert city(0)['Resources'] == stock
                    # A ready guest loses transport while another city still
                    # prepares. Re-admission must baseline the current state,
                    # without spending or replaying a historical transition.
                    guest = args.players - 1
                    request(guest, 'ready-pointer')
                    wait(lambda: city(guest)['Ready'] and status(guest)['hudMoment'] == 'ReadyWaiting' and not status(guest)['pending'], 'guest ready before disconnect')
                    guest_stock = city(guest)['Resources']
                    capture('ready-guest-before-disconnect', guest)
                    request(guest, 'leave')
                    wait(lambda: status(guest)['state'] is None, 'ready guest disconnects')
                    wait(lambda: not status(0)['state']['Players'][guest]['Connected'], 'authority observes ready guest disconnect')
                    request(guest, 'reconnect')
                    wait(lambda: status(guest)['connected'] and status(guest)['state']['Phase'] == 'Building', 'ready-wait reconnect baseline')
                    assert status(guest)['state']['Turn'] == 1 and city(guest)['Resources'] == guest_stock
                    assert status(guest)['receiptCount'] == 0 and not status(guest)['draftVisible']
                    if city(guest)['Ready']:
                        request(guest, 'ready-pointer')
                        wait(lambda: not city(guest)['Ready'] and not status(guest)['pending'], 'rejoined guest resumes preparing')
                    capture('ready-wait-reconnected', guest)
                    request(0, 'ready-pointer')
                    wait(lambda: city(0)['Ready'] and not status(0)['pending'], 'ready again')
                    capture('ready-unready-waiting')
                if stage == 3:
                    request(0, 'journey-overlay', overlay=('army', 'worldtree', 'settings')[wave - 1])
                    request(0, 'hold-combat', wave=wave, tick=60)
                request(args.players - 1, 'ready-pointer')
                if stage < 3:
                    wait(lambda: all(status(i)['state']['Turn'] == stage + 1 and not city(i)['Ready'] for i in range(args.players)), 'one accepted production boundary')
            wait(lambda: status(0)['state']['Wave'] == wave and status(0)['state']['Phase'] == 'Combat' and status(0)['state']['Paused'], 'real combat capture boundary')
            wait(lambda: all(status(i)['hudMoment'] == 'Combat' for i in range(args.players)), 'combat presentation')
            assert all(not status(i)['detailsVisible'] for i in range(args.players))
            if wave == 3:
                assert status(0)['settingsVisible']
                capture('live-settings-exception')
                request(0, 'cancel-native')
                wait(lambda: not status(0)['settingsVisible'], 'native Settings cancel')
            capture(f'w{wave}-combat')
            if args.cooperative_focus and wave == 1:
                camera_checks()
            if wave == 1:
                request(1, 'hold-presentation', enabled='yes')
                wait(lambda: status(1)['presentationHeld'], 'slow client holds accepted playback while RPC receipt continues')
                held_tick = status(1)['playbackTick']
            send(0, 'resume')
            wait(lambda: all(status(i)['state']['Phase'] == 'RewardDraft' for i in range(args.players)), 'authoritative outcome')
            if wave == 1:
                wait(lambda: status(1)['finishingCombat'], 'slow client still finishing actual playback')
                assert status(1)['playbackTick'] == held_tick and status(1)['state']['Tick'] > held_tick
                wait(lambda: 'Finishing' in status(1)['teamRows'][1], 'local team row reaches the presented finishing boundary')
                assert not status(1)['draftVisible']
                capture('slow-client-finishing', 1)
                request(1, 'hold-presentation', enabled='no')
            wait(lambda: all(not status(i)['finishingCombat'] and status(i)['draftVisible'] for i in range(args.players)), 'all actual playback boundaries finished')
            capture(f'w{wave}-reward')
            last = 0 if wave == 2 else 1
            for i in range(args.players):
                if i != last:
                    choose(i)
            first = 1 if last == 0 else 0
            wait(lambda: status(first)['hudMoment'] == 'RewardWaiting', 'accepted waiting')
            assert not status(first)['readyVisible'] and not status(first)['previewVisible']
            capture(f'w{wave}-accepted-waiting', first)
            if wave == 2:
                # Reconnect an already-chosen guest while another real player still chooses.
                chosen = city(1)['Run']['Choice']
                request(1, 'leave')
                wait(lambda: status(1)['state'] is None, 'guest leaves explicitly')
                wait(lambda: not status(0)['state']['Players'][1]['Connected'], 'authority observes chosen guest disconnect')
                request(1, 'reconnect')
                wait(lambda: status(1)['connected'] and status(1)['state']['Phase'] == 'RewardDraft', 'private reconnect to actual draft')
                wait(lambda: status(1)['hudMoment'] == 'RewardWaiting', 'current chosen baseline')
                assert city(1)['Run']['Choice'] == chosen and status(1)['receiptCount'] == 0 and not status(1)['draftVisible']
                capture('reconnected-accepted-baseline', 1)
            if wave == 1:
                with timing.measure('deliberate-test-delay'):
                    os.killpg(processes[0].pid, signal.SIGSTOP)
                    try:
                        count = status(last)['receiptCount']
                        delayed_before = city(last)
                        delayed_quote = delayed_before['Run']['Quotes'][0]
                        request(last, 'reward-pointer', index=0)
                        wait(lambda: status(last)['pending'], 'real delayed acknowledgment')
                        assert status(last)['draftVisible'] and status(last)['receiptCount'] == count
                        capture('pending-choice-before-authority', last)
                    finally:
                        os.killpg(processes[0].pid, signal.SIGCONT)
                wait(lambda: city(last)['Run']['Choice'] is not None and not status(last)['pending'], 'delayed choice accepted')
                wait(lambda: status(last)['receiptCount'] == count + 1, 'one delayed receipt')
                assert_reward_grant(last, delayed_before, delayed_quote)
            else:
                choose(last)
            wait(lambda: all(status(i)['state']['Phase'] == 'Building' and status(i)['state']['Wave'] == wave + 1 for i in range(args.players)), 'next preparation only on authority')
            assert all(not status(i)['draftVisible'] and not status(i)['detailsVisible'] for i in range(args.players))
            capture(f'w{wave}-next-planning')
        print(f'GODOT_JOURNEY_NETWORK_PASS players={args.players} width={args.width} debug={args.debug} waves=3 native-purchases ready/unready overlays finishing pending accepted-waiting reconnect', flush=True)
    finally:
        (args.output / 'timing.json').write_text(json.dumps(timing.report(), indent=2))
        for process in reversed(processes):
            if process.poll() is None:
                os.killpg(process.pid, signal.SIGCONT)
                os.killpg(process.pid, signal.SIGTERM)
                try:
                    process.wait(timeout=5)
                except subprocess.TimeoutExpired:
                    os.killpg(process.pid, signal.SIGKILL)
                    process.wait(timeout=5)
        diagnostics = []
        for i, log in enumerate(logs):
            log.seek(0)
            transcript = log.read()
            (args.output / f'client-{i}.log').write_text(transcript)
            if 'ERROR:' in transcript or 'Unhandled exception' in transcript:
                diagnostics.append(transcript[-4000:])
            log.close()
        (args.output / 'receipts.json').write_text(json.dumps(receipts, indent=2))
        if diagnostics:
            raise RuntimeError('\n'.join(diagnostics))
