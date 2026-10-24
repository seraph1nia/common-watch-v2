#!/usr/bin/env python3
# SPDX-License-Identifier: GPL-3.0-only
"""Real remote finite recovery: held authority, cancel, exact retry and private rejoin."""
import json
import os
from pathlib import Path
import signal
import socket
import subprocess
import tempfile
import time

ROOT = Path(__file__).resolve().parent.parent
processes, logs, serial = [], [], 0
signal.signal(signal.SIGTERM, lambda *_: (_ for _ in ()).throw(SystemExit(143)))
with tempfile.TemporaryDirectory(prefix='common-watch-recovery-') as temporary:
    work = Path(temporary)
    with socket.socket(socket.AF_INET, socket.SOCK_DGRAM) as sock:
        sock.bind(('127.0.0.1', 0)); port = sock.getsockname()[1]
    def status(i):
        p = work / str(i) / 'control/status.json'
        return json.loads(p.read_text()) if p.exists() else None
    def city(i):
        s = status(i)
        return next(p for p in s['state']['Players'] if p['Id'] == s['player'])
    def wait(test, label, seconds=60):
        end = time.monotonic() + seconds
        while time.monotonic() < end:
            result = test()
            if result: return result
            for i, process in enumerate(processes):
                if process.poll() is not None: raise RuntimeError(f'client {i} exited during {label}: {process.returncode}')
                s = status(i)
                if s and s.get('nativeError'): raise RuntimeError(s['nativeError'])
            time.sleep(.05)
        raise TimeoutError(label)
    def control(i, kind, **values):
        global serial
        serial += 1
        p = work / str(i) / 'control' / f'request-{serial:06}.json'
        p.with_suffix('.tmp').write_text(json.dumps(dict(kind=kind, **values))); p.with_suffix('.tmp').replace(p)
    def send(i, action, **values):
        old = status(i).get('result'); sequence = old['Sequence'] if old else 0
        control(i, 'send', action=action, **values)
        def acknowledged():
            s = status(i)
            return s['result'] if s['result'] and s['result']['Sequence'] > sequence and not s['pending'] else None
        result = wait(acknowledged, action)
        assert result['Accepted'], result
    def native(i, kind, **values):
        count = status(i)['processedRequests']; control(i, kind, **values)
        wait(lambda: status(i)['processedRequests'] > count and not status(i)['nativeBusy'], kind)
    stopped = False
    try:
        for i in range(2):
            d = work / str(i); (d / 'control').mkdir(parents=True)
            log = open(work / f'{i}.log', 'w+'); logs.append(log)
            command = ['bash', 'tools/client.sh', '--settings-path', str(d/'settings.json'), '--data-path', str(d),
                       '--control-dir', str(d/'control'), '--test-fast-combat', 'yes', '--gameplay-stop-wave', '8', '--gameplay-family', 'reserve',
                       '--world-art', 'sprites', '--hud-art', 'illustrated', '--view', 'angled', '--width', '1280', '--height', '720']
            command += ['--host', str(port)] if i == 0 else ['--join', f'127.0.0.1:{port}']
            processes.append(subprocess.Popen(command, cwd=ROOT, stdout=log, stderr=subprocess.STDOUT, start_new_session=True))
            wait(lambda: status(i) and status(i)['connected'], 'admission')
        wait(lambda: len(status(0)['state']['Players']) == 2, 'roster')
        for i in range(2):
            offers = wait(lambda: status(i)['doctrineOffers'], 'visible doctrine offers')
            choice = 'warden' if 'warden' in offers else next(d for d in offers if d != 'merchant')
            send(i, 'doctrine:' + choice)
        send(0, 'start')
        for i in range(2): control(i, 'policy', enabled='yes')
        wait(lambda: all(status(i)['state']['Wave'] == 8 and status(i)['state']['Phase'] == 'Building' for i in range(2)), 'earned wave-eight checkpoint', seconds=150)
        for i in range(2): control(i, 'policy', enabled='no')
        wait(lambda: all(not status(i)['pending'] for i in range(2)), 'policy drained')
        c = city(1); hall = c['Army']['Halls'][0]
        groups = {}
        for u in c['Soldiers']:
            if not u['Assignment']['Stored'] and u['Health'] < u['Profile']['Health']: groups.setdefault(u['SquadId'], []).append(u)
        squad, wounded = max(groups.items(), key=lambda pair: len(pair[1]))
        assert len(wounded) >= 2, 'need a real multi-member wound group'
        occupied = sum(u['Assignment']['Stored'] and u['Assignment']['HallSlot'] == hall['Slot'] for u in c['Soldiers'])
        if hall['Capacity'] - occupied < len(wounded):
            send(1, 'upgrade-capacity', slot=hall['Slot'])
            wait(lambda: next(h for h in city(1)['Army']['Halls'] if h['Slot'] == hall['Slot'])['Capacity'] >= occupied + len(wounded), 'earned hall capacity upgrade')
        wait(lambda: status(1)['hudMoment'] == 'Planning', 'local playback reaches planning')
        native(1, 'recovery-review-native', squadId=squad)
        before = city(1); old_stored = {u['Id'] for u in before['Soldiers'] if u['Assignment']['Stored']}
        os.kill(processes[0].pid, signal.SIGSTOP); stopped = True
        native(1, 'recovery-commit-native')
        wait(lambda: status(1)['pending'], 'first remote member pending')
        native(1, 'recovery-cancel-native')
        os.kill(processes[0].pid, signal.SIGCONT); stopped = False
        wait(lambda: not status(1)['pending'] and status(1)['recoveryCompleted'] == 1, 'cancelled accepted prefix observed')
        after = city(1); stored = {u['Id'] for u in after['Soldiers'] if u['Assignment']['Stored']}
        assert len(stored - old_stored) == 1
        assert after['Resources'] == before['Resources']
        for u in after['Soldiers']:
            previous = next(v for v in before['Soldiers'] if v['Id'] == u['Id'])
            assert (u['Health'],u['Level'],u['Experience']) == (previous['Health'],previous['Level'],previous['Experience'])
        result = status(1)['result']; processed = status(1)['processedRequests']; control(1, 'retry')
        wait(lambda: status(1)['processedRequests'] > processed, 'retry request consumed')
        wait(lambda: not status(1)['pending'] and status(1)['result']['Sequence'] == result['Sequence'], 'exact member retry')
        assert {u['Id'] for u in city(1)['Soldiers'] if u['Assignment']['Stored']} == stored
        control(1, 'leave'); wait(lambda: status(1)['state'] is None, 'leave')
        control(1, 'reconnect'); wait(lambda: status(1)['connected'] and status(1)['state'] and status(1)['state']['Wave'] == 8, 'private rejoin')
        assert {u['Id'] for u in city(1)['Soldiers'] if u['Assignment']['Stored']} == stored
        native(1, 'recovery-review-native', squadId=squad)
        assert status(1)['recoveryTotal'] == 0  # Review has not committed a new sequence.
        assert f'{len(wounded)-1}/{len(wounded)-1}' in status(1)['recoveryText']
        native(1, 'recovery-commit-native')
        wait(lambda: not status(1)['pending'] and status(1)['recoveryCompleted'] == len(wounded)-1, 'remaining members only')
        ready_before = city(1)
        for i in range(2): send(i, 'ready')
        wait(lambda: status(1)['state']['Turn'] == 2, 'real production after rejoin')
        for u in city(1)['Soldiers']:
            old = next(v for v in ready_before['Soldiers'] if v['Id'] == u['Id'])
            if old['Assignment']['Stored']:
                expected = min(old['Profile']['Health'], old['Health'] + (old['Profile']['Health'] * hall['HealingPercent'] + 99)//100) if old.get('RecoveryEligible') else old['Health']
                assert u['Health'] == expected
        output = ROOT / 'artifacts/captures/recovery/network'; output.mkdir(parents=True, exist_ok=True)
        (output/'receipt.json').write_text(json.dumps(dict(before=before,afterCancel=after,afterRejoin=ready_before,afterPayment=city(1)),indent=2))
        print('GODOT_RECOVERY_NETWORK_PASS real-wounds cancel-pending one-accepted-prefix exact-retry private-rejoin remaining-only actual-healing')
    finally:
        if stopped: os.kill(processes[0].pid, signal.SIGCONT)
        for process in reversed(processes):
            if process.poll() is None:
                os.killpg(process.pid, signal.SIGTERM)
                try: process.wait(timeout=5)
                except subprocess.TimeoutExpired: os.killpg(process.pid, signal.SIGKILL); process.wait(timeout=5)
        target = ROOT/'artifacts/test-logs/recovery-network.log'; target.parent.mkdir(parents=True,exist_ok=True)
        with target.open('w') as out:
            for log in logs:
                log.seek(0); out.write(log.read()); log.close()
