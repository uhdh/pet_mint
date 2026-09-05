'use strict';

const { BunnyStateMachine } = window.BunnyStateMachine;
const machine = new BunnyStateMachine({ direction: -1 });

const pet = document.querySelector('#pet');
const bunny = document.querySelector('#bunny');
const directionLayer = document.querySelector('#direction');
const message = document.querySelector('#message');
const hearts = document.querySelector('#hearts');

const ASSETS = {
  idle: '../assets/bunny-idle.png',
  walk: '../assets/bunny-walk.png',
  stand: '../assets/bunny-stand.png',
  sleep: '../assets/bunny-sleep.png',
  happy: '../assets/bunny-happy.png',
  drag: '../assets/bunny-stand.png'
};

const PHRASES = [
  '💕', '🥰', '💖', '💗', '💓', '💞', '😻', '🌸', '✨', '❤️', '🧡', '💛', '🤍', '😍', '😚', '💘'
];

const CURSOR_PHRASES = [
  '👀', '❓', '👋', '🐰', '😳', '✨', '⭐', '💡', '🔍', '🧐', '🤍', '🐾', '😮', '💫'
];

const ITEM_PHRASES = {
  doll: [
    '💢', '😡', '😤', '😒', '🥺', '😱', '🙄', '💔', '⚡', '👿', '😣', '😾', '😠', '💥'
  ],
  hay: [
    '🌾', '😋', '🥕', '🤤', '🥣', '🌿', '🍀', '🍽️', '🥗', '👅', '🍴', '🌱'
  ],
  bag: [
    '🎒', '🎈', '🎉', '🗺️', '🧭', '🥪', '👟', '🏕️', '🏃', '✨', '🎊', '🏖️'
  ],
  house: [
    '🏠', '💤', '🛋️', '🌙', '⭐', '🕯️', '🏡', '🛌', '😴', '☁️', '🛏️', '🪵'
  ],
  chair: [
    '🪑', '😌', '🛋️', '☕', '🍃', '🌸', '💆', '✨', '🧋', '🍵', '🌼', '🫖'
  ]
};

const RESTING_PHRASES = [
  '🍞', '🍞', '💤', '💤', '😴', '☁️', '🥐', '🥱', '🌾', '🌙', '✨'
];

const PURR_PHRASES = [
  '🥰', '💖', '💕', '🌸', '😻', '💓', '💗'
];

const PLAY_PHRASES = [
  '🎉', '🏃', '✨', '🐾', '🎈', '👀', '🥳'
];

const SCOLD_PHRASES = [
  '🍞', '🥺', '😳', '💤', '🤐', '🤫'
];

const itemHouse = document.querySelector('#item-house');
const itemChair = document.querySelector('#item-chair');
const itemDoll = document.querySelector('#item-doll');
const itemHay = document.querySelector('#item-hay');
const itemBag = document.querySelector('#item-bag');
let currentItem = 'none';

let behaviorTimer = null;
let walkTimer = null;
let messageTimer = null;
let dragging = false;
let dragMoved = false;
let lastPointer = null;
let movementQueue = Promise.resolve();

function randomBetween(min, max) {
  return Math.round(min + Math.random() * (max - min));
}

function setDirection(value) {
  machine.direction = value < 0 ? -1 : 1;
  directionLayer.classList.toggle('facing-left', machine.direction < 0);
}

function setVisualState(next) {
  machine.setState(next);
  pet.className = `state-${next}`;
  const assetKey = (!machine.playMode && (next === 'idle' || next === 'sleep')) ? 'sleep' : next;
  bunny.src = ASSETS[assetKey];
  bunny.alt = (next === 'sleep' || !machine.playMode)
    ? '식빵을 굽고 있는 귀여운 흰색 롭이어 토끼'
    : '회색 귀를 가진 하얀 롭이어 토끼';
}

function stopWalking() {
  if (walkTimer) clearInterval(walkTimer);
  walkTimer = null;
}

function queueMove(dx, dy) {
  movementQueue = movementQueue
    .then(() => window.bunnyDesktop.moveBy(dx, dy))
    .then((result) => {
      if (result?.hitX && machine.state === 'walk') setDirection(machine.turnAround());
      return result;
    })
    .catch(() => null);
  return movementQueue;
}

function startWalking() {
  stopWalking();
  if (Math.random() < 0.24) setDirection(machine.turnAround());
  walkTimer = setInterval(() => {
    if (machine.state !== 'walk' || machine.paused || dragging) return;
    queueMove(machine.direction * 2, 0);
  }, 80);
}

function showMessage(text, duration = 2100) {
  if (messageTimer) clearTimeout(messageTimer);
  message.textContent = text;
  message.classList.add('visible');
  messageTimer = setTimeout(() => message.classList.remove('visible'), duration);
}

