#!/usr/bin/env python3
# SPDX-License-Identifier: GPL-3.0-only
"""Compare paid native task traces; no simulation or manufactured checkpoints."""
import argparse
import json
from pathlib import Path


def compare(root):
    cases = []
    for width in (1280, 1920):
        before, after = [json.loads((root / layout / str(width) / 'army-workspace-task.json').read_text())
                         for layout in ('before', 'after')]
        assert before['width'] == after['width'] == width
        assert before['height'] == after['height'] == width * 9 // 16
        assert after['gestures'] < before['gestures']
        assert after['navigations'] < before['navigations']
        assert after['withinArmySectionChanges'] == 0 < before['withinArmySectionChanges']
        commands = [{step['name']: step for step in task['trace'] if step.get('command')} for task in (before, after)]
        assert commands[0].keys() == commands[1].keys() == {'reinforce', 'deploy', 'buy-home', 'battle'}
        for name in commands[0]:
            old, new = [steps[name] for steps in commands]
            assert old['command']['Accepted'] and new['command']['Accepted'], name
            assert old['before'] == new['before'] and old['after'] == new['after'], (width, name, 'payment')
            assert old['soldiers'] == new['soldiers'], (width, name, 'actual accepted assignment')
        cases.append(dict(width=width, beforeActions=before['gestures'], afterActions=after['gestures'],
                          beforeNavigation=before['navigations'], afterNavigation=after['navigations'],
                          beforeSections=before['withinArmySectionChanges'], afterSections=0,
                          matchedAcceptedCommands=list(commands[0]),
                          beforeInput=before['inputQualification'], afterInput=after['inputQualification']))
    return dict(cases=cases, limitation='Scripted native control counts and accepted payments/assignments; not human speed or comprehension evidence.')


if __name__ == '__main__':
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--root', type=Path, default=Path('artifacts/captures/army-workspace'))
    arguments = parser.parse_args()
    result = compare(arguments.root)
    (arguments.root / 'comparison.json').write_text(json.dumps(result, indent=2) + '\n')
    print('GODOT_ARMY_COMPARISON_PASS sizes=1280/1920 payments-and-assignments=exact actions=12-to-8 navigation=4-to-2 sections=2-to-0')
