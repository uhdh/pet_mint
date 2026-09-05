using System;
using System.Diagnostics;
using System.Threading;
using System.Windows;
using Microsoft.Win32;
using System.Text;
using Forms = System.Windows.Forms;

namespace BunnyPet
{
    public partial class App : Application
    {
        private const string MutexName = "MyBunnyDesktopPet.SingleInstance";
        private const string ShowEventName = "MyBunnyDesktopPet.Show";
        private const string RunValueName = "MyBunnyDesktopPet";

        private Mutex instanceMutex;
        private EventWaitHandle showEvent;
        private RegisteredWaitHandle showRegistration;
        private MainWindow window;
        private Forms.NotifyIcon tray;
        private Forms.ToolStripMenuItem playToggleItem;
        private Forms.ToolStripMenuItem visibilityItem;
        private AppSettings settings;
        private bool quitting;
        private bool showPending;

        public void UpdatePlayToggle(bool isPlay)
        {
            if (playToggleItem != null)
            {
                playToggleItem.Text = isPlay ? "🛑 나대지마 (멈추기)" : "🎉 놀자! (움직이기)";
            }
        }

        public static void Log(string msg)
        {
            try
            {
                var dir = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "MyBunnyDesktopPet");
                System.IO.Directory.CreateDirectory(dir);
                System.IO.File.AppendAllText(System.IO.Path.Combine(dir, "run.log"), $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] {msg}\r\n");
            }
            catch { }
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            Log("OnStartup begin");
            AppDomain.CurrentDomain.UnhandledException += (s, ev) => Log("AppDomain UnhandledException: " + ev.ExceptionObject);
            DispatcherUnhandledException += (s, ev) => { Log("DispatcherUnhandledException: " + ev.Exception); ev.Handled = true; };

            base.OnStartup(e);

            var currentProc = Process.GetCurrentProcess();
            var others = Process.GetProcessesByName(currentProc.ProcessName);
            foreach (var other in others)
            {
                if (other.Id != currentProc.Id)
                {
                    try
                    {
                        Log("Terminating older instance PID: " + other.Id);
                        other.Kill();
                        other.WaitForExit(500);
                    }
                    catch (Exception ex)
                    {
                        Log("Could not terminate PID " + other.Id + ": " + ex.Message);
                    }
                }
            }

            EnsureShowEvent();
            try
            {
                instanceMutex = new Mutex(true, MutexName, out _);
            }
            catch (AbandonedMutexException ame)
            {
                Log("Mutex AbandonedMutexException: " + ame.Message);
            }
            catch (Exception ex)
            {
                Log("Mutex Exception: " + ex.Message);
            }

            Log("Loading settings");
            settings = AppSettings.Load();
            RegisterShowEvent();
            Log("Creating MainWindow");
            window = new MainWindow();
            MainWindow = window;
            window.SetAlwaysOnTop(settings.AlwaysOnTop);
            window.SetRestRemindersEnabled(settings.RestRemindersEnabled);
            Log("Showing window");
            window.Show();
            window.Activate();
            if (showPending) ShowWindow();
            Log("Creating Tray");
            CreateTray();
            Log("OnStartup completed");
        }

        private void EnsureShowEvent()
        {
            try
            {
                showEvent = new EventWaitHandle(false, EventResetMode.AutoReset, ShowEventName);
            }
            catch (Exception)
            {
                showEvent = null;
            }
        }

        private void RegisterShowEvent()
        {
            try
            {
                if (showEvent == null)
                    showEvent = new EventWaitHandle(false, EventResetMode.AutoReset, ShowEventName);
                showRegistration = ThreadPool.RegisterWaitForSingleObject(
                    showEvent,
                    OnShowSignal,
                    null,
                    Timeout.Infinite,
                    false);
            }
            catch (Exception)
            {
                DisposeShowEvent();
            }
        }

        private void OnShowSignal(object state, bool timedOut)
        {
            try
            {
                if (!timedOut && !quitting) Dispatcher.BeginInvoke(new Action(ShowWindow));
            }
            catch (Exception) { }
        }

        private void SignalExistingInstance()
        {
            try
            {
                if (showEvent != null) showEvent.Set();
                else using (var signal = EventWaitHandle.OpenExisting(ShowEventName)) signal.Set();
            }
            catch (Exception)
            {
                // A stale or inaccessible signal should not make a second launch crash.
            }
        }