function floatHeart() {
  const heart = document.createElement('span');
  heart.className = 'heart';
  heart.textContent = '♥';
  heart.style.setProperty('--drift', `${randomBetween(-21, 21)}px`);
  heart.style.setProperty('--turn', `${randomBetween(-22, 22)}deg`);
  hearts.appendChild(heart);
  setTimeout(() => heart.remove(), 1450);
}

function triggerPurring() {
  if (dragging) return;
  floatHeart();
  showMessage(PURR_PHRASES[randomBetween(0, PURR_PHRASES.length - 1)], 2200);
}

function showRestingEmoji() {
  if (dragging) return;
  const emoji = RESTING_PHRASES[randomBetween(0, RESTING_PHRASES.length - 1)];
  showMessage(emoji, 2500);
}

function setPlayMode(play) {
  machine.setPlayMode(play);
  stopWalking();
  if (play) {
    setDirection(machine.direction);
    setVisualState('happy');
    floatHeart();
    showMessage(PLAY_PHRASES[randomBetween(0, PLAY_PHRASES.length - 1)], 2200);
    scheduleBehavior(1500);
  } else {
    setVisualState('idle');
    showMessage(SCOLD_PHRASES[randomBetween(0, SCOLD_PHRASES.length - 1)], 2400);
    scheduleBehavior(randomBetween(4000, 7500));
  }
}

function scheduleBehavior(delay = randomBetween(3500, 7600)) {
  if (behaviorTimer) clearTimeout(behaviorTimer);
  behaviorTimer = setTimeout(() => {
    if (dragging) return scheduleBehavior(900);
    stopWalking();

    if (!machine.playMode) {
      setVisualState('idle');
      const roll = Math.random();
      if (roll < 0.35) {
        triggerPurring();
      } else if (roll < 0.75) {
        showRestingEmoji();
      }
      scheduleBehavior(randomBetween(4500, 8500));
      return;
    }

    const next = machine.chooseNext();
    setVisualState(next);
    if (next === 'walk') startWalking();
    const duration = next === 'sleep'
      ? randomBetween(9500, 18000)
      : randomBetween(3600, 7800);
    scheduleBehavior(duration);
  }, delay);
}

function setItem(item) {
  currentItem = item || 'none';
  if (itemHouse) itemHouse.classList.toggle('hidden', item !== 'house');
  if (itemChair) itemChair.classList.toggle('hidden', item !== 'chair');
  if (itemDoll) itemDoll.classList.toggle('hidden', item !== 'doll');
  if (itemHay) itemHay.classList.toggle('hidden', item !== 'hay');
  if (itemBag) itemBag.classList.toggle('hidden', item !== 'bag');

  machine.touch();
  stopWalking();

  if (item === 'doll') {
    setDirection(-1);
    setVisualState('stand');
    showMessage(ITEM_PHRASES.doll[randomBetween(0, ITEM_PHRASES.doll.length - 1)], 2600);
    scheduleBehavior(3000);
  } else if (item === 'hay') {
    setDirection(1);
    setVisualState('happy');
    floatHeart();
    showMessage(ITEM_PHRASES.hay[randomBetween(0, ITEM_PHRASES.hay.length - 1)], 2500);
    scheduleBehavior(2800);
  } else if (item === 'chair') {
    setVisualState('idle');
    floatHeart();
    showMessage(ITEM_PHRASES.chair[randomBetween(0, ITEM_PHRASES.chair.length - 1)], 2500);
    scheduleBehavior(2800);
  } else if (item === 'bag') {
    setVisualState('happy');
    showMessage(ITEM_PHRASES.bag[randomBetween(0, ITEM_PHRASES.bag.length - 1)], 2500);
    scheduleBehavior(2800);
  } else if (item === 'house') {
    setVisualState('idle');
    showMessage(ITEM_PHRASES.house[randomBetween(0, ITEM_PHRASES.house.length - 1)], 2500);
    scheduleBehavior(2800);
  } else {
    showMessage('✨', 1800);
    setVisualState('idle');
    scheduleBehavior(2000);
  }
}

function reactToPetting() {
  machine.touch();
  stopWalking();

  if (currentItem === 'doll') {
    setDirection(-1);
    setVisualState('stand');
    const phrase = ITEM_PHRASES.doll[randomBetween(0, ITEM_PHRASES.doll.length - 1)];
    showMessage(phrase, 2800);
    scheduleBehavior(3000);
    return;
  }

  if (!machine.playMode) {
    triggerPurring();
    scheduleBehavior(randomBetween(4500, 8000));
    return;
  }

  setVisualState('happy');
  floatHeart();
  setTimeout(floatHeart, 190);

  let phrase;
  if (currentItem === 'hay') {
    phrase = ITEM_PHRASES.hay[randomBetween(0, ITEM_PHRASES.hay.length - 1)];
  } else if (currentItem === 'chair') {
    phrase = ITEM_PHRASES.chair[randomBetween(0, ITEM_PHRASES.chair.length - 1)];
  } else if (currentItem === 'bag') {
    phrase = ITEM_PHRASES.bag[randomBetween(0, ITEM_PHRASES.bag.length - 1)];
  } else if (currentItem === 'house') {
    phrase = ITEM_PHRASES.house[randomBetween(0, ITEM_PHRASES.house.length - 1)];
  } else {
    phrase = PHRASES[randomBetween(0, PHRASES.length - 1)];
  }

  showMessage(phrase, 2200);
  scheduleBehavior(2400);
}

