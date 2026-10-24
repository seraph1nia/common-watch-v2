#!/usr/bin/env python3
"""Bounded real two-client phase inventory; legal purchases, native Ready and actual viewport captures."""
import argparse
import json
import os
from pathlib import Path
import signal
import socket
import subprocess
import tempfile
import time

parser = argparse.ArgumentParser()
parser.add_argument('--baseline', action='store_true')
parser.add_argument('--output', type=Path, required=True)
args = parser.parse_args()
ROOT = Path(__file__).resolve().parent.parent
args.output.mkdir(parents=True, exist_ok=True)
processes, logs, receipts = [], [], []
serial = 0
signal.signal(signal.SIGTERM, lambda *_: (_ for _ in ()).throw(SystemExit(143)))
with tempfile.TemporaryDirectory(prefix='common-watch-hud-') as temporary:
    work = Path(temporary)
    with socket.socket(socket.AF_INET, socket.SOCK_DGRAM) as sock:
        sock.bind(('127.0.0.1', 0))
        port = sock.getsockname()[1]

    def status(i):
        path = work / str(i) / 'control/status.json'
        return json.loads(path.read_text()) if path.exists() else None

    def wait(predicate, reason, seconds=45):
        deadline = time.monotonic() + seconds
        while time.monotonic() < deadline:
            value = predicate()
            if value:
                return value
            if any(p.poll() is not None for p in processes):
                raise RuntimeError('Client stopped: ' + reason)
            time.sleep(.06)
        raise TimeoutError(reason)

    def control(i, kind, **values):
        global serial
        serial += 1
        path = work / str(i) / 'control' / f'request-{serial:06}.json'
        path.with_suffix('.tmp').write_text(json.dumps(dict(kind=kind, **values)))
        path.with_suffix('.tmp').replace(path)
        wait(lambda: status(i) and status(i)['processedRequests'] >= counts[i], kind)

    counts = [0, 0]
    def request(i, kind, **values):
        counts[i] += 1
        control(i, kind, **values)

    def city(i):
        current = status(i)
        return next(p for p in current['state']['Players'] if p['Id'] == current['player'])

    def send(i, action, **values):
        old = (status(i)['result'] or {}).get('Sequence', 0)
        request(i, 'send', action=action, **values)
        result = wait(lambda: status(i)['result'] if status(i)['result'] and status(i)['result']['Sequence'] > old and not status(i)['pending'] else None, action)
        assert result['Accepted'], result

    def capture(name, i=0):
        for width, height in ((1280, 720), (1920, 1080)):
            request(i, 'resize', width=width, height=height)
            wait(lambda: status(i)['viewportWidth'] == width, 'resize')
            path = args.output.resolve() / f'{name}-{width}.png'
            path.unlink(missing_ok=True)
            request(i, 'capture', path=str(path))
            wait(path.exists, name)
            current = status(i)
            receipts.append(dict(name=name, width=width, phase=current['state']['Phase'],
                                 moment=current.get('hudMoment'), regions=current.get('hudRegions'),
                                 local=city(i), notices=current['hudNotice']))
        request(i, 'resize', width=1280, height=720)

    try:
        for i in range(2):
            directory = work / str(i)
            (directory / 'control').mkdir(parents=True)
            log = open(work / f'{i}.log', 'w+')
            logs.append(log)
            command = ['bash', 'tools/client.sh', '--settings-path', str(directory / 'settings.json'),
                       '--data-path', str(directory), '--control-dir', str(directory / 'control'),
                       '--test-fast-combat', 'yes']
            command += ['--host', str(port)] if i == 0 else ['--join', f'127.0.0.1:{port}']
            processes.append(subprocess.Popen(command, cwd=ROOT, stdout=log, stderr=subprocess.STDOUT, start_new_session=True))
            wait(lambda: status(i) and status(i)['connected'], 'connected')
        wait(lambda: len(status(0)['state']['Players']) == 2, 'roster')
        for i in range(2):
            send(i, 'doctrine:warden')
        send(0, 'start')
        wait(lambda: status(1)['state']['Phase'] == 'Building', 'start propagated')
        capture('planning')
        for i in range(2):
            for slot, building in enumerate(('Farm', 'Barracks', 'MetalMine', 'Lumbermill')):
                send(i, 'build', slot=slot, building=building, payment='GoldRecovery' if building == 'Lumbermill' else 'Standard')
        for stage in range(1, 4):
            if stage > 1:
                for i in range(2):
                    for _ in range(2):
                        send(i, 'recruit', slot=1)
            request(0, 'ready-pointer')
            wait(lambda: city(0)['Ready'], 'local native Ready acknowledged')
            if stage == 1:
                capture('ready-waiting')
                if not args.baseline:
                    assert status(0)['hudMoment'] == 'ReadyWaiting'
                    assert 'selected-plot' not in status(0)['hudRegions']
            request(1, 'ready-pointer')
            if stage < 3:
                wait(lambda: status(0)['state']['Turn'] == stage + 1 and not city(0)['Ready'], 'production')
        wait(lambda: status(0)['state']['Phase'] == 'Combat', 'battle')
        send(0, 'pause')
        capture('combat')
        if not args.baseline:
            assert not set(status(0)['hudRegions']) & {'ledger', 'threat', 'guide', 'selected-plot', 'navigation/build', 'preparation-track'}
            assert status(0)['hudFocusVisible'], 'hidden Ready restores visible combat focus'
        send(0, 'resume')
        wait(lambda: all(status(i)['state']['Phase'] == 'RewardDraft' and not status(i)['finishingCombat'] for i in range(2)), 'reward boundary')
        capture('choosing-reward')
        reward = city(0)['Run']['Offers'][0]
        if args.baseline:
            send(0, 'reward:' + reward)
        else:
            request(0, 'reward-pointer', index=0)
            wait(lambda: city(0)['Run']['Choice'] == reward and not status(0)['pending'], 'reward acknowledgment')
        capture('reward-waiting')
        if not args.baseline:
            assert status(0)['hudMoment'] == 'RewardWaiting'
            assert not status(0)['draftVisible']
            assert not status(0)['readyVisible']
            assert status(0)['hudFocusVisible'], 'hidden chooser restores visible waiting focus'
        send(1, 'reward:' + city(1)['Run']['Offers'][0])
        wait(lambda: status(0)['state']['Wave'] == 2 and status(0)['state']['Phase'] == 'Building', 'next preparation')
        capture('next-planning')
        print('GODOT_HUD_PHASES_PASS native-ready combat choosing accepted-waiting next-planning sizes=1280/1920', flush=True)
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
            (args.output / f'client-{index}.log').write_text(transcript)
            if 'ERROR:' in transcript or 'Unhandled exception' in transcript:
                diagnostics.append(transcript[-4000:])
            log.close()
        (args.output / 'receipts.json').write_text(json.dumps(receipts, indent=2))
        if diagnostics:
            raise RuntimeError('\n'.join(diagnostics))