        private void CreateTray()
        {
            try
            {
                var menu = new Forms.ContextMenuStrip();

                playToggleItem = new Forms.ToolStripMenuItem(window != null && window.IsPlayMode ? "🛑 나대지마 (멈추기)" : "🎉 놀자! (움직이기)");
                playToggleItem.Font = new System.Drawing.Font(menu.Font, System.Drawing.FontStyle.Bold);
                playToggleItem.Click += delegate
                {
                    if (window != null) window.TogglePlayMode();
                };
                menu.Items.Add(playToggleItem);
                menu.Items.Add(new Forms.ToolStripSeparator());

                var petItem = new Forms.ToolStripMenuItem("🖐️ 민트 쓰다듬기 (Pet Mint)", null, delegate { window.ReactToPetting(); });
                menu.Items.Add(petItem);

                var poseMenu = new Forms.ToolStripMenuItem("📸 민트 특별 포즈 (Special Poses)");

                var begItem = new Forms.ToolStripMenuItem("🌾 간식 내놔! 민트 (실사 포즈)", null, delegate
                {
                    if (window != null) window.TriggerPose(BunnyState.Beg);
                });
                poseMenu.DropDownItems.Add(begItem);

                var angryItem = new Forms.ToolStripMenuItem("💢 화났어! 민트 (실사 포즈)", null, delegate
                {
                    if (window != null) window.TriggerPose(BunnyState.Angry);
                });
                poseMenu.DropDownItems.Add(angryItem);

                var frontItem = new Forms.ToolStripMenuItem("🐰 똘망똘망 민트 (정면 포즈)", null, delegate
                {
                    if (window != null) window.TriggerPose(BunnyState.Front);
                });
                poseMenu.DropDownItems.Add(frontItem);

                var confusedItem = new Forms.ToolStripMenuItem("👀 어리둥절 민트 (실사 영상)", null, delegate
                {
                    if (window != null)
                    {
                        if (window.CurrentItem == BunnyItem.House) window.SetItem(BunnyItem.None);
                        window.PlayConfusedAnimation(() =>
                        {
                            window.ResetVisualToIdle();
                        });
                    }
                });
                poseMenu.DropDownItems.Add(confusedItem);

                menu.Items.Add(poseMenu);
                menu.Items.Add(new Forms.ToolStripSeparator());

                var itemsMenu = new Forms.ToolStripMenuItem("🎁 민트에게 선물하기");
                var hayItem = new Forms.ToolStripMenuItem("🌾 맛있는 건초 (Hay)", null, delegate { window.SetItem(BunnyItem.Hay); });
                var chairItem = new Forms.ToolStripMenuItem("🪑 작은 의자 (Chair)", null, delegate { window.SetItem(BunnyItem.Chair); });
                var dollItem = new Forms.ToolStripMenuItem("🧸 토끼 인형 (Plush Doll)", null, delegate { window.SetItem(BunnyItem.Doll); });
                var bagItem = new Forms.ToolStripMenuItem("🎒 소풍 가방 (Backpack)", null, delegate { window.SetItem(BunnyItem.Bag); });
                var houseItem = new Forms.ToolStripMenuItem("🏠 아늑한 집 (House)", null, delegate { window.SetItem(BunnyItem.House); });
                var clearItem = new Forms.ToolStripMenuItem("❌ 아이템 치우기 (Remove Item)", null, delegate { window.SetItem(BunnyItem.None); });

                itemsMenu.DropDownItems.Add(hayItem);
                itemsMenu.DropDownItems.Add(chairItem);
                itemsMenu.DropDownItems.Add(dollItem);
                itemsMenu.DropDownItems.Add(bagItem);
                itemsMenu.DropDownItems.Add(houseItem);
                itemsMenu.DropDownItems.Add(new Forms.ToolStripSeparator());
                itemsMenu.DropDownItems.Add(clearItem);
                menu.Items.Add(itemsMenu);
                menu.Items.Add(new Forms.ToolStripSeparator());

                var topmostItem = new Forms.ToolStripMenuItem("📌 항상 위에 표시 (Always on top)")
                {
                    CheckOnClick = true,
                    Checked = settings.AlwaysOnTop
                };
                topmostItem.Click += delegate
                {
                    settings.AlwaysOnTop = topmostItem.Checked;
                    window.SetAlwaysOnTop(settings.AlwaysOnTop);
                    SaveSettings();
                };
                menu.Items.Add(topmostItem);

                var autoStartItem = new Forms.ToolStripMenuItem("🚀 윈도우 시작 시 자동 실행 (Start with Windows)")
                {
                    CheckOnClick = true,
                    Checked = settings.AutoStart,
                    Enabled = !IsPackaged()
                };
                autoStartItem.Click += delegate
                {
                    settings.AutoStart = autoStartItem.Checked;
                    SetAutoStart(settings.AutoStart);
                    SaveSettings();
                };
                menu.Items.Add(autoStartItem);

                var restRemindersItem = new Forms.ToolStripMenuItem("🍵 휴식 알림 받기 (Rest reminders)")
                {
                    CheckOnClick = true,
                    Checked = settings.RestRemindersEnabled
                };
                restRemindersItem.Click += delegate
                {
                    settings.RestRemindersEnabled = restRemindersItem.Checked;
                    window.SetRestRemindersEnabled(settings.RestRemindersEnabled);
                    SaveSettings();
                };
                menu.Items.Add(restRemindersItem);

                var resetItem = new Forms.ToolStripMenuItem("↩ 민트 자리로 부르기 (Reset position)");
                resetItem.Click += delegate { window.ResetPosition(); };
                menu.Items.Add(resetItem);

                menu.Items.Add(new Forms.ToolStripSeparator());
                visibilityItem = new Forms.ToolStripMenuItem("민트 숨기기 (Hide Mint)");
                visibilityItem.Click += delegate { ToggleWindow(); };
                menu.Items.Add(visibilityItem);

                menu.Items.Add(new Forms.ToolStripSeparator());
                var backupItem = new Forms.ToolStripMenuItem("설정 백업하기 (Backup settings)...");
                backupItem.Click += delegate { BackupSettings(); };
                menu.Items.Add(backupItem);

                var restoreItem = new Forms.ToolStripMenuItem("설정 복원하기 (Restore settings)...");
                restoreItem.Click += delegate { RestoreSettings(topmostItem, autoStartItem, restRemindersItem); };
                menu.Items.Add(restoreItem);

                menu.Items.Add(new Forms.ToolStripSeparator());
                var quitItem = new Forms.ToolStripMenuItem("👋 민트 재우기 / 종료 (Quit)");
                quitItem.Click += delegate { Quit(); };
                menu.Items.Add(quitItem);

                tray = new Forms.NotifyIcon
                {
                    Text = "민트 키우기",
                    Icon = LoadTrayIcon(),
                    ContextMenuStrip = menu,
                    Visible = true
                };
                tray.MouseClick += delegate(object sender, Forms.MouseEventArgs args)
                {
                    if (args.Button == Forms.MouseButtons.Left) ToggleWindow();
                };
            }
            catch (Exception ex)
            {
                Log("CreateTray FAILED: " + ex);
                window.ExitOnClose = true;
                DisposeTray();
            }
        }

