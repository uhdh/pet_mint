'use strict';

const { contextBridge, ipcRenderer } = require('electron');

contextBridge.exposeInMainWorld('bunnyDesktop', {
  moveBy: (dx, dy) => ipcRenderer.invoke('pet:move-by', dx, dy),
  getState: () => ipcRenderer.invoke('pet:get-state'),
  showContextMenu: () => ipcRenderer.send('pet:context-menu'),
  resetPosition: () => ipcRenderer.send('pet:reset-position'),
  onPauseChanged: (callback) => {
    const listener = (_event, paused) => callback(paused);
    ipcRenderer.on('pet:pause-changed', listener);
    return () => ipcRenderer.removeListener('pet:pause-changed', listener);
  },
  onItemChanged: (callback) => {
    const listener = (_event, item) => callback(item);
    ipcRenderer.on('pet:item-changed', listener);
    return () => ipcRenderer.removeListener('pet:item-changed', listener);
  },
  onPetRequested: (callback) => {
    const listener = () => callback();
    ipcRenderer.on('pet:pet-requested', listener);
    return () => ipcRenderer.removeListener('pet:pet-requested', listener);
  }
});
