# SPDX-License-Identifier: GPL-3.0-only
"""Bounded inter-observation accounting for scripted native clients."""
from collections import defaultdict
from contextlib import contextmanager
import time

class PreparationTiming:
    def __init__(self, clock=time.monotonic):
        self.clock = clock
        self.mode = None
        self.last = {}
        self.seconds = defaultdict(float)

    @staticmethod
    def classify(status):
        state = status.get('state')
        if not state:
            return ('setup', 0, 0), 'disconnected' if not status.get('connected') else 'setup'
        key = state['Phase'], state['Wave'], state['Turn']
        city = next((p for p in state['Players'] if p['Id'] == status['player']), None)
        if not status.get('connected') or city is None or not city['Connected']:
            cause = 'disconnected'
        elif city.get('Eliminated'):
            cause = 'observer'
        elif state['Paused']:
            cause = 'paused'
        elif status.get('nativeBusy'):
            cause = 'native-action'
        elif status.get('pending'):
            cause = 'awaiting-authority'
        elif status.get('hudMoment') == 'Combat' and state['Phase'] != 'Combat':
            cause = 'finishing-presentation'
        elif state['Phase'] in ('Building', 'Preparation'):
            cause = 'already-ready' if city['Ready'] else 'available-decision-window'
        elif state['Phase'] == 'RewardDraft':
            cause = 'selected-reward-wait' if city['Run'].get('Choice') else 'reward-choice-window'
        elif state['Phase'] == 'Combat':
            cause = 'active-combat'
        else:
            cause = 'terminal' if state['Phase'] in ('Victory', 'Defeat') else 'setup'
        return key, cause

    def observe(self, client, status):
        now = self.clock()
        self._flush(client, now)
        key, cause = self.classify(status)
        self.last[client] = now, key, cause

    def _flush(self, client, now):
        if client in self.last:
            start, key, cause = self.last[client]
            self.seconds[(client, *key, self.mode or cause)] += max(0, now - start)
            self.last[client] = now, key, cause

    def set_mode(self, mode):
        now = self.clock()
        for client in list(self.last):
            self._flush(client, now)
        self.mode = mode

    @contextmanager
    def measure(self, mode):
        previous = self.mode
        self.set_mode(mode)
        try:
            yield
        finally:
            self.set_mode(previous)

    def report(self):
        now = self.clock()
        for client in list(self.last):
            self._flush(client, now)
        return dict(intervals=[dict(client=k[0], phase=k[1], wave=k[2], turn=k[3], cause=k[4], seconds=v)
                               for k, v in sorted(self.seconds.items())],
                    limitation='Intervals are assigned to the previous observed client state, with explicit capture/test-delay overrides. Script/poll/PNG/software-rendering time is included. This is not human decision time or proof of why a person waited.')
