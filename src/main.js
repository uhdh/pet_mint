'use strict';

const { app, BrowserWindow, ipcMain, Menu, Tray, screen, nativeImage } = require('electron');
const fs = require('node:fs');
const path = require('node:path');

const WINDOW_WIDTH = 107;
const WINDOW_HEIGHT = 100;
const MARGIN = 4;

let petWindow = null;
let tray = null;
let isPaused = false;
let settings = { alwaysOnTop: true, autoStart: false };

function settingsPath() {
  return path.join(app.getPath('userData'), 'settings.json');
}

function loadSettings() {
  try {
    settings = { ...settings, ...JSON.parse(fs.readFileSync(settingsPath(), 'utf8')) };
  } catch {
    // Defaults are used on first launch or after a damaged settings file.
  }
  settings.autoStart = process.platform === 'win32' && !process.windowsStore
    ? app.getLoginItemSettings().openAtLogin
    : false;
}

function saveSettings() {
  fs.mkdirSync(path.dirname(settingsPath()), { recursive: true });
  fs.writeFileSync(settingsPath(), JSON.stringify(settings, null, 2));
}

function activeWorkAreaFor(bounds) {
  const point = {
    x: Math.round(bounds.x + bounds.width / 2),
    y: Math.round(bounds.y + bounds.height / 2)
  };
  return screen.getDisplayNearestPoint(point).workArea;
}

function clampPosition(x, y, bounds = petWindow.getBounds()) {
  const area = activeWorkAreaFor({ ...bounds, x, y });
  return {
    x: Math.max(area.x, Math.min(x, area.x + area.width - bounds.width)),
    y: Math.max(area.y, Math.min(y, area.y + area.height - bounds.height)),
    workArea: area
  };
}

function resetPosition() {
  if (!petWindow) return;
  const area = screen.getPrimaryDisplay().workArea;
  petWindow.setPosition(
    area.x + area.width - WINDOW_WIDTH - MARGIN,
    area.y + area.height - WINDOW_HEIGHT - MARGIN,
    true
  );
}

function createWindow() {
  const area = screen.getPrimaryDisplay().workArea;
  petWindow = new BrowserWindow({
    width: WINDOW_WIDTH,
    height: WINDOW_HEIGHT,
    x: area.x + area.width - WINDOW_WIDTH - MARGIN,
    y: area.y + area.height - WINDOW_HEIGHT - MARGIN,
    transparent: true,
    frame: false,
    resizable: false,
    maximizable: false,
    fullscreenable: false,
    show: false,
    hasShadow: false,
    alwaysOnTop: settings.alwaysOnTop,
    skipTaskbar: true,
    backgroundColor: '#00000000',
    webPreferences: {
      preload: path.join(__dirname, 'preload.js'),
      contextIsolation: true,
      nodeIntegration: false,
      sandbox: true
    }
  });

  petWindow.setAlwaysOnTop(settings.alwaysOnTop, 'floating');
  petWindow.webContents.setWindowOpenHandler(() => ({ action: 'deny' }));
  petWindow.webContents.on('will-navigate', (event) => event.preventDefault());
  petWindow.loadFile(path.join(__dirname, 'index.html'));
  petWindow.once('ready-to-show', () => petWindow.showInactive());
  petWindow.on('closed', () => { petWindow = null; });
}

function sendPauseState() {
  petWindow?.webContents.send('pet:pause-changed', isPaused);
}

function refreshTrayMenu() {
  tray?.setContextMenu(buildMenu());
}

