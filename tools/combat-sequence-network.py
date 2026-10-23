#!/usr/bin/env python3
"""Paid mixed-army ENet replay, then ordinary 1x combat and native local/overview capture."""
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
parser.add_argument('--players', type=int, choices=(2, 4, 6), default=6)
parser.add_argument('--width', type=int, choices=(1280, 1920), required=True)
parser.add_argument('--debug', action='store_true')
parser.add_argument('--output', type=Path, required=True)
args = parser.parse_args()
ROOT = Path(__file__).resolve().parent.parent
client_env = dict(os.environ, LIBGL_ALWAYS_SOFTWARE='1', COMMON_WATCH_TEST_AUDIO='dummy', COMMON_WATCH_STEAM_DISABLED='1')
args.output = args.output.resolve()
args.output.mkdir(parents=True, exist_ok=True)
processes, logs, samples = [], [], []
serial = 0
signal.signal(signal.SIGTERM, lambda *_: (_ for _ in ()).throw(SystemExit(143)))
with tempfile.TemporaryDirectory(prefix='common-watch-combat-review-') as temporary:
    work = Path(temporary)
    with socket.socket(socket.AF_INET, socket.SOCK_DGRAM) as sock:
        sock.bind(('127.0.0.1', 0))
        port = sock.getsockname()[1]

    def status(i=0):
        path = work / str(i) / 'control/status.json'
        return json.loads(path.read_text()) if path.exists() else None

    def wait(predicate, reason, seconds=75):
        deadline = time.monotonic() + seconds
        while time.monotonic() < deadline:
            result = predicate()
            if result:
                return result
            for i, process in enumerate(processes):
                if process.poll() is not None:
                    raise RuntimeError(f'Client {i} stopped: {reason}')
                if (current := status(i)) and current['nativeError']:
                    raise RuntimeError(current['nativeError'])
            time.sleep(.04)
        raise TimeoutError(reason)

    def request(i, kind, **values):
        global serial
        serial += 1
        path = work / str(i) / 'control' / f'request-{serial:06}.json'
        path.with_suffix('.tmp').write_text(json.dumps(dict(kind=kind, **values)))
        path.with_suffix('.tmp').replace(path)

    def send(i, action):
        previous = (status(i)['result'] or {}).get('Sequence', 0)
        request(i, 'send', action=action)
        def acknowledged():
            current = status(i)
            result = current['result']
            return result if result and result['Sequence'] > previous and not current['pending'] else None
        result = wait(acknowledged, action)
        assert result['Accepted'], result

    def measure(name):
        path = args.output / f'{name}.json'
        path.unlink(missing_ok=True)
        request(0, 'combat-metrics', path=str(path))
        wait(path.exists, 'actual rendered geometry')
        samples.append(dict(name=name, monotonic=time.monotonic(), data=json.loads(path.read_text())))

    def capture(name):
        path = args.output / f'{name}.png'
        path.unlink(missing_ok=True)
        request(0, 'capture', path=str(path))
        wait(path.exists, 'actual viewport still')
        measure(name)

    try:
        video_started = time.monotonic()
        for i in range(args.players):
            directory = work / str(i)
            (directory / 'control').mkdir(parents=True)
            log = open(args.output / f'client-{i}.log', 'w+')
            logs.append(log)
            command = ['bash', 'tools/record-client.sh', str(args.output / 'desktop.mkv')] if i == 0 else ['bash', 'tools/client.sh']
            command += ['--settings-path', str(directory / 'settings.json'), '--data-path', str(directory),
                        '--control-dir', str(directory / 'control'), '--test-fast-combat', 'yes',
                        '--gameplay-stop-wave', '7', '--gameplay-family', 'mixed',
                        '--test-match-id', f'combat-readability-{args.players}p-v1',
                        '--width', str(args.width), '--height', str(args.width * 9 // 16)]
            if args.debug:
                command += ['--world-art', 'debug', '--hud-art', 'plain', '--view', 'topdown', '--debug-overlay', 'off']
            command += ['--host', str(port)] if i == 0 else ['--join', f'127.0.0.1:{port}']
            processes.append(subprocess.Popen(command, cwd=ROOT, env=client_env, stdout=log, stderr=subprocess.STDOUT, start_new_session=True))
            wait(lambda: status(i) and status(i)['connected'], 'admission')
            send(i, 'doctrine:warden')
        send(0, 'start')
        request(0, 'hold-combat', wave=6, tick=1)
        for i in range(args.players):
            request(i, 'policy', enabled='yes')
        wait(lambda: (s := status())['state']['Wave'] == 6 and s['state']['Phase'] == 'Combat' and s['state']['Paused'], 'paid wave-six boundary', seconds=150)
        for i in range(args.players):
            request(i, 'policy', enabled='no')
        request(0, 'normal-combat')
        send(0, 'speed-1x')
        request(0, 'camera-native', key='F')
        capture('all-sectors-start')
        request(0, 'camera-native', key='R')
        capture('local-start')
        first = status()['state']
        start_tick = first['Tick']
        started = time.monotonic()
        send(0, 'resume')
        overview = False
        while status()['state']['Phase'] == 'Combat' or status()['finishingCombat']:
            if time.monotonic() - started > 90:
                raise TimeoutError('normal 1x encounter exceeded 90 seconds')
            measure(f'normal-{len(samples):03}')
            if not overview and status()['state']['Tick'] - start_tick >= 120:
                request(0, 'camera-native', key='F')
                capture('all-sectors-live')
                overview = True
            time.sleep(.2)
        end = status()['state']
        report = dict(players=args.players, width=args.width, debug=args.debug, wave=6, speed='1x', factor=.5,
                      startTick=start_tick, endTick=end['Tick'], simulationSeconds=(end['Tick'] - start_tick) / 60,
                      wallSeconds=time.monotonic() - started, normalVideoOffsetSeconds=started - video_started,
                      initial=first, final=end, samples=samples,
                      limitations='Software rendering, three stills, geometry sampling and optional desktop recording; no human or hardware qualification.')
        (args.output / 'sequence.json').write_text(json.dumps(report, indent=2))
        print(f'GODOT_COMBAT_NETWORK_SEQUENCE_PASS players={args.players} width={args.width} 1x factor=.5 ticks={end["Tick"] - start_tick} wall={report["wallSeconds"]:.2f}s', flush=True)
    finally:
        live = [p for p in reversed(processes) if p.poll() is None]
        for process in live:
            os.killpg(process.pid, signal.SIGCONT)
            os.killpg(process.pid, signal.SIGTERM)
        deadline = time.monotonic() + 7
        for process in live:
            try:
                process.wait(timeout=max(.01, deadline - time.monotonic()))
            except subprocess.TimeoutExpired:
                os.killpg(process.pid, signal.SIGKILL)
                process.wait(timeout=2)
        for log in logs:
            log.close()
