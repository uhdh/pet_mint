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
        public AppSettings CurrentSettings => settings;
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

            EnsureShowEvent();
            bool createdNew = false;
            try
            {
                instanceMutex = new Mutex(true, MutexName, out createdNew);
            }
            catch (AbandonedMutexException ame)
            {
                Log("Mutex AbandonedMutexException: " + ame.Message);
            }
            catch (Exception ex)
            {
                Log("Mutex Exception: " + ex.Message);
            }
            if (instanceMutex != null && !createdNew)
            {
                SignalExistingInstance();
                Shutdown();
                return;
            }

            Log("Loading settings");
            settings = AppSettings.Load();
            RegisterShowEvent();
            window = new MainWindow();
            MainWindow = window;
            window.SetAlwaysOnTop(settings.AlwaysOnTop);
            window.SetRestRemindersEnabled(settings.RestRemindersEnabled);
            window.SetAffinity(settings.Affinity);
            window.SetEmojiFrequency(settings.EmojiFrequency);
            window.SetPurrFrequency(settings.PurrFrequency);
            window.AffinityChanged += (newAffinity) =>
            {
                settings.Affinity = newAffinity;
                SaveSettings();
            };
            Log("Showing window");
            window.Show();
            if (settings.AlwaysOnTop)
            {
                window.Activate();
            }
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

                var dashboardItem = new Forms.ToolStripMenuItem("🌸 민트 대시보드 및 설정...");
                dashboardItem.Font = new System.Drawing.Font(menu.Font, System.Drawing.FontStyle.Bold);
                dashboardItem.Click += delegate
                {
                    window?.OpenDashboard();
                };
                menu.Items.Add(dashboardItem);

                menu.Items.Add(new Forms.ToolStripSeparator());

                playToggleItem = new Forms.ToolStripMenuItem(window != null && window.IsPlayMode ? "🛑 나대지마 (멈추기)" : "🎉 놀자! (움직이기)");
                playToggleItem.Click += delegate
                {
                    if (window != null) window.TogglePlayMode();
                };
                menu.Items.Add(playToggleItem);

                var resetItem = new Forms.ToolStripMenuItem("↩ 민트 자리로 부르기 (오른쪽 아래)");
                resetItem.Click += delegate
                {
                    if (window != null)
                    {
                        window.ResetPosition();
                        window.PlayIntroGreeting();
                    }
                };
                menu.Items.Add(resetItem);

                visibilityItem = new Forms.ToolStripMenuItem("민트 숨기기 (Hide Mint)");
                visibilityItem.Click += delegate { ToggleWindow(); };
                menu.Items.Add(visibilityItem);

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
                    window.SetAlwaysOnTop(settings.AlwaysOnTop);
                    if (settings.AlwaysOnTop)
                    {
                        window.Activate();
                    }
                }
                if (visibilityItem != null) visibilityItem.Text = window.IsVisible ? "민트 숨기기 (Hide Mint)" : "민트 보이기 (Show Mint)";
            }
            catch (Exception) { }
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
                window.SetAlwaysOnTop(settings.AlwaysOnTop);
                if (settings.AlwaysOnTop)
                {
                    window.Activate();
                }
                if (visibilityItem != null) visibilityItem.Text = "민트 숨기기 (Hide Mint)";
            }
            catch (Exception ex) { Log("ShowWindow exception: " + ex); }
        }

        public static bool IsPackagedApp()
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

        public static void SetAutoStart(bool enabled)
        {
            if (IsPackagedApp()) return;
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
