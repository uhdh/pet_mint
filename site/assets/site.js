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
    confused: 'assets/bunny-confused.gif',
    beg: 'assets/bunny-beg.png',
    angry: 'assets/bunny-angry.png',
    front: 'assets/bunny-front.png',
    intro: 'assets/bunny-intro.png',
    binky: 'assets/bunny-binky.png',
    kiss: 'assets/bunny-kiss.png',
    wash: 'assets/bunny-wash.png',
    flop: 'assets/bunny-flop.png'
  };

  const INTRO_PHRASES = [
    '✨', '🤍', '🕊️', '🐰', '⭐', '🎉', '💖', '🥰'
  ];

  const BINKY_PHRASES = [
    '🤸', '🎉', '✨', '신나! 🐾', '너무 좋아! 🎈', '폴짝! 🐇', '빙키! 💫'
  ];

  const KISS_PHRASES = [
    '💋', '💖', '쪽! 💋', '사랑해! 🤍', '친해져서 좋아! 💕', '뽀뽀~ 😚', '헤헤 🥰'
  ];

  const WASH_PHRASES = [
    '🧼', '✨', '🌸', '쓱싹쓱싹 🧼', '세수하는 중 🫧', '개운해! 🤍', '단장 완료 ✨'
  ];

  const FLOP_PHRASES = [
    '🛌', '💤', '🤍', '안전해~ ☁️', '벌러덩~ 😴', '편안해요 🤍', '나른해~ 🌾'
  ];

  const SPAM_ANGRY_PHRASES = [
    '그만 찔러! 😤', '민트 뿔났다! ⚡', '발로 쿵쿵! 😾', '아야! 괴롭히지 마! 💢', '화났어! 😡'
  ];

  const PET_PHRASES = [
    '💕', '🥰', '💖', '💗', '💓', '💞', '😻', '🌸', '✨', '❤️', '🧡', '💛', '🤍', '😍', '😚', '💘'
  ];

  const CURSOR_PHRASES = [
    '👀', '❓', '👋', '🐰', '😳', '✨', '⭐', '💡', '🔍', '🧐', '🤍', '🐾', '😮', '💫', '👽', '👾'
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

  const BEG_PHRASES = [
    '🌾', '😋', '🥕', '🤤', '🍽️', '🥺', '👀'
  ];

  const ANGRY_PHRASES = [
    '💢', '😡', '😤', '⚡', '👿', '😾', '😠'
  ];

  const FRONT_PHRASES = [
    '🐰', '👀', '✨', '🤍', '🐾', '⭐', '👽', '🛸'
  ];

  const RESTING_PHRASES = [
    '🍞', '🍞', '💤', '💤', '😴', '☁️', '🥐', '🥱', '🌾', '🌙', '✨'
  ];

  const PURR_PHRASES = [
    '🥰', '💖', '💕', '🌸', '😻', '💓', '💗'
  ];

  const PLAY_PHRASES = [
    '🎉', '🏃', '✨', '🐾', '🎈', '👀', '🥳', '👽'
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
     호감도 및 해금 시스템 (Bunny Progression System)
     ========================================================================== */
  const BUNNY_PROGRESSION = {
    items: {
      hay: 0,
      chair: 10,
      doll: 25,
      bag: 40,
      house: 60
    },
    states: {
      intro: 0,
      front: 0,
      idle: 0,
      walk: 0,
      stand: 0,
      sleep: 0,
      happy: 0,
      beg: 5,
      wash: 15,
      angry: 20,
      kiss: 30,
      binky: 50,
      confused: 70,
      flop: 85
    },
    milestones: [
      { threshold: 5, name: '간식 내놔! 포즈 🌾' },
      { threshold: 10, name: '작은 의자 선물 🪑' },
      { threshold: 15, name: '손으로 세수하기 포즈 🧼' },
      { threshold: 20, name: '화났어! 포즈 💢' },
      { threshold: 25, name: '토끼 인형 선물 🧸' },
      { threshold: 30, name: '래빗키스 뽀뽀 포즈 💋' },
      { threshold: 40, name: '소풍 가방 선물 🎒' },
      { threshold: 50, name: '신나는 점프 빙키 포즈 🤸' },
      { threshold: 60, name: '아늑한 집 선물 🏠' },
      { threshold: 70, name: '어리둥절 실사 영상 포즈 👀' },
      { threshold: 85, name: '안심 벌러덩 눕기 포즈 🛌' }
    ],
    getLevelName(aff) {
      if (aff >= 85) return 'Lv.5 가족이 된 민트 💖';
      if (aff >= 60) return 'Lv.4 무한 애정 민트 💕';
      if (aff >= 30) return 'Lv.3 단짝 친구 민트 🥰';
      if (aff >= 10) return 'Lv.2 마음을 여는 민트 🌸';
      return 'Lv.1 낯가리는 민트 🌱';
    },
    getNextUnlock(aff) {
      if (aff < 5) return '다음 해금: 간식 내놔! (5점) 🌾';
      if (aff < 10) return '다음 해금: 작은 의자 (10점) 🪑';
      if (aff < 15) return '다음 해금: 세수하기 (15점) 🧼';
      if (aff < 20) return '다음 해금: 화났어! (20점) 💢';
      if (aff < 25) return '다음 해금: 토끼 인형 (25점) 🧸';
      if (aff < 30) return '다음 해금: 래빗키스 뽀뽀 (30점) 💋';
      if (aff < 40) return '다음 해금: 소풍 가방 (40점) 🎒';
      if (aff < 50) return '다음 해금: 신나는 점프 빙키 (50점) 🤸';
      if (aff < 60) return '다음 해금: 아늑한 집 (60점) 🏠';
      if (aff < 70) return '다음 해금: 어리둥절 실사 영상 (70점) 👀';
      if (aff < 85) return '다음 해금: 안심 벌러덩 눕기 (85점) 🛌';
      return '모든 특별 포즈와 선물 해금 완료! 👑';
    },
    isItemUnlocked(item, aff) {
      return aff >= (this.items[item] ?? 0);
    },
    isStateUnlocked(state, aff) {
      return aff >= (this.states[state] ?? 0);
    }
  };

  let globalAffinity = parseInt(localStorage.getItem('mint-affinity') || '10', 10);
  if (isNaN(globalAffinity)) globalAffinity = 10;

  function updateAffinityUI() {
    const levelName = BUNNY_PROGRESSION.getLevelName(globalAffinity);
    const nextDesc = BUNNY_PROGRESSION.getNextUnlock(globalAffinity);

    const simBadge = document.getElementById('sim-affinity-badge');
    if (simBadge) {
      simBadge.textContent = `💖 ${globalAffinity}점 (${levelName.split(' ')[0]} 🌸)`;
    }

    const sbVal = document.getElementById('sandbox-affinity-val');
    if (sbVal) sbVal.textContent = `💖 민트와의 호감도: ${globalAffinity}점`;
    const sbLevel = document.getElementById('sandbox-affinity-level');
    if (sbLevel) sbLevel.textContent = `(${levelName})`;
    const sbNext = document.getElementById('sandbox-affinity-next');
    if (sbNext) sbNext.textContent = `💡 ${nextDesc}`;

    // Update buttons in demo mockup
    const binkyBtn = document.getElementById('sim-mock-binky-btn');
    if (binkyBtn) binkyBtn.textContent = BUNNY_PROGRESSION.isStateUnlocked('binky', globalAffinity) ? '🤸 빙키' : '🔒 빙키(50)';
    const kissBtn = document.getElementById('sim-mock-kiss-btn');
    if (kissBtn) kissBtn.textContent = BUNNY_PROGRESSION.isStateUnlocked('kiss', globalAffinity) ? '💋 뽀뽀' : '🔒 뽀뽀(30)';
    const washBtn = document.getElementById('sim-mock-wash-btn');
    if (washBtn) washBtn.textContent = BUNNY_PROGRESSION.isStateUnlocked('wash', globalAffinity) ? '🧼 세수' : '🔒 세수(15)';
    const flopBtn = document.getElementById('sim-mock-flop-btn');
    if (flopBtn) flopBtn.textContent = BUNNY_PROGRESSION.isStateUnlocked('flop', globalAffinity) ? '🛌 벌러덩' : '🔒 벌러덩(85)';
    const confusedBtn = document.getElementById('sim-mock-confused-btn');
    if (confusedBtn) confusedBtn.textContent = BUNNY_PROGRESSION.isStateUnlocked('confused', globalAffinity) ? '👀 어리둥절' : '🔒 어리둥절(70)';

    const chairBtn = document.getElementById('sim-chair-btn');
    if (chairBtn) chairBtn.textContent = BUNNY_PROGRESSION.isItemUnlocked('chair', globalAffinity) ? '🪑 의자' : '🔒 의자(10)';
    const dollBtn = document.getElementById('sim-doll-btn');
    if (dollBtn) dollBtn.textContent = BUNNY_PROGRESSION.isItemUnlocked('doll', globalAffinity) ? '🧸 인형' : '🔒 인형(25)';
    const hayBtn = document.getElementById('sim-hay-btn');
    if (hayBtn) hayBtn.textContent = '🌾 건초';

    // Update sandbox buttons
    document.querySelectorAll('.state-btn').forEach(btn => {
      const st = btn.dataset.state;
      if (st && BUNNY_PROGRESSION.states[st] > 0) {
        const unl = BUNNY_PROGRESSION.isStateUnlocked(st, globalAffinity);
        btn.classList.toggle('locked-btn', !unl);
      }
    });
    document.querySelectorAll('.item-btn').forEach(btn => {
      const it = btn.dataset.item;
      if (it && it !== 'none' && BUNNY_PROGRESSION.items[it] > 0) {
        const unl = BUNNY_PROGRESSION.isItemUnlocked(it, globalAffinity);
        btn.classList.toggle('locked-btn', !unl);
      }
    });
  }

  function changeAffinity(delta, onCelebration) {
    const oldAff = globalAffinity;
    globalAffinity = Math.max(0, Math.min(100, globalAffinity + delta));
    localStorage.setItem('mint-affinity', String(globalAffinity));
    updateAffinityUI();

    if (delta > 0) {
      let newlyUnlocked = null;
      for (const m of BUNNY_PROGRESSION.milestones) {
        if (oldAff < m.threshold && globalAffinity >= m.threshold) {
          newlyUnlocked = m.name;
        }
      }
      if (newlyUnlocked && onCelebration) {
        onCelebration(newlyUnlocked, globalAffinity);
      }
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

    function triggerSimCelebration(name, aff) {
      for (let i = 0; i < 5; i++) {
        setTimeout(floatSimHeart, i * 140);
      }
      showSimSpeech(`🎉 호감도 ${aff}점 달성! 🎉\n✨ ${name} 해금! ✨`, 3800, true);
    }

    function setSimItem(item) {
      if (item && item !== 'none' && !BUNNY_PROGRESSION.isItemUnlocked(item, globalAffinity)) {
        const req = BUNNY_PROGRESSION.items[item] || 0;
        showSimSpeech(`아직 덜 친해요... 🔒\n(호감도 ${req}점 필요! 🥺)`, 3000, true);
        return;
      }

      simCurrentItem = item || 'none';
      if (simItemHouse) simItemHouse.classList.toggle('hidden', item !== 'house');
      if (simItemChair) simItemChair.classList.toggle('hidden', item !== 'chair');
      if (simItemDoll) simItemDoll.classList.toggle('hidden', item !== 'doll');
      if (simItemHay) simItemHay.classList.toggle('hidden', item !== 'hay');
      if (simItemBag) simItemBag.classList.toggle('hidden', item !== 'bag');

      if (simDollBtn) simDollBtn.classList.toggle('active', item === 'doll');
      if (simHayBtn) simHayBtn.classList.toggle('active', item === 'hay');
      if (simChairBtn) simChairBtn.classList.toggle('active', item === 'chair');

      if (item === 'house') {
        simSprite.classList.add('inside-house');
        simSprite.classList.remove('on-chair');
      } else if (item === 'chair') {
        simSprite.classList.add('on-chair');
        simSprite.classList.remove('inside-house');
      } else {
        simSprite.classList.remove('inside-house');
        simSprite.classList.remove('on-chair');
      }

      stopSimWalking();

      if (item === 'hay') {
        changeAffinity(2, triggerSimCelebration);
      } else if (item && item !== 'none') {
        changeAffinity(1, triggerSimCelebration);
      }

      if (item === 'doll') {
        simDirection = -1;
        updateSimTransform();
        setSimState('angry');
        playPopSound();
        showSimSpeech(randomItem(ITEM_PHRASES.doll), 2600, true);
        scheduleSimBehavior(3000);
      } else if (item === 'hay') {
        simDirection = 1;
        updateSimTransform();
        setSimState('beg');
        floatSimHeart();
        showSimSpeech(randomItem(ITEM_PHRASES.hay), 2500, true);
        scheduleSimBehavior(2800);
      } else if (item === 'chair') {
        simDirection = 1;
        updateSimTransform();
        setSimState('idle');
        floatSimHeart();
        showSimSpeech(randomItem(ITEM_PHRASES.chair), 2500, true);
        scheduleSimBehavior(2800);
      } else if (item === 'bag') {
        simDirection = 1;
        updateSimTransform();
        setSimState('idle');
        floatSimHeart();
        showSimSpeech(randomItem(ITEM_PHRASES.bag), 2500, true);
        scheduleSimBehavior(2800);
      } else if (item === 'house') {
        simDirection = 1;
        updateSimTransform();
        setSimState('front');
        floatSimHeart();
        showSimSpeech(randomItem(ITEM_PHRASES.house), 2500, true);
        scheduleSimBehavior(3200);
      } else {
        setSimState('idle');
        showSimSpeech('✨', 1800, true);
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
      let assetKey = next;
      if (simCurrentItem === 'house') {
        assetKey = 'front';
      } else if (!simPlayMode && (next === 'idle' || next === 'sleep')) {
        assetKey = 'sleep';
      }
      if (ASSETS[assetKey]) {
        simImg.src = ASSETS[assetKey];
      }
      simSprite.className = `sim-bunny-sprite ${simCurrentItem === 'house' ? 'inside-house' : ''} ${simDirection < 0 ? 'facing-left' : ''} anim-${(!simPlayMode && next === 'idle') || next === 'sleep' ? 'sleep' : next === 'idle' ? 'breathe' : next === 'stand' || next === 'beg' ? 'curious' : next === 'happy' ? 'happy' : next === 'angry' ? 'angry' : ''}`;
    }

    function hideSimSpeech() {
      if (simSpeechTimer) clearTimeout(simSpeechTimer);
      simSpeechTimer = null;
      simSpeech.classList.add('hidden');
    }

    function showSimSpeech(text, duration = 2000, force = false) {
      if (!simPlayMode && !force) return;
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
      if (simCurrentItem === 'house') return;
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

        if (simCurrentItem === 'house') {
          setSimState('front');
          if (Math.random() < 0.25) floatSimHeart();
          scheduleSimBehavior(randomBetween(4000, 8000));
          return;
        }

        if (!simPlayMode) {
          if (BUNNY_PROGRESSION.isStateUnlocked('flop', globalAffinity) && Math.random() < 0.28) {
            setSimState('flop');
            floatSimHeart();
            showSimSpeech(randomItem(FLOP_PHRASES), 3000, true);
            scheduleSimBehavior(randomBetween(4500, 7500));
            return;
          }
          if (BUNNY_PROGRESSION.isStateUnlocked('wash', globalAffinity) && Math.random() < 0.22) {
            setSimState('wash');
            showSimSpeech(randomItem(WASH_PHRASES), 2600, true);
            scheduleSimBehavior(randomBetween(4000, 6500));
            return;
          }
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
        if (roll < 0.28) {
          setSimState('idle');
          scheduleSimBehavior(randomBetween(3500, 6000));
        } else if (roll < 0.52) {
          setSimState('walk');
          startSimWalking();
          scheduleSimBehavior(randomBetween(3500, 7000));
        } else if (roll < 0.64) {
          setSimState('stand');
          scheduleSimBehavior(randomBetween(2500, 4500));
        } else if (roll < 0.74 && BUNNY_PROGRESSION.isStateUnlocked('binky', globalAffinity)) {
          setSimState('binky');
          floatSimHeart();
          showSimSpeech(randomItem(BINKY_PHRASES), 2600);
          scheduleSimBehavior(3200);
        } else if (roll < 0.82 && BUNNY_PROGRESSION.isStateUnlocked('wash', globalAffinity)) {
          setSimState('wash');
          showSimSpeech(randomItem(WASH_PHRASES), 2600);
          scheduleSimBehavior(3200);
        } else if (roll < 0.89 && BUNNY_PROGRESSION.isStateUnlocked('beg', globalAffinity)) {
          setSimState('beg');
          showSimSpeech(randomItem(BEG_PHRASES), 2600);
          scheduleSimBehavior(3200);
        } else if (roll < 0.95 && BUNNY_PROGRESSION.isStateUnlocked('confused', globalAffinity)) {
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

    const simClickHistory = [];

    function reactSimPetting() {
      stopSimWalking();

      const now = Date.now();
      simClickHistory.push(now);
      while (simClickHistory.length && now - simClickHistory[0] > 1800) {
        simClickHistory.shift();
      }
      if (simClickHistory.length >= 4) {
        simClickHistory.length = 0;
        changeAffinity(-2);
        setSimState('angry');
        playPopSound();
        showSimSpeech(randomItem(SPAM_ANGRY_PHRASES), 2600, true);
        scheduleSimBehavior(3500);
        return;
      }

      changeAffinity(1, triggerSimCelebration);

      if (simCurrentItem === 'house') {
        floatSimHeart();
        showSimSpeech(randomItem(ITEM_PHRASES.house), 2500, true);
        scheduleSimBehavior(3000);
        return;
      }

      if (simCurrentItem === 'doll') {
        simDirection = -1;
        updateSimTransform();
        setSimState('angry');
        playPopSound();
        showSimSpeech(randomItem(ITEM_PHRASES.doll), 2600, true);
        scheduleSimBehavior(3000);
        return;
      }

      if (simCurrentItem === 'hay') {
        simDirection = 1;
        updateSimTransform();
        setSimState('beg');
        floatSimHeart();
        showSimSpeech(randomItem(ITEM_PHRASES.hay), 2500, true);
        scheduleSimBehavior(2800);
        return;
      }

      if (BUNNY_PROGRESSION.isStateUnlocked('kiss', globalAffinity) && Math.random() < 0.42) {
        setSimState('kiss');
        floatSimHeart();
        setTimeout(floatSimHeart, 180);
        showSimSpeech(randomItem(KISS_PHRASES), 2800, true);
        scheduleSimBehavior(3400);
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
      if (simCurrentItem === 'chair') phrase = randomItem(ITEM_PHRASES.chair);
      else if (simCurrentItem === 'bag') phrase = randomItem(ITEM_PHRASES.bag);
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
        if (!BUNNY_PROGRESSION.isStateUnlocked('confused', globalAffinity)) {
          showSimSpeech('아직 덜 친해요... 🔒\n(호감도 70점 필요! 🥺)', 3000, true);
          return;
        }
        stopSimWalking();
        setSimState('confused');
        showSimSpeech('👽', 2200, true);
        scheduleSimBehavior(3300);
      });
    });

    const binkyBtns = document.querySelectorAll('#sim-mock-binky-btn');
    binkyBtns.forEach(btn => {
      btn.addEventListener('click', () => {
        if (!BUNNY_PROGRESSION.isStateUnlocked('binky', globalAffinity)) {
          showSimSpeech('아직 덜 친해요... 🔒\n(호감도 50점 필요! 🥺)', 3000, true);
          return;
        }
        stopSimWalking();
        setSimState('binky');
        floatSimHeart();
        showSimSpeech(randomItem(BINKY_PHRASES), 2600, true);
        scheduleSimBehavior(3200);
      });
    });

    const kissBtns = document.querySelectorAll('#sim-mock-kiss-btn');
    kissBtns.forEach(btn => {
      btn.addEventListener('click', () => {
        if (!BUNNY_PROGRESSION.isStateUnlocked('kiss', globalAffinity)) {
          showSimSpeech('아직 덜 친해요... 🔒\n(호감도 30점 필요! 🥺)', 3000, true);
          return;
        }
        stopSimWalking();
        setSimState('kiss');
        floatSimHeart();
        showSimSpeech(randomItem(KISS_PHRASES), 2600, true);
        scheduleSimBehavior(3200);
      });
    });

    const washBtns = document.querySelectorAll('#sim-mock-wash-btn');
    washBtns.forEach(btn => {
      btn.addEventListener('click', () => {
        if (!BUNNY_PROGRESSION.isStateUnlocked('wash', globalAffinity)) {
          showSimSpeech('아직 덜 친해요... 🔒\n(호감도 15점 필요! 🥺)', 3000, true);
          return;
        }
        stopSimWalking();
        setSimState('wash');
        showSimSpeech(randomItem(WASH_PHRASES), 2600, true);
        scheduleSimBehavior(3200);
      });
    });

    const flopBtns = document.querySelectorAll('#sim-mock-flop-btn');
    flopBtns.forEach(btn => {
      btn.addEventListener('click', () => {
        if (!BUNNY_PROGRESSION.isStateUnlocked('flop', globalAffinity)) {
          showSimSpeech('아직 덜 친해요... 🔒\n(호감도 85점 필요! 🥺)', 3000, true);
          return;
        }
        stopSimWalking();
        setSimState('flop');
        floatSimHeart();
        showSimSpeech(randomItem(FLOP_PHRASES), 3000, true);
        scheduleSimBehavior(4500);
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

    function triggerSandboxCelebration(name, aff) {
      for (let i = 0; i < 5; i++) {
        setTimeout(floatSandboxHeart, i * 140);
      }
      showSandboxSpeech(`🎉 호감도 ${aff}점 달성! 🎉\n✨ ${name} 해금! ✨`, 3800);
    }

    function setSandboxState(state, message) {
      if (!BUNNY_PROGRESSION.isStateUnlocked(state, globalAffinity)) {
        const req = BUNNY_PROGRESSION.states[state] || 0;
        showSandboxSpeech(`아직 덜 친해요... 🔒\n(호감도 ${req}점 필요! 🥺)`, 3000);
        return;
      }

      currentState = state;
      sandboxImg.src = ASSETS[state] || ASSETS.idle;
      sandboxImg.className = (state === 'confused' || state === 'front' || state === 'beg' || state === 'angry' || state === 'intro' || state === 'binky' || state === 'kiss' || state === 'wash' || state === 'flop') ? '' : `anim-${state === 'idle' ? 'breathe' : state === 'walk' ? 'hop' : state === 'stand' ? 'curious' : state === 'happy' ? 'happy' : 'sleep'}`;

      stateBtns.forEach(btn => {
        btn.classList.toggle('active', btn.dataset.state === state);
      });

      if (state === 'intro' || state === 'binky' || state === 'kiss' || state === 'flop') {
        floatSandboxHeart();
        if (!message) {
          if (state === 'binky') showSandboxSpeech(randomItem(BINKY_PHRASES), 2500);
          else if (state === 'kiss') showSandboxSpeech(randomItem(KISS_PHRASES), 2500);
          else if (state === 'wash') showSandboxSpeech(randomItem(WASH_PHRASES), 2500);
          else if (state === 'flop') showSandboxSpeech(randomItem(FLOP_PHRASES), 2500);
          else showSandboxSpeech(randomItem(INTRO_PHRASES), 2500);
        }
      }

      if (message) {
        showSandboxSpeech(message, 2200);
      }
    }

    function setSandboxItem(item) {
      if (item && item !== 'none' && !BUNNY_PROGRESSION.isItemUnlocked(item, globalAffinity)) {
        const req = BUNNY_PROGRESSION.items[item] || 0;
        showSandboxSpeech(`아직 덜 친해요... 🔒\n(호감도 ${req}점 필요! 🥺)`, 3000);
        return;
      }

      sandboxCurrentItem = item || 'none';
      if (sandboxItemHouse) sandboxItemHouse.classList.toggle('hidden', item !== 'house');
      if (sandboxItemChair) sandboxItemChair.classList.toggle('hidden', item !== 'chair');
      if (sandboxItemDoll) sandboxItemDoll.classList.toggle('hidden', item !== 'doll');
      if (sandboxItemHay) sandboxItemHay.classList.toggle('hidden', item !== 'hay');
      if (sandboxItemBag) sandboxItemBag.classList.toggle('hidden', item !== 'bag');

      itemBtns.forEach(btn => {
        btn.classList.toggle('active', btn.dataset.item === item);
      });

      if (item === 'hay') {
        changeAffinity(2, triggerSandboxCelebration);
      } else if (item && item !== 'none') {
        changeAffinity(1, triggerSandboxCelebration);
      }

      if (item === 'house') {
        sandboxPet.classList.add('inside-house');
        sandboxPet.classList.remove('on-chair');
      } else if (item === 'chair') {
        sandboxPet.classList.add('on-chair');
        sandboxPet.classList.remove('inside-house');
      } else {
        sandboxPet.classList.remove('inside-house');
        sandboxPet.classList.remove('on-chair');
      }

      if (item === 'doll') {
        sandboxImg.style.transform = 'scaleX(-1)';
        setSandboxState('angry');
        playPopSound();
        showSandboxSpeech(randomItem(ITEM_PHRASES.doll), 2600);
      } else if (item === 'hay') {
        sandboxImg.style.transform = 'scaleX(1)';
        setSandboxState('beg');
        floatSandboxHeart();
        showSandboxSpeech(randomItem(ITEM_PHRASES.hay), 2500);
      } else if (item === 'chair') {
        sandboxImg.style.transform = 'scaleX(1)';
        setSandboxState('idle');
        floatSandboxHeart();
        showSandboxSpeech(randomItem(ITEM_PHRASES.chair), 2500);
      } else if (item === 'bag') {
        sandboxImg.style.transform = 'scaleX(1)';
        setSandboxState('idle');
        floatSandboxHeart();
        showSandboxSpeech(randomItem(ITEM_PHRASES.bag), 2500);
      } else if (item === 'house') {
        sandboxImg.style.transform = '';
        setSandboxState('front');
        floatSandboxHeart();
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

    const sandboxClickHistory = [];

    function triggerSandboxPetting() {
      const now = Date.now();
      sandboxClickHistory.push(now);
      while (sandboxClickHistory.length && now - sandboxClickHistory[0] > 1800) {
        sandboxClickHistory.shift();
      }
      if (sandboxClickHistory.length >= 4) {
        sandboxClickHistory.length = 0;
        changeAffinity(-2);
        setSandboxState('angry');
        playPopSound();
        showSandboxSpeech(randomItem(SPAM_ANGRY_PHRASES), 2600);
        if (sandboxHint) sandboxHint.style.opacity = '0';
        return;
      }

      changeAffinity(1, triggerSandboxCelebration);

      if (sandboxCurrentItem === 'house') {
        floatSandboxHeart();
        showSandboxSpeech(randomItem(ITEM_PHRASES.house), 2500);
        return;
      }
      if (sandboxCurrentItem === 'doll') {
        sandboxImg.style.transform = 'scaleX(-1)';
        setSandboxState('angry');
        playPopSound();
        showSandboxSpeech(randomItem(ITEM_PHRASES.doll), 2600);
        if (sandboxHint) sandboxHint.style.opacity = '0';
        return;
      }
      if (sandboxCurrentItem === 'hay') {
        sandboxImg.style.transform = 'scaleX(1)';
        setSandboxState('beg');
        floatSandboxHeart();
        showSandboxSpeech(randomItem(ITEM_PHRASES.hay), 2500);
        if (sandboxHint) sandboxHint.style.opacity = '0';
        return;
      }

      if (BUNNY_PROGRESSION.isStateUnlocked('kiss', globalAffinity) && Math.random() < 0.42) {
        setSandboxState('kiss');
        floatSandboxHeart();
        setTimeout(floatSandboxHeart, 180);
        showSandboxSpeech(randomItem(KISS_PHRASES), 2800);
        if (sandboxHint) sandboxHint.style.opacity = '0';
        return;
      }

      setSandboxState('happy');
      floatSandboxHeart();
      setTimeout(floatSandboxHeart, 190);

      let phrase;
      if (sandboxCurrentItem === 'chair') phrase = randomItem(ITEM_PHRASES.chair);
      else if (sandboxCurrentItem === 'bag') phrase = randomItem(ITEM_PHRASES.bag);
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
      confused: '👽',
      beg: '🌾',
      angry: '💢',
      front: '🐰',
      happy: '💖',
      sleep: '💤',
      binky: '🤸',
      kiss: '💋',
      wash: '🧼',
      flop: '🛌'
    };

    stateBtns.forEach(btn => {
      btn.addEventListener('click', () => {
        const state = btn.dataset.state;
        setSandboxState(state, STATE_PHRASES[state]);
        if (state === 'happy' || state === 'binky' || state === 'kiss' || state === 'flop') {
          floatSandboxHeart();
        }
        if (sandboxHint) sandboxHint.style.opacity = '0';
      });
    });

    // 초기 상태
    setSandboxState('idle', '👋');

    // 호감도 UI 초기 동기화 및 2분마다 동반 보너스 +1
    updateAffinityUI();
    setInterval(() => {
      changeAffinity(1, triggerSandboxCelebration);
    }, 120000);
  }

})();

