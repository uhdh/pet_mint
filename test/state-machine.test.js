'use strict';

const test = require('node:test');
const assert = require('node:assert/strict');
const { BunnyStateMachine, VALID_STATES } = require('../src/state-machine.js');

test('starts idle and facing left by default', () => {
  const machine = new BunnyStateMachine();
  assert.equal(machine.state, 'idle');
  assert.equal(machine.direction, -1);
});

test('turnAround flips direction every time', () => {
  const machine = new BunnyStateMachine({ direction: 1 });
  assert.equal(machine.turnAround(), -1);
  assert.equal(machine.turnAround(), 1);
});

test('pause puts the bunny to sleep', () => {
  const machine = new BunnyStateMachine();
  machine.setPaused(true);
  assert.equal(machine.paused, true);
  assert.equal(machine.state, 'sleep');
});

test('stopped mode stays idle by default', () => {
  const now = 1_000_000;
  const machine = new BunnyStateMachine({ now });
  assert.equal(machine.playMode, false);
  assert.equal(machine.chooseNext(() => 0.5, now), 'idle');
});

test('play mode maps random ranges to expected actions', () => {
  const now = 1_000_000;
  const machine = new BunnyStateMachine({ now });
  machine.setPlayMode(true);
  assert.equal(machine.playMode, true);
  assert.equal(machine.chooseNext(() => 0.1, now), 'idle');
  assert.equal(machine.chooseNext(() => 0.5, now), 'walk');
  assert.equal(machine.chooseNext(() => 0.8, now), 'stand');
  assert.equal(machine.chooseNext(() => 0.95, now), 'sleep');
});

test('long inactivity chooses sleep and touch wakes it', () => {
  const machine = new BunnyStateMachine({ now: 0 });
  assert.equal(machine.chooseNext(() => 0, 120001), 'sleep');
  machine.setState('sleep');
  machine.touch(120002);
  assert.equal(machine.state, 'idle');
});

test('invalid states are rejected', () => {
  const machine = new BunnyStateMachine();
  assert.throws(() => machine.setState('fly'), /Unknown bunny state/);
  assert.deepEqual([...VALID_STATES].sort(), ['drag', 'happy', 'idle', 'sleep', 'stand', 'walk']);
});