let lastHoverReactionTime = 0;

function reactToHover(event) {
  if (dragging || machine.paused || machine.state === 'happy' || machine.state === 'drag') return;
  const now = Date.now();
  if (now - lastHoverReactionTime < 2500) return;
  lastHoverReactionTime = now;

  machine.touch();
  stopWalking();

  if (event) {
    const rect = pet.getBoundingClientRect();
    const centerX = rect.left + rect.width / 2;
    if (event.clientX < centerX) {
      setDirection(-1);
    } else {
      setDirection(1);
    }
  }

  if (!machine.playMode) {
    setVisualState('idle');
    const phrase = CURSOR_PHRASES[randomBetween(0, CURSOR_PHRASES.length - 1)];
    showMessage(phrase, 1800);
    scheduleBehavior(3600);
    return;
  }

  setVisualState('stand');
  const phrase = CURSOR_PHRASES[randomBetween(0, CURSOR_PHRASES.length - 1)];
  showMessage(phrase, 2000);
  scheduleBehavior(2300);
}

let strokeDistance = 0;
let lastStrokeTime = 0;
let lastPetReactionTime = 0;

pet.addEventListener('pointerenter', reactToHover);

pet.addEventListener('pointerdown', (event) => {
  if (event.button !== 0) return;
  machine.touch();
  dragging = true;
  dragMoved = false;
  strokeDistance = 0;
  lastPointer = { x: event.screenX, y: event.screenY };
  pet.setPointerCapture(event.pointerId);
});

pet.addEventListener('pointermove', (event) => {
  if (!dragging) {
    const now = Date.now();
    if (now - lastStrokeTime > 700) {
      strokeDistance = 0;
    }
    lastStrokeTime = now;
    if (lastPointer) {
      strokeDistance += Math.abs(event.screenX - lastPointer.x) + Math.abs(event.screenY - lastPointer.y);
    }
    lastPointer = { x: event.screenX, y: event.screenY };
    if (strokeDistance > 70 && now - lastPetReactionTime > 2200) {
      lastPetReactionTime = now;
      strokeDistance = 0;
      reactToPetting();
    }
    return;
  }
  if (!lastPointer) return;
  const dx = event.screenX - lastPointer.x;
  const dy = event.screenY - lastPointer.y;
  if (Math.abs(dx) + Math.abs(dy) > 7) {
    if (!dragMoved) {
      dragMoved = true;
      stopWalking();
      setVisualState('drag');
    }
    queueMove(dx, dy);
    lastPointer = { x: event.screenX, y: event.screenY };
  }
});

function finishDrag(event) {
  if (!dragging) return;
  if (pet.hasPointerCapture(event.pointerId)) pet.releasePointerCapture(event.pointerId);
  dragging = false;
  lastPointer = null;
  if (dragMoved) {
    setVisualState('idle');
    scheduleBehavior(1700);
  } else {
    reactToPetting();
  }
}

pet.addEventListener('pointerup', finishDrag);
pet.addEventListener('pointercancel', finishDrag);
pet.addEventListener('dblclick', () => {
  machine.touch();
  setPlayMode(!machine.playMode);
});

pet.addEventListener('contextmenu', (event) => {
  event.preventDefault();
  window.bunnyDesktop.showContextMenu();
});

document.addEventListener('keydown', (event) => {
  if (event.key === 'Escape') {
    setVisualState('idle');
    window.bunnyDesktop.resetPosition();
    scheduleBehavior(1600);
  }
});

window.bunnyDesktop.onPauseChanged((paused) => {
  machine.setPaused(paused);
  stopWalking();
  setVisualState(paused ? 'sleep' : 'idle');
  showMessage(paused ? '💤' : '✨');
  scheduleBehavior(paused ? 30000 : 1400);
});

if (window.bunnyDesktop.onItemChanged) {
  window.bunnyDesktop.onItemChanged((item) => {
    setItem(item);
  });
}

if (window.bunnyDesktop.onPetRequested) {
  window.bunnyDesktop.onPetRequested(() => {
    reactToPetting();
  });
}

window.bunnyDesktop.getState().then((state) => {
  machine.setPaused(Boolean(state?.paused));
  machine.setPlayMode(false);
  setVisualState(machine.paused ? 'sleep' : 'idle');
  showMessage('🍞', 2500);
  scheduleBehavior(2200);
});
