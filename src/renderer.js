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
  '오늘도 같이 있어요',
  '코를 살짝 눌러 주세요',
  '간식 생각 중…',
  '옆에 있어도 될까요?',
  '쓰다듬어 줘서 고마워요'
];

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
  bunny.src = ASSETS[next];
  bunny.alt = next === 'sleep'
    ? '편안하게 잠든 회색 귀의 하얀 롭이어 토끼'
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

function scheduleBehavior(delay = randomBetween(3500, 7600)) {
  if (behaviorTimer) clearTimeout(behaviorTimer);
  behaviorTimer = setTimeout(() => {
    if (dragging) return scheduleBehavior(900);
    const next = machine.chooseNext();
    stopWalking();
    setVisualState(next);
    if (next === 'walk') startWalking();
    const duration = next === 'sleep'
      ? randomBetween(9500, 18000)
      : randomBetween(3600, 7800);
    scheduleBehavior(duration);
  }, delay);
}

function reactToPetting() {
  machine.touch();
  stopWalking();
  setVisualState('happy');
  floatHeart();
  setTimeout(floatHeart, 190);
  showMessage(PHRASES[randomBetween(0, PHRASES.length - 1)]);
  scheduleBehavior(2300);
}

pet.addEventListener('pointerdown', (event) => {
  if (event.button !== 0) return;
  machine.touch();
  dragging = true;
  dragMoved = false;
  lastPointer = { x: event.screenX, y: event.screenY };
  pet.setPointerCapture(event.pointerId);
});

pet.addEventListener('pointermove', (event) => {
  if (!dragging || !lastPointer) return;
  const dx = event.screenX - lastPointer.x;
  const dy = event.screenY - lastPointer.y;
  if (Math.abs(dx) + Math.abs(dy) > 2) {
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
  stopWalking();
  setVisualState('stand');
  showMessage('무슨 소리였지?');
  scheduleBehavior(3000);
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
  showMessage(paused ? '여기서 잠깐 잘게요' : '다시 놀아 볼까요?');
  scheduleBehavior(paused ? 30000 : 1400);
});

window.bunnyDesktop.getState().then((state) => {
  machine.setPaused(Boolean(state?.paused));
  setVisualState(machine.paused ? 'sleep' : 'idle');
  showMessage('안녕하세요!');
  scheduleBehavior(2200);
});
