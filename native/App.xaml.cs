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
        private Forms.ToolStripMenuItem pauseItem;
        private AppSettings settings;
        private bool paused;
        private bool quitting;

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            bool created;
            try
            {
                instanceMutex = new Mutex(true, MutexName, out created);
            }
            catch (Exception)
            {
                Shutdown();
                return;
            }

            if (!created)
            {
                SignalExistingInstance();
                Shutdown();
                return;
            }

            settings = AppSettings.Load();
            RegisterShowEvent();
            window = new MainWindow();
            MainWindow = window;
            window.SetAlwaysOnTop(settings.AlwaysOnTop);
            window.Show();
            CreateTray();
        }

        private void RegisterShowEvent()
        {
            try
            {
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
                using (var signal = EventWaitHandle.OpenExisting(ShowEventName)) signal.Set();
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
                pauseItem = new Forms.ToolStripMenuItem("Pause");
                pauseItem.Click += delegate
                {
                    paused = !paused;
                    window.SetPaused(paused);
                    pauseItem.Text = paused ? "Resume" : "Pause";
                };
                menu.Items.Add(pauseItem);

                var topmostItem = new Forms.ToolStripMenuItem("Always on top")
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

                var autoStartItem = new Forms.ToolStripMenuItem("Start with Windows")
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

                var resetItem = new Forms.ToolStripMenuItem("Reset position");
                resetItem.Click += delegate { window.ResetPosition(); };
                menu.Items.Add(resetItem);

                menu.Items.Add(new Forms.ToolStripSeparator());
                var quitItem = new Forms.ToolStripMenuItem("Quit");
                quitItem.Click += delegate { Quit(); };
                menu.Items.Add(quitItem);

                tray = new Forms.NotifyIcon
                {
                    Text = "My Bunny Desktop Pet",
                    Icon = LoadTrayIcon(),
                    ContextMenuStrip = menu,
                    Visible = true
                };
                tray.MouseClick += delegate(object sender, Forms.MouseEventArgs args)
                {
                    if (args.Button == Forms.MouseButtons.Left) ToggleWindow();
                };
            }
            catch (Exception)
            {
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
            var resource = GetResourceStream(new Uri("pack://application:,,,/Assets/icon.ico", UriKind.Absolute));
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
            }
            catch (Exception) { }
        }

        private void ShowWindow()
        {
            if (quitting || window == null) return;
            try
            {
                window.Show();
                window.Activate();
            }
            catch (Exception) { }
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
            if (!quitting) Quit(false);
            base.OnExit(e);
        }

        [System.Runtime.InteropServices.DllImport("kernel32.dll", CharSet = System.Runtime.InteropServices.CharSet.Unicode)]
        private static extern int GetCurrentPackagePath(ref uint packagePathLength, StringBuilder packagePath);
    }
}
