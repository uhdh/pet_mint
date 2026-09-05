/**
 * My Bunny Desktop Pet — Promotional & Download Site Script
 * Interactive Desktop Simulation & Playground
 */

(function () {
  'use strict';

  /* ==========================================================================
     1. 테마 토글 (Dark / Light Theme)
     ========================================================================== */
  const themeToggleBtn = document.querySelector('[data-theme-toggle]');
  const root = document.documentElement;

  function getPreferredTheme() {
    const saved = localStorage.getItem('bunny-theme');
    if (saved) return saved;
    return window.matchMedia('(prefers-color-scheme: dark)').matches ? 'dark' : 'light';
  }

  function applyTheme(theme) {
    if (theme === 'dark') {
      root.setAttribute('data-theme', 'dark');
    } else {
      root.removeAttribute('data-theme');
    }
    localStorage.setItem('bunny-theme', theme);
  }

  applyTheme(getPreferredTheme());

  if (themeToggleBtn) {
    themeToggleBtn.addEventListener('click', () => {
      const current = root.getAttribute('data-theme') === 'dark' ? 'dark' : 'light';
      applyTheme(current === 'dark' ? 'light' : 'dark');
    });
  }

  /* ==========================================================================
     2. 공통 에셋 및 대사 데이터
     ========================================================================== */
  const ASSETS = {
    idle: 'assets/bunny-idle.png',
    walk: 'assets/bunny-walk.png',
    stand: 'assets/bunny-stand.png',
    sleep: 'assets/bunny-sleep.png',
    happy: 'assets/bunny-happy.png',
    confused: 'assets/bunny-confused.gif'
  };

  const PET_PHRASES = [
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
    chair: [
      '🪑', '😌', '🛋️', '☕', '🍃', '🌸', '💆', '✨', '🧋', '🍵', '🌼', '🫖'
    ],
    bag: [
      '🎒', '🎈', '🎉', '🗺️', '🧭', '🥪', '👟', '🏕️', '🏃', '✨', '🎊', '🏖️'
    ],
    house: [
      '🏠', '💤', '🛋️', '🌙', '⭐', '🕯️', '🏡', '🛌', '😴', '☁️', '🛏️', '🪵'
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

  function randomItem(arr) {
    return arr[Math.floor(Math.random() * arr.length)];
  }

  function randomBetween(min, max) {
    return Math.floor(min + Math.random() * (max - min + 1));
  }

  /* 부드러운 웹 오디오 팝 효과음 (외부 파일 없이 순수 Web Audio API) */
  let audioCtx = null;
  function playPopSound() {
    try {
      if (!audioCtx) audioCtx = new (window.AudioContext || window.webkitAudioContext)();
      if (audioCtx.state === 'suspended') audioCtx.resume();
      
      const osc = audioCtx.createOscillator();
      const gain = audioCtx.createGain();
      
      osc.type = 'sine';
      const freq = randomBetween(520, 680);
      osc.frequency.setValueAtTime(freq, audioCtx.currentTime);
      osc.frequency.exponentialRampToValueAtTime(freq * 1.5, audioCtx.currentTime + 0.08);

      gain.gain.setValueAtTime(0.08, audioCtx.currentTime);
      gain.gain.exponentialRampToValueAtTime(0.001, audioCtx.currentTime + 0.12);

      osc.connect(gain);
      gain.connect(audioCtx.destination);

      osc.start();
      osc.stop(audioCtx.currentTime + 0.12);
    } catch (e) {
      // AudioContext 정책 무시
    }
  }

  /* ==========================================================================
     3. 미니 데스크톱 시뮬레이터 (#demo)
     ========================================================================== */
  const simScreen = document.getElementById('sim-screen');
  const simBunny = document.getElementById('sim-bunny');
  const simSprite = document.getElementById('sim-sprite');
  const simImg = document.getElementById('sim-img');
  const simSpeech = document.getElementById('sim-speech');
  const simSpeechText = document.getElementById('sim-speech-text');
  const simHearts = document.getElementById('sim-hearts');
  const simClock = document.getElementById('sim-clock');
  const simResetBtn = document.getElementById('sim-reset-btn');
  const simPetBtn = document.getElementById('sim-pet-btn');
  const simChairBtn = document.getElementById('sim-chair-btn');
  const simDollBtn = document.getElementById('sim-doll-btn');
  const simHayBtn = document.getElementById('sim-hay-btn');
  const simItemHouse = document.getElementById('sim-item-house');
  const simItemChair = document.getElementById('sim-item-chair');
  const simItemDoll = document.getElementById('sim-item-doll');
  const simItemHay = document.getElementById('sim-item-hay');
  const simItemBag = document.getElementById('sim-item-bag');

  // 시뮬레이터 시계
  function updateClock() {
    if (!simClock) return;
    const now = new Date();
    const h = String(now.getHours()).padStart(2, '0');
    const m = String(now.getMinutes()).padStart(2, '0');
    simClock.textContent = `${h}:${m}`;
  }
  updateClock();
  setInterval(updateClock, 30000);

  if (simScreen && simBunny) {
    let simX = 0;
    let simY = 42; // bottom px
    let simDirection = -1; // -1: left, 1: right
    let simState = 'idle'; // idle, walk, stand, happy, sleep
    let simWalkTimer = null;
    let simBehaviorTimer = null;
    let simSpeechTimer = null;
    let simDragging = false;
    let simDragMoved = false;
    let simLastPointer = null;
    let simLastReactionTime = 0;
    let simCurrentItem = 'none';

    function setSimItem(item) {
      simCurrentItem = item || 'none';
      if (simItemHouse) simItemHouse.classList.toggle('hidden', item !== 'house');
      if (simItemChair) simItemChair.classList.toggle('hidden', item !== 'chair');
      if (simItemDoll) simItemDoll.classList.toggle('hidden', item !== 'doll');
      if (simItemHay) simItemHay.classList.toggle('hidden', item !== 'hay');
      if (simItemBag) simItemBag.classList.toggle('hidden', item !== 'bag');

      if (simDollBtn) simDollBtn.classList.toggle('active', item === 'doll');
      if (simHayBtn) simHayBtn.classList.toggle('active', item === 'hay');
      if (simChairBtn) simChairBtn.classList.toggle('active', item === 'chair');

      stopSimWalking();

      if (item === 'doll') {
        simDirection = -1;
        updateSimTransform();
        setSimState('stand');
        playPopSound();
        showSimSpeech(randomItem(ITEM_PHRASES.doll), 2600);
        scheduleSimBehavior(3000);
      } else if (item === 'hay') {
        simDirection = 1;
        updateSimTransform();
        setSimState('happy');
        floatSimHeart();
        showSimSpeech(randomItem(ITEM_PHRASES.hay), 2500);
        scheduleSimBehavior(2800);
      } else if (item === 'chair') {
        setSimState('idle');
        floatSimHeart();
        showSimSpeech(randomItem(ITEM_PHRASES.chair), 2500);
        scheduleSimBehavior(2800);
      } else if (item === 'bag') {
        setSimState('happy');
        showSimSpeech(randomItem(ITEM_PHRASES.bag), 2500);
        scheduleSimBehavior(2800);
      } else if (item === 'house') {
        setSimState('idle');
        showSimSpeech(randomItem(ITEM_PHRASES.house), 2500);
        scheduleSimBehavior(2800);
      } else {
        setSimState('idle');
        showSimSpeech('✨', 1800);
        scheduleSimBehavior(2000);
      }
    }

    function getScreenBounds() {
      return simScreen.getBoundingClientRect();
    }

    function initSimPosition() {
      const bounds = getScreenBounds();
      simX = Math.max(20, bounds.width - 120);
      simY = 42;
      simDirection = -1;
      updateSimTransform();
      setSimState('idle');
      hideSimSpeech();
    }

    function updateSimTransform() {
      simBunny.style.left = `${simX}px`;
      simBunny.style.bottom = `${simY}px`;
      simSprite.classList.toggle('facing-left', simDirection < 0);
    }

    function setSimState(next) {
      simState = next;
      const assetKey = (!simPlayMode && (next === 'idle' || next === 'sleep')) ? 'sleep' : next;
      if (ASSETS[assetKey]) {
        simImg.src = ASSETS[assetKey];
      }
      simSprite.className = `sim-bunny-sprite ${simDirection < 0 ? 'facing-left' : ''} anim-${(!simPlayMode && next === 'idle') || next === 'sleep' ? 'sleep' : next === 'idle' ? 'breathe' : next === 'stand' ? 'curious' : next === 'happy' ? 'happy' : ''}`;
    }

    function hideSimSpeech() {
      if (simSpeechTimer) clearTimeout(simSpeechTimer);
      simSpeechTimer = null;
      simSpeech.classList.add('hidden');
    }

    function showSimSpeech(text, duration = 2000) {
      if (!simPlayMode) return;
      if (simSpeechTimer) clearTimeout(simSpeechTimer);
      simSpeechText.textContent = text;
      simSpeech.classList.remove('hidden');
      simSpeechTimer = setTimeout(() => {
        simSpeech.classList.add('hidden');
      }, duration);
    }

    function floatSimHeart() {
      playPopSound();
      const heart = document.createElement('span');
      heart.className = 'heart-float';
      heart.textContent = '♥';
      heart.style.left = '50%';
      heart.style.top = '30px';
      heart.style.setProperty('--dx', `${randomBetween(-24, 24)}px`);
      heart.style.setProperty('--rot', `${randomBetween(-25, 25)}deg`);
      simHearts.appendChild(heart);
      setTimeout(() => heart.remove(), 1300);
    }

    function stopSimWalking() {
      if (simWalkTimer) clearInterval(simWalkTimer);
      simWalkTimer = null;
    }

    function startSimWalking() {
      stopSimWalking();
      if (Math.random() < 0.3) {
        simDirection *= -1;
      }
      simWalkTimer = setInterval(() => {
        if (simDragging || simState !== 'walk') return;
        const bounds = getScreenBounds();
        const minX = 10;
        const maxX = bounds.width - 100;

        simX += simDirection * 2;
        if (simX <= minX) {
          simX = minX;
          simDirection = 1;
        } else if (simX >= maxX) {
          simX = maxX;
          simDirection = -1;
        }
        updateSimTransform();
      }, 70);
    }

    let simPlayMode = false;

    function setSimPlayMode(play) {
      simPlayMode = Boolean(play);
      stopSimWalking();
      const playBtns = document.querySelectorAll('#sim-play-toggle-btn, #sim-mock-play-btn');
      playBtns.forEach(playBtn => {
        playBtn.textContent = simPlayMode ? '🛑 나대지마 (멈추기)' : '🎉 놀자! (움직이기)';
        playBtn.classList.toggle('active', simPlayMode);
      });
      if (simPlayMode) {
        setSimState('happy');
        floatSimHeart();
        showSimSpeech(randomItem(PLAY_PHRASES), 2200);
        scheduleSimBehavior(1500);
      } else {
        hideSimSpeech();
        setSimState('idle');
        scheduleSimBehavior(randomBetween(4000, 7500));
      }
    }

    function scheduleSimBehavior(delay = randomBetween(3500, 7000)) {
      if (simBehaviorTimer) clearTimeout(simBehaviorTimer);
      simBehaviorTimer = setTimeout(() => {
        if (simDragging) return scheduleSimBehavior(1000);

        stopSimWalking();

        if (!simPlayMode) {
          setSimState('idle');
          const roll = Math.random();
          if (roll < 0.30) {
            // 멈추기 자세에서 가끔 갸르릉 하기 (하트만, 말풍선 X)
            floatSimHeart();
          }
          scheduleSimBehavior(randomBetween(4500, 8500));
          return;
        }

        const roll = Math.random();
        if (roll < 0.38) {
          setSimState('idle');
          scheduleSimBehavior(randomBetween(3500, 6000));
        } else if (roll < 0.68) {
          setSimState('walk');
          startSimWalking();
          scheduleSimBehavior(randomBetween(3500, 7000));
        } else if (roll < 0.82) {
          setSimState('stand');
          scheduleSimBehavior(randomBetween(2500, 4500));
        } else if (roll < 0.94) {
          // 실사 어리둥절 민트!
          setSimState('confused');
          showSimSpeech('❓', 2200);
          scheduleSimBehavior(3300);
        } else {
          setSimState('sleep');
          scheduleSimBehavior(randomBetween(7000, 12000));
        }
      }, delay);
    }

    function reactSimPetting() {
      stopSimWalking();

      if (simCurrentItem === 'doll') {
        simDirection = -1;
        updateSimTransform();
        setSimState('stand');
        playPopSound();
        showSimSpeech(randomItem(ITEM_PHRASES.doll), 2600);
        scheduleSimBehavior(3000);
        return;
      }

      if (!simPlayMode) {
        floatSimHeart();
        scheduleSimBehavior(randomBetween(4500, 8000));
        return;
      }

      setSimState('happy');
      floatSimHeart();
      setTimeout(floatSimHeart, 180);

      let phrase;
      if (simCurrentItem === 'hay') phrase = randomItem(ITEM_PHRASES.hay);
      else if (simCurrentItem === 'chair') phrase = randomItem(ITEM_PHRASES.chair);
      else if (simCurrentItem === 'bag') phrase = randomItem(ITEM_PHRASES.bag);
      else if (simCurrentItem === 'house') phrase = randomItem(ITEM_PHRASES.house);
      else phrase = randomItem(PET_PHRASES);

      showSimSpeech(phrase, 2200);
      scheduleSimBehavior(2400);
    }

    function reactSimCursor(relX) {
      const now = Date.now();
      if (now - simLastReactionTime < 2400) return;
      if (simDragging || simState === 'happy') return;

      simLastReactionTime = now;
      stopSimWalking();

      // 커서 위치 바라보기
      simDirection = relX < simX + 45 ? -1 : 1;
      updateSimTransform();

      if (!simPlayMode) {
        setSimState('idle');
        scheduleSimBehavior(3600);
        return;
      }

      setSimState('stand');
      showSimSpeech(randomItem(CURSOR_PHRASES), 1800);
      scheduleSimBehavior(2200);
    }

    // 마우스 호버 감지 (시뮬레이터 화면 내 커서 위치 체크)
    simScreen.addEventListener('mousemove', (e) => {
      if (simDragging) return;
      const bounds = getScreenBounds();
      const mouseX = e.clientX - bounds.left;
      const mouseY = bounds.bottom - e.clientY;

      const bunnyCenterX = simX + 45;
      const bunnyCenterY = simY + 40;
      const dist = Math.hypot(mouseX - bunnyCenterX, mouseY - bunnyCenterY);

      if (dist < 85) {
        reactSimCursor(mouseX);
      }
    });

    // 드래그 & 클릭 & 쓰다듬기(비비기) 상호작용
    let simStrokeDistance = 0;
    let simLastStrokeTime = 0;
    let simLastStrokePos = null;
    let simLastPetReactionTime = 0;

    simBunny.addEventListener('pointermove', (e) => {
      if (simDragging) return;
      const now = Date.now();
      if (now - simLastStrokeTime > 700) {
        simStrokeDistance = 0;
      }
      simLastStrokeTime = now;
      if (simLastStrokePos) {
        simStrokeDistance += Math.abs(e.clientX - simLastStrokePos.x) + Math.abs(e.clientY - simLastStrokePos.y);
      }
      simLastStrokePos = { x: e.clientX, y: e.clientY };
      if (simStrokeDistance > 65 && now - simLastPetReactionTime > 2200) {
        simLastPetReactionTime = now;
        simStrokeDistance = 0;
        reactSimPetting();
      }
    });

    simBunny.addEventListener('pointerdown', (e) => {
      if (e.button !== 0) return;
      simDragging = true;
      simDragMoved = false;
      simLastPointer = { x: e.clientX, y: e.clientY };
      simBunny.setPointerCapture(e.pointerId);
      stopSimWalking();
    });

    window.addEventListener('pointermove', (e) => {
      if (!simDragging || !simLastPointer) return;
      const dx = e.clientX - simLastPointer.x;
      const dy = e.clientY - simLastPointer.y;

      if (Math.abs(dx) + Math.abs(dy) > 8) {
        if (!simDragMoved) {
          simDragMoved = true;
          setSimState('stand');
        }
        const bounds = getScreenBounds();
        simX = Math.max(10, Math.min(bounds.width - 100, simX + dx));
        simY = Math.max(42, Math.min(bounds.height - 90, simY - dy));
        updateSimTransform();
        simLastPointer = { x: e.clientX, y: e.clientY };
      }
    });

    function finishSimDrag(e) {
      if (!simDragging) return;
      try { simBunny.releasePointerCapture(e.pointerId); } catch (_) {}
      simDragging = false;
      simLastPointer = null;

      if (simDragMoved) {
        // 드래그 후 내려놓기 (바닥으로 천천히 복귀)
        simY = 42;
        updateSimTransform();
        setSimState('idle');
        scheduleSimBehavior(1800);
      } else {
        // 단순 클릭 = 쓰다듬기
        reactSimPetting();
      }
    }

    simBunny.addEventListener('pointerup', finishSimDrag);
    simBunny.addEventListener('pointercancel', finishSimDrag);

    // 컨트롤 버튼 연동
    if (simResetBtn) {
      simResetBtn.addEventListener('click', initSimPosition);
    }
    if (simPetBtn) {
      simPetBtn.addEventListener('click', reactSimPetting);
    }
    if (simChairBtn) {
      simChairBtn.addEventListener('click', () => {
        setSimItem(simCurrentItem === 'chair' ? 'none' : 'chair');
      });
    }
    if (simDollBtn) {
      simDollBtn.addEventListener('click', () => {
        setSimItem(simCurrentItem === 'doll' ? 'none' : 'doll');
      });
    }
    if (simHayBtn) {
      simHayBtn.addEventListener('click', () => {
        setSimItem(simCurrentItem === 'hay' ? 'none' : 'hay');
      });
    }

    simBunny.addEventListener('dblclick', (e) => {
      e.stopPropagation();
      setSimPlayMode(!simPlayMode);
    });

    const playBtns = document.querySelectorAll('#sim-play-toggle-btn, #sim-mock-play-btn');
    playBtns.forEach(btn => {
      btn.addEventListener('click', () => {
        setSimPlayMode(!simPlayMode);
      });
    });

    const confusedBtns = document.querySelectorAll('#sim-confused-btn, #sim-mock-confused-btn');
    confusedBtns.forEach(btn => {
      btn.addEventListener('click', () => {
        stopSimWalking();
        setSimState('confused');
        showSimSpeech('❓', 2200);
        scheduleSimBehavior(3300);
      });
    });

    // 초기화
    setTimeout(initSimPosition, 300);
    scheduleSimBehavior(3500);
    window.addEventListener('resize', () => {
      const bounds = getScreenBounds();
      if (simX > bounds.width - 100) {
        simX = Math.max(20, bounds.width - 110);
        updateSimTransform();
      }
    });
  }

  /* ==========================================================================
     4. 인터랙티브 샌드박스 (#play)
     ========================================================================== */
  const sandboxStage = document.getElementById('sandbox-stage');
  const sandboxPet = document.getElementById('sandbox-pet');
  const sandboxImg = document.getElementById('sandbox-img');
  const sandboxSpeech = document.getElementById('sandbox-speech');
  const sandboxSpeechText = document.getElementById('sandbox-speech-text');
  const sandboxHearts = document.getElementById('sandbox-hearts');
  const sandboxHint = document.getElementById('sandbox-hint');
  const sandboxItemHouse = document.getElementById('sandbox-item-house');
  const sandboxItemChair = document.getElementById('sandbox-item-chair');
  const sandboxItemDoll = document.getElementById('sandbox-item-doll');
  const sandboxItemHay = document.getElementById('sandbox-item-hay');
  const sandboxItemBag = document.getElementById('sandbox-item-bag');
  const stateBtns = document.querySelectorAll('.state-btn');
  const itemBtns = document.querySelectorAll('.item-btn');
  let sandboxCurrentItem = 'none';

  if (sandboxPet && sandboxImg) {
    let sandboxSpeechTimer = null;
    let currentState = 'idle';

    function setSandboxState(state, message) {
      currentState = state;
      sandboxImg.src = ASSETS[state] || ASSETS.idle;
      sandboxImg.className = state === 'confused' ? '' : `anim-${state === 'idle' ? 'breathe' : state === 'walk' ? 'hop' : state === 'stand' ? 'curious' : state === 'happy' ? 'happy' : 'sleep'}`;

      stateBtns.forEach(btn => {
        btn.classList.toggle('active', btn.dataset.state === state);
      });

      if (message) {
        showSandboxSpeech(message, 2200);
      }
    }

    function setSandboxItem(item) {
      sandboxCurrentItem = item || 'none';
      if (sandboxItemHouse) sandboxItemHouse.classList.toggle('hidden', item !== 'house');
      if (sandboxItemChair) sandboxItemChair.classList.toggle('hidden', item !== 'chair');
      if (sandboxItemDoll) sandboxItemDoll.classList.toggle('hidden', item !== 'doll');
      if (sandboxItemHay) sandboxItemHay.classList.toggle('hidden', item !== 'hay');
      if (sandboxItemBag) sandboxItemBag.classList.toggle('hidden', item !== 'bag');

      itemBtns.forEach(btn => {
        btn.classList.toggle('active', btn.dataset.item === item);
      });

      if (item === 'doll') {
        sandboxImg.style.transform = 'scaleX(-1)';
        setSandboxState('stand');
        playPopSound();
        showSandboxSpeech(randomItem(ITEM_PHRASES.doll), 2600);
      } else if (item === 'hay') {
        sandboxImg.style.transform = 'scaleX(1)';
        setSandboxState('happy');
        floatSandboxHeart();
        showSandboxSpeech(randomItem(ITEM_PHRASES.hay), 2500);
      } else if (item === 'chair') {
        sandboxImg.style.transform = '';
        setSandboxState('idle');
        floatSandboxHeart();
        showSandboxSpeech(randomItem(ITEM_PHRASES.chair), 2500);
      } else if (item === 'bag') {
        sandboxImg.style.transform = '';
        setSandboxState('happy');
        showSandboxSpeech(randomItem(ITEM_PHRASES.bag), 2500);
      } else if (item === 'house') {
        sandboxImg.style.transform = '';
        setSandboxState('idle');
        showSandboxSpeech(randomItem(ITEM_PHRASES.house), 2500);
      } else {
        sandboxImg.style.transform = '';
        setSandboxState('idle');
        showSandboxSpeech('✨', 1800);
      }
      if (sandboxHint) sandboxHint.style.opacity = '0';
    }

    function showSandboxSpeech(text, duration = 2000) {
      if (sandboxSpeechTimer) clearTimeout(sandboxSpeechTimer);
      sandboxSpeechText.textContent = text;
      sandboxSpeech.classList.remove('hidden');
      sandboxSpeechTimer = setTimeout(() => {
        sandboxSpeech.classList.add('hidden');
      }, duration);
    }

    function floatSandboxHeart() {
      playPopSound();
      const heart = document.createElement('span');
      heart.className = 'heart-float';
      heart.textContent = '♥';
      heart.style.left = '50%';
      heart.style.bottom = '40px';
      heart.style.setProperty('--dx', `${randomBetween(-30, 30)}px`);
      heart.style.setProperty('--rot', `${randomBetween(-28, 28)}deg`);
      sandboxHearts.appendChild(heart);
      setTimeout(() => heart.remove(), 1350);
    }

    function triggerSandboxPetting() {
      if (sandboxCurrentItem === 'doll') {
        sandboxImg.style.transform = 'scaleX(-1)';
        setSandboxState('stand');
        playPopSound();
        showSandboxSpeech(randomItem(ITEM_PHRASES.doll), 2600);
        if (sandboxHint) sandboxHint.style.opacity = '0';
        return;
      }
      setSandboxState('happy');
      floatSandboxHeart();
      setTimeout(floatSandboxHeart, 190);

      let phrase;
      if (sandboxCurrentItem === 'hay') phrase = randomItem(ITEM_PHRASES.hay);
      else if (sandboxCurrentItem === 'chair') phrase = randomItem(ITEM_PHRASES.chair);
      else if (sandboxCurrentItem === 'bag') phrase = randomItem(ITEM_PHRASES.bag);
      else if (sandboxCurrentItem === 'house') phrase = randomItem(ITEM_PHRASES.house);
      else phrase = randomItem(PET_PHRASES);

      showSandboxSpeech(phrase, 2200);
      if (sandboxHint) sandboxHint.style.opacity = '0';
      setTimeout(() => {
        if (currentState === 'happy') setSandboxState('idle');
      }, 2100);
    }

    // 클릭 상호작용
    sandboxPet.addEventListener('click', triggerSandboxPetting);

    // 마우스 문지르기(쓰다듬기) 상호작용
    let sandboxStrokeDistance = 0;
    let sandboxLastStrokeTime = 0;
    let sandboxLastStrokePos = null;
    let sandboxLastPetReactionTime = 0;

    sandboxPet.addEventListener('pointermove', (e) => {
      const now = Date.now();
      if (now - sandboxLastStrokeTime > 700) {
        sandboxStrokeDistance = 0;
      }
      sandboxLastStrokeTime = now;
      if (sandboxLastStrokePos) {
        sandboxStrokeDistance += Math.abs(e.clientX - sandboxLastStrokePos.x) + Math.abs(e.clientY - sandboxLastStrokePos.y);
      }
      sandboxLastStrokePos = { x: e.clientX, y: e.clientY };
      if (sandboxStrokeDistance > 60 && now - sandboxLastPetReactionTime > 2200) {
        sandboxLastPetReactionTime = now;
        sandboxStrokeDistance = 0;
        triggerSandboxPetting();
      }
    });

    itemBtns.forEach(btn => {
      btn.addEventListener('click', () => {
        const item = btn.dataset.item;
        setSandboxItem(item);
      });
    });

    // 상태 버튼 클릭 리스너
    const STATE_PHRASES = {
      idle: '🤍',
      walk: '🐾',
      stand: '❓',
      confused: '👀',
      happy: '💖',
      sleep: '💤'
    };

    stateBtns.forEach(btn => {
      btn.addEventListener('click', () => {
        const state = btn.dataset.state;
        setSandboxState(state, STATE_PHRASES[state]);
        if (state === 'happy') {
          floatSandboxHeart();
        }
        if (sandboxHint) sandboxHint.style.opacity = '0';
      });
    });

    // 초기 상태
    setSandboxState('idle', '👋');
  }

})();