        internal void QuitFromWindow()
        {
            Quit();
        }

        private System.Drawing.Icon LoadTrayIcon()
        {
            var resource = GetResourceStream(new Uri("pack://application:,,,/BunnyPet;component/Assets/icon.ico", UriKind.Absolute));
            if (resource == null) throw new InvalidOperationException("Tray icon resource is missing.");
            using (resource.Stream)
            using (var source = new System.Drawing.Icon(resource.Stream))
                return new System.Drawing.Icon(source, source.Width, source.Height);
        }

        private void ToggleWindow()
        {
            if (window == null) return;
            try
            {
                if (window.IsVisible) window.Hide();
                else
                {
                    window.Show();
                    window.Activate();
                }
                if (visibilityItem != null) visibilityItem.Text = window.IsVisible ? "민트 숨기기 (Hide Mint)" : "민트 보이기 (Show Mint)";
            }
            catch (Exception) { }
        }

        private void BackupSettings()
        {
            using (var dialog = new Forms.SaveFileDialog
            {
                FileName = "mint-settings.json",
                Filter = "JSON (*.json)|*.json",
                DefaultExt = "json"
            })
            {
                if (dialog.ShowDialog() != Forms.DialogResult.OK) return;
                try { settings.SaveTo(dialog.FileName); }
                catch (Exception)
                {
                    System.Windows.MessageBox.Show("설정을 내보내지 못했습니다.", "민트 키우기");
                }
            }
        }

