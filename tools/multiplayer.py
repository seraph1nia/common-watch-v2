#!/usr/bin/env python3
"""Supervise actual client processes, each with private preferences and resume data."""
import argparse
import json
import os
from pathlib import Path
import signal
import socket
import subprocess
import tempfile
import time

def terminate(signum, frame):
    raise SystemExit(128 + signum)

signal.signal(signal.SIGTERM, terminate)

ROOT = Path(__file__).resolve().parent.parent
parser = argparse.ArgumentParser()
parser.add_argument('operation', choices=['test', 'dev'])
parser.add_argument('--players', type=int, choices=[1, 2, 3, 4], default=2)
args = parser.parse_args()
processes = []
logs = []
with tempfile.TemporaryDirectory(prefix='common-watch-network-') as temporary:
    work = Path(temporary)
    with socket.socket(socket.AF_INET, socket.SOCK_DGRAM) as reservation:
        reservation.bind(('127.0.0.1', 0))
        port = reservation.getsockname()[1]
    try:
        for index in range(args.players):
            data = work / str(index)
            data.mkdir()
            log = open(work / f'{index}.log', 'w+')
            logs.append(log)
            command = ['bash', 'tools/client.sh', '--settings-path', str(data / 'settings.json'),
                       '--data-path', str(data), '--render-mode', 'debug']
            command += ['--host', str(port)] if index == 0 else ['--join', f'127.0.0.1:{port}']
            if args.operation == 'test':
                command += ['--network-test', str(args.players), '--report', str(work / f'{index}.json')]
                if index == 0:
                    command += ['--lobby-capture', str(ROOT / 'artifacts' / 'captures' / 'godot' / f'lobby-{args.players}p.png')]
            processes.append(subprocess.Popen(command, cwd=ROOT, stdout=log, stderr=subprocess.STDOUT, start_new_session=True))
            if index == 0:
                time.sleep(1)
        deadline = time.monotonic() + 150
        while True:
            if any(process.poll() is not None for process in processes):
                raise RuntimeError('A client exited before the exercise finished')
            if args.operation == 'test':
                if all((work / f'{index}.json').exists() for index in range(args.players)):
                    records = [json.loads((work / f'{index}.json').read_text()) for index in range(args.players)]
                    destination = ROOT / 'artifacts' / 'test-logs'
                    destination.mkdir(parents=True, exist_ok=True)
                    for index, record in enumerate(records):
                        (destination / f'godot-{args.players}p-{index}.json').write_text(json.dumps(record))
                    if not all(r['animation']['hits'] > 0 for r in records):
                        if time.monotonic() > deadline:
                            raise AssertionError('Every host and guest must consume real hit effects: ' + str([r['animation'] for r in records]))
                        time.sleep(.1)
                        continue
                    assert all(r['animation']['effects'] <= 4096 for r in records)
                    assert len({record['player'] for record in records}) == args.players
                    if not all(record['state'] == records[0]['state'] for record in records):
                        if time.monotonic() > deadline:
                            raise AssertionError('Authoritative snapshots did not converge')
                        time.sleep(.1)
                        continue
                    assert records[0]['state']['Wave'] == 2
                    assert len(records[0]['state']['Players']) == args.players
                    print(f'godot: {args.players} actual processes passed admission, paid opening, all-player readiness, combat, individual drafting and identical wave-two snapshots', flush=True)
                    break
                if time.monotonic() > deadline:
                    raise TimeoutError('Actual multiplayer exercise exceeded 150 seconds')
            time.sleep(.1)
    finally:
        for process in processes:
            if process.poll() is None:
                os.killpg(process.pid, signal.SIGTERM)
        cleanup_deadline = time.monotonic() + 3
        for process in processes:
            try:
                process.wait(timeout=max(.05, cleanup_deadline - time.monotonic()))
            except subprocess.TimeoutExpired:
                os.killpg(process.pid, signal.SIGKILL)
                process.wait(timeout=5)
        destination = ROOT / 'artifacts' / 'test-logs'
        destination.mkdir(parents=True, exist_ok=True)
        diagnostic_errors = []
        for index, log in enumerate(logs):
            log.flush()
            log.seek(0)
            transcript = log.read()
            (destination / f'godot-{args.players}p-{index}.log').write_text(transcript)
            if args.operation == 'test':
                print(transcript[-2000:])
            if 'ERROR:' in transcript or 'Unhandled exception' in transcript:
                diagnostic_errors.append(transcript)
            log.close()
        if diagnostic_errors:
            raise RuntimeError('Engine diagnostics during process verification:\n' + '\n'.join(diagnostic_errors))
