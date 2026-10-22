#!/usr/bin/env python3
"""Two real Godot processes: complete intent arguments, exact retry and allied native inspection."""
import json
import os
from pathlib import Path
import signal
import socket
import subprocess
import tempfile
import time

ROOT = Path(__file__).resolve().parent.parent
processes, logs = [], []
serial = 0
solo_observation = os.getenv('COMMON_WATCH_PREPARATION_SOLO') == '1'
signal.signal(signal.SIGTERM, lambda *_: (_ for _ in ()).throw(SystemExit(143)))
with tempfile.TemporaryDirectory(prefix='common-watch-gameplay-') as temporary:
    work = Path(temporary)
    with socket.socket(socket.AF_INET, socket.SOCK_DGRAM) as sock:
        sock.bind(('127.0.0.1', 0))
        port = sock.getsockname()[1]
    def launch(index):
        directory = work / str(index)
        directory.mkdir()
        (directory / 'control').mkdir()
        log = open(work / f'{index}.log', 'w+')
        logs.append(log)
        command = ['bash', 'tools/client.sh', '--settings-path', str(directory / 'settings.json'),
                   '--data-path', str(directory), '--control-dir', str(directory / 'control'),
                   '--test-fast-combat', 'yes', '--gameplay-stop-wave', '4' if solo_observation else '8',
                   '--gameplay-family', 'research' if index == 0 else 'mixed']
        art = 'forest' if os.getenv('COMMON_WATCH_GAMEPLAY_ART') == 'illustrated' else 'plain'
        observation = ROOT / f'artifacts/observations/preparation/{"solo" if solo_observation else art}-player-{index + 1}.json'
        observation.unlink(missing_ok=True)
        command += ['--preparation-observations', str(observation)]
        if os.getenv('COMMON_WATCH_GAMEPLAY_ART') == 'illustrated':
            command += ['--world-art', 'sprites', '--hud-art', 'illustrated', '--view', 'angled']
        else:
            command += ['--world-art', 'debug', '--hud-art', 'plain', '--view', 'topdown', '--debug-overlay', 'off']
        if not solo_observation:
            command += ['--host', str(port)] if index == 0 else ['--join', f'127.0.0.1:{port}']
        processes.append(subprocess.Popen(command, cwd=ROOT, stdout=log, stderr=subprocess.STDOUT, start_new_session=True))
    def status(index):
        path = work / str(index) / 'control/status.json'
        return json.loads(path.read_text()) if path.exists() else None
    def wait(predicate, message, seconds=45):
        deadline = time.monotonic() + seconds
        while time.monotonic() < deadline:
            result = predicate()
            if result:
                return result
            if any(p.poll() not in (None, 0) for p in processes):
                raise RuntimeError('Godot process failed: ' + message)
            time.sleep(.06)
        raise TimeoutError(message + ': ' + repr([status(i) and (status(i)['status'], status(i)['state'] and status(i)['state']['Wave']) for i in range(len(processes))]))
    def control(client_index, kind, **values):
        global serial
        serial += 1
        path = work / str(client_index) / 'control' / f'request-{serial:06}.json'
        path.with_suffix('.tmp').write_text(json.dumps(dict(kind=kind, **values)))
        path.with_suffix('.tmp').replace(path)
    def send(index, action, accepted=True, **values):
        old = status(index)['result']
        sequence = old['Sequence'] if old else 0
        control(index, 'send', action=action, **values)
        def acknowledged():
            current = status(index)
            result = current['result']
            return result if result and result['Sequence'] > sequence and not current['pending'] else None
        result = wait(acknowledged, action)
        assert result['Accepted'] == accepted, (action, result)
        return result
    def capture_feedback(name, expected):
        control(1, 'close-details')
        wait(lambda: expected in status(1)['hudNotice'], name + ' native feedback')
        wait(lambda: status(1)['hudNoticeVisible'] and expected in status(1)['hudNoticeText'], name + ' visible feedback control')
        assert status(1)['readyDisabled'] == (name != 'rejected')
        for width, height in ((1280, 720), (1920, 1080)):
            control(1, 'resize', width=width, height=height)
            art = 'forest' if os.getenv('COMMON_WATCH_GAMEPLAY_ART') == 'illustrated' else 'plain'
            path = ROOT / f'artifacts/captures/gameplay/network-{art}/{name}-{width}.png'
            path.unlink(missing_ok=True)
            control(1, 'capture', path=str(path))
            wait(lambda: path.exists(), name + ' capture')
        control(1, 'resize', width=1280, height=720)

    def city(index):
        current = status(index)
        return next(p for p in current['state']['Players'] if p['Id'] == current['player'])
    def capture_preparation(name):
        control(1, 'close-details')
        wait(lambda: status(1)['preparationVisible'], name + ' visible preparation summary')
        for width, height in ((1280, 720), (1920, 1080)):
            control(1, 'resize', width=width, height=height)
            wait(lambda: status(1)['viewportWidth'] == width, name + ' resize')
            art = 'forest' if os.getenv('COMMON_WATCH_GAMEPLAY_ART') == 'illustrated' else 'plain'
            path = ROOT / f'artifacts/captures/gameplay/network-{art}/{name}-{width}.png'
            path.unlink(missing_ok=True)
            control(1, 'capture', path=str(path))
            wait(lambda: path.exists(), name + ' capture')
            wait(lambda: status(1)['readyFits'], name + ' complete Ready frame: ' + repr(status(1)['readyLayout']), seconds=5)
        control(1, 'resize', width=1280, height=720)
    try:
        if solo_observation:
            launch(0)
            wait(lambda: status(0), 'ordinary single-player menu')
            control(0, 'single-player')
            wait(lambda: status(0)['state'] and status(0)['state']['Phase'] == 'Lobby', 'single-player doctrine screen')
            control(0, 'choose-doctrine', index=0)
            wait(lambda: not status(0)['doctrineStartDisabled'], 'explicit doctrine selection')
            send(0, 'doctrine:warden')  # Same paid Warden policy as the cooperative comparison.
            send(0, 'start')
            control(0, 'policy', enabled='yes')
            wait(lambda: status(0)['state']['Wave'] == 4 and status(0)['state']['Phase'] == 'Building', 'three paid solo waves', seconds=100)
            observation = ROOT / 'artifacts/observations/preparation/solo-player-1.json'
            wait(lambda: observation.exists() and len(json.loads(observation.read_text())['completed']) == 9, 'nine solo preparation observations')
            stages = json.loads(observation.read_text())['completed']
            assert all(s['Complete'] and not s['Partial'] and s['UnobservedCommands'] == 0 for s in stages), stages
            print('GODOT_PREPARATION_SOLO_PASS three-waves nine-stages recorded-actions waiting unchanged-rules', flush=True)
            raise SystemExit(0)
        launch(0)
        wait(lambda: status(0) and status(0)['connected'], 'host')
        launch(1)
        wait(lambda: status(1) and status(1)['connected'], 'guest')
        wait(lambda: all(len(status(i)['doctrineOffers']) == 3 for i in range(2)), 'personal doctrine offers')
        offers = [status(i)['doctrineOffers'] for i in range(2)]
        assert all(len(set(personal)) == 3 for personal in offers)
        assert status(0)['doctrineStartDisabled'] and not status(1)['doctrineStartVisible']
        control(0, 'choose-doctrine', index=0)
        wait(lambda: city(0)['Run']['Doctrine'] == offers[0][0], 'host explicit doctrine')
        assert status(0)['doctrineStartDisabled'], 'host waits for guest choice'
        control(1, 'resize', width=1920, height=1080)
        control(1, 'choose-doctrine', index=1)
        wait(lambda: not status(0)['doctrineStartDisabled'] and not status(1)['pending'] and city(1)['Run']['Doctrine'] == offers[1][1], 'all connected players explicitly selected')
        assert status(1)['doctrineOffers'] == offers[1]
        assert city(1)['Run']['Doctrine'] == offers[1][1]
        lobby_revision = status(1)['state']['Revision']
        control(1, 'reconnect')
        wait(lambda: status(1)['connected'] and status(1)['state']['Revision'] > lobby_revision, 'lobby reconnect')
        assert status(1)['doctrineOffers'] == offers[1]
        assert city(1)['Run']['Doctrine'] == offers[1][1]
        wait(lambda: not status(0)['doctrineStartDisabled'], 'explicit choices survive lobby reconnect')
        # The existing paid seven-wave fixture deliberately starts from Warden's known economy.
        send(0, 'doctrine:warden')
        send(1, 'doctrine:warden')
        send(0, 'start')
        wait(lambda: all(status(i)['state']['Phase'] == 'Building' for i in range(2)), 'start synchronized')
        send(0, 'ready')
        assert city(0)['Ready']
        wait(lambda: 'Ready: 1/2' in status(0)['transitionText'] and status(0)['readyText'] == 'Unready', 'visible all-city readiness and Unready')
        assert status(0)['state']['Turn'] == 1, 'one ready city cannot advance production'
        capture_preparation('waiting-for-ally')
        send(0, 'unready')
        assert not city(0)['Ready']
        send(1, 'speed-2x', accepted=False)
        send(0, 'speed-1.5x')
        send(1, 'pause')
        send(1, 'build', accepted=False, slot=0, building='Farm')
        send(0, 'resume')
        # Hold the actual authority process briefly to observe an unacknowledged command.
        os.killpg(processes[0].pid, signal.SIGSTOP)
        try:
            control(1, 'send', action='ready')
            wait(lambda: status(1)['pending'], 'real pending acknowledgment')
            capture_feedback('pending', 'Pending')
        finally:
            os.killpg(processes[0].pid, signal.SIGCONT)
        wait(lambda: not status(1)['pending'] and city(1)['Ready'], 'ready acknowledged after authority resumes')
        send(1, 'unready')
        send(1, 'speed-2x', accepted=False)
        capture_feedback('rejected', 'Rejected')
        control(0, 'hold-combat', wave=6)
        for index in range(2):
            control(index, 'policy', enabled='yes')
        wait(lambda: all(status(i)['state']['Wave'] == 6 and status(i)['state']['Phase'] == 'Combat' and status(i)['state']['Paused'] for i in range(2)), 'dense cooperative battle held by authoritative pause', seconds=100)
        actors = [u for p in status(1)['state']['Players'] for u in p['Soldiers'] if u['Health'] > 0] + status(1)['state']['Enemies']
        assert len(actors) >= 16, ('dense battle actor count', len(actors))
        unit = next(u for u in actors if u['Faction'] == 'Adventurers' and u['Owner'] == 2 and u['Hex']['Lifecycle'] == 'Alive')
        control(1, 'inspect-combat', unitId=unit['Id'])
        wait(lambda: status(1)['liveVisible'] and status(1)['liveUnit'] == unit['Id'] and status(1)['highlightedMembers'] > 0, 'read-only live inspection in shared arena')
        assert not status(1)['blocksWorld'] and 'Effects:' in status(1)['liveText']
        assert 'Committed' not in status(1)['liveText'] and status(1)['squadCues'] > 1
        for width, height in ((1280, 720), (1920, 1080)):
            control(1, 'resize', width=width, height=height)
            wait(lambda: status(1)['viewportWidth'] == width and status(1)['liveFits'], 'dense battle live inspector layout')
            art = 'forest' if os.getenv('COMMON_WATCH_GAMEPLAY_ART') == 'illustrated' else 'plain'
            path = ROOT / f'artifacts/captures/gameplay/network-{art}/dense-combat-{width}.png'
            path.unlink(missing_ok=True); control(1, 'capture', path=str(path))
            wait(lambda: path.exists(), 'dense battle capture')
        print(f'GODOT_COMBAT_COOP_PASS actors={len(actors)} cues={status(1)["squadCues"]} sizes=1280/1920 read-only ownership', flush=True)
        control(1, 'close-combat'); control(1, 'resize', width=1280, height=720)
        send(0, 'resume')
        wait(lambda: all(status(i)['state']['Wave'] == 8 and status(i)['state']['Phase'] == 'Building' for i in range(2)), 'seven legal cooperative waves', seconds=100)
        for index in range(2):
            art = 'forest' if os.getenv('COMMON_WATCH_GAMEPLAY_ART') == 'illustrated' else 'plain'
            observation = ROOT / f'artifacts/observations/preparation/{art}-player-{index + 1}.json'
            wait(lambda: observation.exists() and len(json.loads(observation.read_text())['completed']) >= 21, 'three observed preparations per earned wave')
            stages = json.loads(observation.read_text())['completed'][:9]
            assert all(s['Complete'] and not s['Partial'] and s['UnobservedCommands'] == 0 for s in stages), stages
        for index in range(2):
            control(index, 'policy', enabled='no')
        wait(lambda: all(not status(i)['finishingCombat'] for i in range(2)), 'local combat presentation completes before preparation capture')
        send(1, 'ready')  # Hold the guest ready while inspecting the host; host stays unready.
        send(0, 'pause')
        # Native city selector and open dialogs must keep authenticated command identity distinct.
        for index in range(2):
            owner = 2 if index == 0 else 1
            control(index, 'inspect', owner=owner, page="Worldtree")
            wait(lambda: status(index)['viewed'] == owner and 'read-only' in status(index)['readOnly'], 'allied read-only Worldtree')
            assert status(index)['disabledResearch']
            wait(lambda: f'Wave 8 → P{owner}:' in status(index)['preparationText'], 'allied summary follows inspected city')
            assert 'prep 2/3' in status(index)['transitionText']
            assert f'P{status(index)["player"]}' in status(index)['readyText']
        capture = ROOT / 'artifacts/captures/gameplay/multiplayer-city-inspection.png'
        capture.unlink(missing_ok=True)
        control(1, 'capture', path=str(capture))
        wait(lambda: capture.exists(), 'actual allied-city capture')
        capture_preparation('allied-preparation')
        assert 'Unready' in status(1)['readyText'] and city(1)['Ready']
        send(1, 'pause')  # Inspecting an ally does not change local command identity.
        send(0, 'resume')
        send(1, 'unready')
        wait(lambda: not city(1)['Ready'], 'remote Unready snapshot while inspecting an ally')
        assert not city(1)['Ready'] and status(1)['state']['Turn'] == 1
        control(1, 'inspect', owner=2, page="Army")
        wait(lambda: status(1)['viewed'] == 2, 'return to own city')
        for index in range(2):
            send(index, 'ready')
        wait(lambda: all(status(i)['state']['Turn'] == 2 for i in range(2)), 'one earned production before trade')
        # Trade arguments and exact duplicate spending over the real remote path.
        market = next(i for i, s in enumerate(city(1)['Slots']) if s['Type'] == 'Market')
        before = city(1)['Metal']
        result = send(1, 'trade', slot=market, resource='Metal', bundles=2)
        wait(lambda: city(1)['Metal'] == before - 10, 'remote trade snapshot')
        control(1, 'retry')
        wait(lambda: status(1)['result'] and status(1)['result']['Sequence'] == result['Sequence'] and not status(1)['pending'], 'exact retry')
        assert city(1)['Metal'] == before - 10
        send(1, 'trade', accepted=False, slot=market, resource='Metal', bundles=0)
        # Explicit squad and deployment-cell arguments on the remote client.
        members = city(1)['Soldiers']
        squad = next(u for u in members if u['Type'] == 'Swordsman')
        home = squad['Assignment']['Tile']
        sector = next(s for s in status(1)['state']['Arena']['Sectors'] if s['Owner'] == 2)
        send(1, 'deploy-squad', squadId=squad['SquadId'], cell=sector['DeploymentCells'][home])
        send(1, 'regroup-member', accepted=False, unitId=squad['Id'], squadId=99999)
        send(1, 'research-tech', accepted=False, technology='FireMastery')
        send(1, 'research-tech', technology='RangedFoundation')
        trainer = next(i for i, s in enumerate(city(1)['Slots']) if s['Type'] == 'Barracks')
        send(1, 'retire', unitId=squad['Id'])
        wait(lambda: all(u['Id'] != squad['Id'] for u in city(1)['Soldiers']), 'remote retirement snapshot')
        send(1, 'recruit', slot=trainer, soldierType='Swordsman', squadId=squad['SquadId'])
        # A foreign ownership request remains rejected by authority, not just by the UI.
        old = status(1)['result']['Sequence']
        control(1, 'probe', action='ready', owner=1)
        wait(lambda: status(1)['result'] and status(1)['result']['Sequence'] > old, 'foreign ownership rejection')
        assert not status(1)['result']['Accepted']
        control(0, 'leave')
        wait(lambda: not status(1)['connected'], 'original host closed')
        capture_feedback('disconnected', 'Disconnected')
        print('GODOT_GAMEPLAY_NETWORK_PASS hosted remote all-arguments seven-waves drafts ready/unready pause speed retry allied-read-only', flush=True)
    finally:
        for process in reversed(processes):
            if process.poll() is None:
                os.killpg(process.pid, signal.SIGTERM)
                try:
                    process.wait(timeout=5)
                except subprocess.TimeoutExpired:
                    os.killpg(process.pid, signal.SIGKILL)
                    process.wait(timeout=5)
        target = ROOT / 'artifacts/test-logs/gameplay-network.log'
        target.parent.mkdir(parents=True, exist_ok=True)
        with target.open('w') as output:
            for log in logs:
                log.seek(0)
                text = log.read()
                output.write(text)
                if 'ERROR:' in text or 'Exception' in text:
                    print(text)
                log.close()
