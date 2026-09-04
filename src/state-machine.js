'use strict';

(function exposeBunnyStateMachine(root, factory) {
  const exported = factory();
  if (typeof module === 'object' && module.exports) module.exports = exported;
  else root.BunnyStateMachine = exported;
}(typeof globalThis !== 'undefined' ? globalThis : this, () => {
  const VALID_STATES = new Set(['idle', 'walk', 'stand', 'sleep', 'happy', 'drag']);

  class BunnyStateMachine {
    constructor(options = {}) {
      this.state = 'idle';
      this.direction = options.direction === 1 ? 1 : -1;
      this.paused = false;
      this.lastInteraction = options.now ?? Date.now();
    }

    setState(next) {
      if (!VALID_STATES.has(next)) throw new Error(`Unknown bunny state: ${next}`);
      this.state = next;
      return this.state;
    }

    setPaused(paused) {
      this.paused = Boolean(paused);
      if (this.paused) this.setState('sleep');
      return this.paused;
    }

    touch(now = Date.now()) {
      this.lastInteraction = now;
      if (this.state === 'sleep' && !this.paused) this.setState('idle');
    }

    turnAround() {
      this.direction *= -1;
      return this.direction;
    }

    chooseNext(random = Math.random, now = Date.now()) {
      if (this.paused || now - this.lastInteraction > 120000) return 'sleep';
      const roll = random();
      if (roll < 0.42) return 'idle';
      if (roll < 0.72) return 'walk';
      if (roll < 0.9) return 'stand';
      return 'sleep';
    }
  }

  return { BunnyStateMachine, VALID_STATES };
}));