        private void RestoreSettings(Forms.ToolStripMenuItem topmostItem, Forms.ToolStripMenuItem autoStartItem, Forms.ToolStripMenuItem restRemindersItem)
        {
            using (var dialog = new Forms.OpenFileDialog
            {
                Filter = "JSON (*.json)|*.json",
                DefaultExt = "json"
            })
            {
                if (dialog.ShowDialog() != Forms.DialogResult.OK) return;
                try
                {
                    settings = AppSettings.LoadFrom(dialog.FileName);
                }
                catch (Exception)
                {
                    System.Windows.MessageBox.Show("설정 파일을 읽지 못했습니다.", "민트 키우기");
                    return;
                }
                window.SetAlwaysOnTop(settings.AlwaysOnTop);
                window.SetRestRemindersEnabled(settings.RestRemindersEnabled);
                if (!IsPackaged()) SetAutoStart(settings.AutoStart);
                topmostItem.Checked = settings.AlwaysOnTop;
                autoStartItem.Checked = settings.AutoStart;
                restRemindersItem.Checked = settings.RestRemindersEnabled;
                SaveSettings();
            }
        }

        private void ShowWindow()
        {
            Log("ShowWindow called");
            if (quitting) return;
            if (window == null)
            {
                showPending = true;
                return;
            }
            showPending = false;
            try
            {
                if (!window.IsVisible) window.Show();
                window.ResetPosition();
                window.Topmost = true;
                window.Activate();
                if (visibilityItem != null) visibilityItem.Text = "민트 숨기기 (Hide Mint)";
            }
            catch (Exception ex) { Log("ShowWindow exception: " + ex); }
        }

        private static bool IsPackaged()
        {
            try
            {
                uint length = 0;
                return GetCurrentPackagePath(ref length, null) != 157;
            }
            catch (Exception)
            {
                return true;
            }
        }

        private static void SetAutoStart(bool enabled)
        {
            if (IsPackaged()) return;
            try
            {
                using (var key = Registry.CurrentUser.CreateSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\Run"))
                {
                    if (key == null) return;
                    if (enabled)
                    {
                        var path = Process.GetCurrentProcess().MainModule.FileName;
                        key.SetValue(RunValueName, "\"" + path + "\"", RegistryValueKind.String);
                    }
                    else key.DeleteValue(RunValueName, false);
                }
            }
            catch (Exception)
            {
                // Registry policy must not prevent the pet from running.
            }
        }

        private void SaveSettings()
        {
            try { settings.Save(); }
            catch (Exception) { }
        }

        private void Quit()
        {
            Quit(true);
        }

        private void Quit(bool requestShutdown)
        {
            Log("Quit called, requestShutdown=" + requestShutdown + ", stack:\r\n" + Environment.StackTrace);
            if (quitting) return;
            quitting = true;
            SaveSettings();
            try
            {
                if (window != null)
                {
                    window.DisposeResources();
                    window.Close();
                }
            }
            catch (Exception) { }
            DisposeTray();
            DisposeShowEvent();
            if (instanceMutex != null)
            {
                try { instanceMutex.ReleaseMutex(); }
                catch (Exception) { }
                instanceMutex.Dispose();
                instanceMutex = null;
            }
            if (requestShutdown) Shutdown();
        }

        private void DisposeTray()
        {
            if (tray == null) return;
            var icon = tray.Icon;
            tray.Icon = null;
            try { tray.Visible = false; }
            catch (Exception) { }
            try { tray.Dispose(); }
            catch (Exception) { }
            try { if (icon != null) icon.Dispose(); }
            catch (Exception) { }
            tray = null;
        }

        private void DisposeShowEvent()
        {
            if (showRegistration != null)
            {
                try { showRegistration.Unregister(null); }
                catch (Exception) { }
                showRegistration = null;
            }
            if (showEvent != null)
            {
                try { showEvent.Dispose(); }
                catch (Exception) { }
                showEvent = null;
            }
        }

        protected override void OnExit(ExitEventArgs e)
        {
            Log("OnExit called, exitCode=" + e.ApplicationExitCode);
            if (!quitting) Quit(false);
            base.OnExit(e);
        }

        [System.Runtime.InteropServices.DllImport("kernel32.dll", CharSet = System.Runtime.InteropServices.CharSet.Unicode)]
        private static extern int GetCurrentPackagePath(ref uint packagePathLength, StringBuilder packagePath);
    }
}