function buildMenu() {
  return Menu.buildFromTemplate([
    {
      label: '🖐️ 민트 쓰다듬기',
      click: () => petWindow?.webContents.send('pet:pet-requested')
    },
    { type: 'separator' },
    {
      label: '🎁 민트에게 선물하기',
      submenu: [
        { label: '🌾 맛있는 건초 주기', click: () => petWindow?.webContents.send('pet:item-changed', 'hay') },
        { label: '🪑 작은 의자 놓기', click: () => petWindow?.webContents.send('pet:item-changed', 'chair') },
        { label: '🧸 토끼 인형 놓기', click: () => petWindow?.webContents.send('pet:item-changed', 'doll') },
        { label: '🎒 소풍 가방 메어주기', click: () => petWindow?.webContents.send('pet:item-changed', 'bag') },
        { label: '🏠 아늑한 집 지어주기', click: () => petWindow?.webContents.send('pet:item-changed', 'house') },
        { type: 'separator' },
        { label: '❌ 아이템 치우기', click: () => petWindow?.webContents.send('pet:item-changed', 'none') }
      ]
    },
    { type: 'separator' },
    {
      label: isPaused ? '▶ 민트 다시 움직이기' : '⏸ 민트 잠깐 멈추기',
      click: () => {
        isPaused = !isPaused;
        sendPauseState();
        refreshTrayMenu();
      }
    },
    { type: 'separator' },
    {
      label: '항상 위에 표시',
      type: 'checkbox',
      checked: settings.alwaysOnTop,
      click: (item) => {
        settings.alwaysOnTop = item.checked;
        petWindow?.setAlwaysOnTop(item.checked, 'floating');
        saveSettings();
        refreshTrayMenu();
      }
    },
    {
      label: 'Windows 시작 시 함께 실행',
      type: 'checkbox',
      checked: settings.autoStart,
      visible: process.platform === 'win32' && !process.windowsStore,
      click: (item) => {
        settings.autoStart = item.checked;
        app.setLoginItemSettings({ openAtLogin: item.checked, path: app.getPath('exe') });
        saveSettings();
        refreshTrayMenu();
      }
    },
    { label: '↩ 민트 자리로 부르기 (오른쪽 아래)', click: resetPosition },
    { type: 'separator' },
    { label: '👋 민트 재우기 (종료)', role: 'quit' }
  ]);
}

function createTray() {
  const trayPath = path.join(__dirname, '..', 'assets', 'icon.png');
  const trayImage = nativeImage.createFromPath(trayPath).resize({ width: 32, height: 32 });
  tray = new Tray(trayImage);
  tray.setToolTip('민트 키우기');
  tray.setContextMenu(buildMenu());
  tray.on('click', () => {
    if (!petWindow) {
      createWindow();
      return;
    }
    petWindow.isVisible() ? petWindow.hide() : petWindow.showInactive();
  });
}

function registerIpc() {
  ipcMain.handle('pet:move-by', (_event, dx, dy) => {
    if (!petWindow) return null;
    const before = petWindow.getBounds();
    const targetX = Math.round(before.x + Number(dx || 0));
    const targetY = Math.round(before.y + Number(dy || 0));
    const next = clampPosition(targetX, targetY, before);
    petWindow.setPosition(next.x, next.y);
    return {
      x: next.x,
      y: next.y,
      hitX: next.x !== targetX,
      hitY: next.y !== targetY,
      workArea: next.workArea
    };
  });

  ipcMain.handle('pet:get-state', () => ({
    paused: isPaused,
    settings,
    bounds: petWindow?.getBounds() || null
  }));

  ipcMain.on('pet:context-menu', () => {
    tray?.setContextMenu(buildMenu());
    buildMenu().popup({ window: petWindow });
  });

  ipcMain.on('pet:reset-position', resetPosition);
}

const gotLock = app.requestSingleInstanceLock();
if (!gotLock) {
  app.quit();
} else {
  process.on('SIGTERM', () => app.quit());
  process.on('SIGINT', () => app.quit());

  app.on('second-instance', () => {
    if (petWindow) {
      petWindow.showInactive();
      petWindow.focus();
    }
  });

  app.whenReady().then(() => {
    loadSettings();
    registerIpc();
    createWindow();
    createTray();
  });

  app.on('window-all-closed', () => {
    // Keep the tray process alive if the pet window is ever closed unexpectedly.
  });

  app.on('activate', () => {
    if (!petWindow) createWindow();
  });
}
