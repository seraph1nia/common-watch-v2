#!/usr/bin/env python3
"""Real-process LAN reconnection/fault checks. No authority state is manufactured."""
import json
import os
from pathlib import Path
import signal
import socket
import subprocess
import tempfile
import time
import uuid

def terminate(signum, frame):
    raise SystemExit(128 + signum)

signal.signal(signal.SIGTERM, terminate)

ROOT = Path(__file__).resolve().parent.parent
processes = []
logs = []
serial = 0
with tempfile.TemporaryDirectory(prefix='common-watch-reconnect-') as temporary:
    work = Path(temporary)
    with socket.socket(socket.AF_INET, socket.SOCK_DGRAM) as reservation:
        reservation.bind(('127.0.0.1', 0))
        port = reservation.getsockname()[1]

    def launch(index, extra=()):
        data = work / str(index)
        data.mkdir(exist_ok=True)
        control = data / 'control'
        control.mkdir(exist_ok=True)
        (control / 'status.json').unlink(missing_ok=True)
        log = open(work / f'{index}-{len(logs)}.log', 'w+')
        logs.append(log)
        command = ['bash', 'tools/client.sh', '--settings-path', str(data / 'settings.json'),
                   '--data-path', str(data), '--control-dir', str(control), '--test-fast-combat', 'yes',
                   '--world-art', 'debug', '--hud-art', 'plain', '--view', 'topdown', '--debug-overlay', 'off']
        command += ['--host', str(port), '--pause-at-death', 'yes'] if index == 0 else ['--join', f'127.0.0.1:{port}']
        command += list(extra)
        process = subprocess.Popen(command, cwd=ROOT, stdout=log, stderr=subprocess.STDOUT, start_new_session=True)
        processes.append(process)
        return process

    def status(index):
        path = work / str(index) / 'control' / 'status.json'
        return json.loads(path.read_text()) if path.exists() else None

    def wait(predicate, message, seconds=20):
        deadline = time.monotonic() + seconds
        while time.monotonic() < deadline:
            result = predicate()
            if result:
                return result
            if any(p.poll() not in (None, 0, -signal.SIGTERM) for p in processes):
                raise RuntimeError('Client failure: ' + message + '; process exits=' + repr([(p.pid, p.poll()) for p in processes]))
            time.sleep(.08)
        states = [(i, current['status'] if (current := status(i)) else 'no status') for i in range(8)]
        raise TimeoutError(message + ': ' + repr(states))

    def control(index, kind, **values):
        global serial
        serial += 1
        path = work / str(index) / 'control' / f'request-{serial:06}.json'
        path.with_suffix('.tmp').write_text(json.dumps(dict(kind=kind, **values)))
        path.with_suffix('.tmp').replace(path)

    def acknowledgment(index, after_sequence=0):
        # The client atomically replaces status.json; retain one snapshot per poll.
        current = status(index)
        result = current['result'] if current else None
        return result if result and result['Sequence'] > after_sequence and not current['pending'] else None

    def send(index, action, accepted=True, **values):
        previous = status(index)['result']
        sequence = previous['Sequence'] if previous else 0
        control(index, 'send', action=action, **values)
        result = wait(lambda: acknowledgment(index, sequence), 'Command acknowledgment: ' + action)
        assert result['Accepted'] == accepted, result

    def phase(expected, turn=None):
        wait(lambda: all((current := status(i)) and (state := current['state']) and state['Phase'] == expected and
                         (turn is None or state['Turn'] == turn) for i in (0, 1)), expected)

    def reconnect(expected_phase):
        before = status(1)
        player = before['player']
        match = before['state']['MatchId']
        last_sequence = before['result']['Sequence'] if before['result'] else 0
        control(1, 'leave')
        wait(lambda: status(1)['state'] is None, 'Guest returns to menu')
        wait(lambda: not next(p for p in status(0)['state']['Players'] if p['Id'] == player)['Connected'], 'Authority observes disconnect')
        control(1, 'reconnect')
        after = wait(lambda: (current := status(1)) and current['connected'] and current, 'Resume admission')
        assert after['player'] == player and after['state']['MatchId'] == match
        assert after['state']['Phase'] == expected_phase
        saved = json.loads((work / '1' / 'resume.json').read_text())
        assert saved['NextSequence'] > last_sequence
        if os.name != 'nt':
            assert (work / '1' / 'resume.json').stat().st_mode & 0o777 == 0o600
        print('godot', 'reconnected during', expected_phase, flush=True)

    try:
        host = launch(0)
        wait(lambda: (current := status(0)) and current['connected'], 'Host')
        guest = launch(1)
        wait(lambda: (current := status(1)) and current['connected'], 'Guest admission')
        phase('Lobby')
        # Ownership and duplicate handling use actual remote delivery.
        control(1, 'probe', action='doctrine:merchant', owner=1, duplicate='yes')
        result = wait(lambda: acknowledgment(1), 'Unauthorized acknowledgment')
        assert not result['Accepted']
        assert 'own' in result['Message']
        control(1, 'probe', action='doctrine:warden', owner=2, duplicate='yes')
        result = wait(lambda: acknowledgment(1, result['Sequence']), 'Duplicate acknowledgment')
        assert result['Accepted'], result
        # Protocol mismatch is rejected before any city is admitted.
        bad = launch(2, ['--core-version', '-1'])
        wait(lambda: (current := status(2)) and 'version' in current['status'].lower(), 'Protocol refusal')
        refused = status(2)
        assert not refused['connected'] and refused['state'] is None
        control(2, 'quit')
        bad.wait(timeout=8)
        send(1, 'start', accepted=False)
        send(0, 'start')
        phase('Building', 1)
        reconnect('Building')
        # A restarted client reuses its retained city and continues the sequence.
        control(1, 'quit')
        guest.wait(timeout=8)
        wait(lambda: not status(0)['state']['Players'][1]['Connected'], 'Restart disconnect')
        guest = launch(1)
        wait(lambda: (current := status(1)) and current['connected'], 'Process restart resume')
        assert status(1)['player'] == 2
        for index in (0, 1):
            for slot, building in enumerate(['Farm', 'Barracks', 'MetalMine', 'Lumbermill']):
                send(index, 'build', slot=slot, building=building, payment='GoldRecovery' if slot == 3 else 'Standard')
        for turn in (2, 3):
            send(0, 'ready')
            send(1, 'ready')
            phase('Building' if turn == 2 else 'Preparation', turn)
            for index in (0, 1):
                send(index, 'recruit', slot=1)
                send(index, 'recruit', slot=1)
        wait(lambda: all(len(status(i)['state']['Players'][1]['Soldiers']) == 4 for i in (0, 1)), 'Authoritative recruitment baseline before retry')
        accepted = status(1)['result']['Sequence']
        city = status(1)['state']['Players'][1]
        before_retry = (city['Resources'], [u['Id'] for u in city['Soldiers']])
        reconnect('Preparation')
        control(1, 'retry')
        result = wait(lambda: acknowledgment(1), 'Accepted command retry after reconnect')
        assert result['Accepted'] and result['Sequence'] == accepted
        city = status(1)['state']['Players'][1]
        assert (city['Resources'], [u['Id'] for u in city['Soldiers']]) == before_retry
        send(0, 'ready')
        send(1, 'ready')
        phase('Combat')
        wait(lambda: status(0)['state']['Paused'] and status(0)['state']['DyingBodies'], 'Authoritative pause during real death animation')
        tick = status(0)['state']['Tick']
        resets = status(1)['animation']['resets']
        reconnect('Combat')
        wait(lambda: status(1)['animation']['resets'] > resets and status(1)['animation']['dying'] > 0, 'Guest baselines and renders remaining death lifecycle')
        assert status(1)['animation']['hits'] == 0, 'Reconnect must not replay historical damage effects'
        assert status(0)['state']['Tick'] == tick, 'Paused authority must not advance'
        send(0, 'resume')
        phase('RewardDraft', None)
        reconnect('RewardDraft')
        for index in (0, 1):
            offer = status(index)['state']['Players'][index]['Run']['Offers'][0]
            send(index, 'reward:' + offer)
        phase('Building', 1)
        assert status(0)['state']['Wave'] == 2
        reconnect('Building')
        # Invalid credentials and expired match cannot create additional cities.
        original = (work / '1' / 'resume.json').read_text()
        control(1, 'leave')
        wait(lambda: status(1)['state'] is None, 'Leave before invalid resume')
        for field, value in [('Token', '0' * 64), ('MatchId', uuid.uuid4().hex)]:
            saved = json.loads(original)
            saved[field] = value
            (work / '1' / 'resume.json').write_text(json.dumps(saved))
            control(1, 'reconnect')
            wait(lambda: 'expired' in status(1)['status'].lower(), 'Invalid/expired resume refusal')
            assert not status(1)['connected']
            control(1, 'leave')
            wait(lambda: (current := status(1))['state'] is None and current['status'] == 'Main menu', 'Rejected session cleanup')
        (work / '1' / 'resume.json').write_text(original)
        control(1, 'reconnect')
        wait(lambda: status(1)['connected'], 'Valid resume after failed attempts')
        # Fresh players cannot join a running match.
        fresh = launch(3)
        wait(lambda: (current := status(3)) and 'locked' in current['status'].lower(), 'Mid-match admission refusal')
        assert len(status(0)['state']['Players']) == 2
        control(3, 'quit')
        fresh.wait(timeout=8)
        control(0, 'quit')
        host.wait(timeout=8)
        wait(lambda: not status(1)['connected'], 'Host shutdown')
        # A new authority on the same endpoint is a new match, with a fresh six-city lobby.
        control(1, 'leave')
        wait(lambda: status(1)['state'] is None, 'Old match cleanup')
        (work / '1' / 'resume.json').unlink()
        host = launch(0)
        wait(lambda: status(0) and status(0)['connected'], 'Replacement authority')
        control(1, 'reconnect')
        wait(lambda: status(1)['connected'], 'Fresh authority admission')
        third = launch(2)
        wait(lambda: (current := status(2)) and current['connected'], 'Third lobby player')
        fourth = launch(3)
        wait(lambda: (current := status(3)) and current['connected'], 'Fourth lobby player')
        fifth = launch(4)
        wait(lambda: (current := status(4)) and current['connected'], 'Fifth lobby player')
        sixth = launch(5)
        wait(lambda: (current := status(5)) and current['connected'], 'Sixth lobby player')
        seventh = launch(6)
        wait(lambda: (current := status(6)) and 'full' in current['status'].lower(), 'Seventh player refused')
        assert [p['Id'] for p in status(0)['state']['Players']] == [1, 2, 3, 4, 5, 6]
        control(3, 'quit'); fourth.wait(timeout=8)
        wait(lambda: not status(0)['state']['Players'][3]['Connected'], 'Lobby leave retained city')
        control(0, 'quit'); host.wait(timeout=8)
        wait(lambda: not status(2)['connected'], 'Host leaving lobby')
        absent = launch(7)
        wait(lambda: (current := status(7)) and ('timed out' in current['status'].lower() or 'unavailable' in current['status'].lower()), 'Missing authority fails connection', seconds=22)
        refused = status(7)
        assert not refused['connected'] and refused['state'] is None
        print('godot', 'PASS: ownership, duplicates, protocol refusal, restart, preparation/combat/death/draft resume, animation reset, sequence continuity, private files, invalid/expired credentials, locked roster and host shutdown', flush=True)
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
            log.flush(); log.seek(0)
            text = log.read()
            (destination / f'godot-reconnect-{index}.log').write_text(text)
            print(text[-1200:])
            if 'ERROR:' in text or 'Unhandled exception' in text:
                diagnostic_errors.append(text)
            log.close()
        if diagnostic_errors:
            raise RuntimeError('Engine diagnostics during process verification:\n' + '\n'.join(diagnostic_errors))
