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

                playToggleItem = new Forms.ToolStripMenuItem(window != null && window.IsPlayMode ? "🛑 나대지마 (멈추기)" : "🎉 놀자! (움직이기)");
                playToggleItem.Font = new System.Drawing.Font(menu.Font, System.Drawing.FontStyle.Bold);
                playToggleItem.Click += delegate
                {
                    if (window != null) window.TogglePlayMode();
                };
                menu.Items.Add(playToggleItem);

                var petItem = new Forms.ToolStripMenuItem("🖐️ 민트 쓰다듬기 (Pet Mint)", null, delegate { window?.ReactToPetting(); });
                menu.Items.Add(petItem);

                var hayQuickItem = new Forms.ToolStripMenuItem("🌾 맛있는 건초 주기 (+2)", null, delegate { window?.SetItem(BunnyItem.Hay); });
                menu.Items.Add(hayQuickItem);

                menu.Items.Add(new Forms.ToolStripSeparator());

                var poseMenu = new Forms.ToolStripMenuItem("📸 민트 특별 포즈 (Special Poses)");

                var binkyItem = new Forms.ToolStripMenuItem("🤸 기분 최고 점프! 빙키", null, delegate { window?.TriggerPose(BunnyState.Binky); });
                poseMenu.DropDownItems.Add(binkyItem);

                var kissItem = new Forms.ToolStripMenuItem("💋 뽀뽀해주는 래빗키스", null, delegate { window?.TriggerPose(BunnyState.Kiss); });
                poseMenu.DropDownItems.Add(kissItem);

                var washItem = new Forms.ToolStripMenuItem("🧼 손으로 쓱싹 세수하기", null, delegate { window?.TriggerPose(BunnyState.Wash); });
                poseMenu.DropDownItems.Add(washItem);

                var flopItem = new Forms.ToolStripMenuItem("🛌 안심하고 벌러덩 눕기", null, delegate { window?.TriggerPose(BunnyState.Flop); });
                poseMenu.DropDownItems.Add(flopItem);

                var begItem = new Forms.ToolStripMenuItem("🌾 간식 내놔!", null, delegate { window?.TriggerPose(BunnyState.Beg); });
                poseMenu.DropDownItems.Add(begItem);

                var angryItem = new Forms.ToolStripMenuItem("💢 화났어!", null, delegate { window?.TriggerPose(BunnyState.Angry); });
                poseMenu.DropDownItems.Add(angryItem);

                var frontItem = new Forms.ToolStripMenuItem("🐰 똘망똘망 정면 포즈", null, delegate { window?.TriggerPose(BunnyState.Front); });
                poseMenu.DropDownItems.Add(frontItem);

                var confusedItem = new Forms.ToolStripMenuItem("👀 어리둥절 실사 영상", null, delegate { window?.TriggerPose(BunnyState.Confused); });
                poseMenu.DropDownItems.Add(confusedItem);

                var introItem = new Forms.ToolStripMenuItem("✨ 천사 민트 등장", null, delegate { window?.PlayIntroGreeting(); });
                poseMenu.DropDownItems.Add(introItem);

                menu.Items.Add(poseMenu);

                var itemsMenu = new Forms.ToolStripMenuItem("🎁 민트에게 선물하기");
                var hayItem = new Forms.ToolStripMenuItem("🌾 맛있는 건초 (Hay)", null, delegate { window?.SetItem(BunnyItem.Hay); });
                var chairItem = new Forms.ToolStripMenuItem("🪑 작은 의자 (Chair)", null, delegate { window?.SetItem(BunnyItem.Chair); });
                var dollItem = new Forms.ToolStripMenuItem("🧸 토끼 인형 (Plush Doll)", null, delegate { window?.SetItem(BunnyItem.Doll); });
                var bagItem = new Forms.ToolStripMenuItem("🎒 소풍 가방 (Backpack)", null, delegate { window?.SetItem(BunnyItem.Bag); });
                var houseItem = new Forms.ToolStripMenuItem("🏠 아늑한 집 (House)", null, delegate { window?.SetItem(BunnyItem.House); });
                var clearItem = new Forms.ToolStripMenuItem("❌ 아이템 치우기 (Remove Item)", null, delegate { window?.SetItem(BunnyItem.None); });

                itemsMenu.DropDownItems.Add(hayItem);
                itemsMenu.DropDownItems.Add(chairItem);
                itemsMenu.DropDownItems.Add(dollItem);
                itemsMenu.DropDownItems.Add(bagItem);
                itemsMenu.DropDownItems.Add(houseItem);
                itemsMenu.DropDownItems.Add(new Forms.ToolStripSeparator());
                itemsMenu.DropDownItems.Add(clearItem);
                menu.Items.Add(itemsMenu);

                menu.Opening += delegate
                {
                    int aff = window != null ? window.Affinity : settings.Affinity;
                    UpdatePoseItemText(binkyItem, BunnyState.Binky, aff, "🤸 기분 최고 점프! 빙키", "빙키 점프");
                    UpdatePoseItemText(kissItem, BunnyState.Kiss, aff, "💋 뽀뽀해주는 래빗키스", "래빗키스");
                    UpdatePoseItemText(washItem, BunnyState.Wash, aff, "🧼 손으로 쓱싹 세수하기", "세수하기");
                    UpdatePoseItemText(flopItem, BunnyState.Flop, aff, "🛌 안심하고 벌러덩 눕기", "벌러덩 눕기");
                    UpdatePoseItemText(begItem, BunnyState.Beg, aff, "🌾 간식 내놔!", "간식 내놔");
                    UpdatePoseItemText(angryItem, BunnyState.Angry, aff, "💢 화났어!", "화났어");
                    UpdatePoseItemText(frontItem, BunnyState.Front, aff, "🐰 똘망똘망 정면 포즈", "정면 포즈");
                    UpdatePoseItemText(confusedItem, BunnyState.Confused, aff, "👀 어리둥절 실사 영상", "어리둥절 영상");

                    UpdateGiftItemText(hayItem, BunnyItem.Hay, aff, "🌾 맛있는 건초 (Hay)", "건초");
                    UpdateGiftItemText(chairItem, BunnyItem.Chair, aff, "🪑 작은 의자 (Chair)", "작은 의자");
                    UpdateGiftItemText(dollItem, BunnyItem.Doll, aff, "🧸 토끼 인형 (Plush Doll)", "토끼 인형");
                    UpdateGiftItemText(bagItem, BunnyItem.Bag, aff, "🎒 소풍 가방 (Backpack)", "소풍 가방");
                    UpdateGiftItemText(houseItem, BunnyItem.House, aff, "🏠 아늑한 집 (House)", "아늑한 집");
                };

                var topmostItem = new Forms.ToolStripMenuItem("📌 항상 위에 표시 (Always on Top)")
                {
                    CheckOnClick = true,
                    Checked = settings.AlwaysOnTop
                };
                topmostItem.Click += delegate
                {
                    settings.AlwaysOnTop = topmostItem.Checked;
                    window?.SetAlwaysOnTop(settings.AlwaysOnTop);
                    SaveSettings();
                };
                menu.Opening += delegate { topmostItem.Checked = settings.AlwaysOnTop; };
                menu.Items.Add(topmostItem);

                var dashboardItem = new Forms.ToolStripMenuItem("🌸 민트 대시보드 및 설정...");
                dashboardItem.Font = new System.Drawing.Font(menu.Font, System.Drawing.FontStyle.Bold);
                dashboardItem.Click += delegate
                {
                    window?.OpenDashboard();
                };
                menu.Items.Add(dashboardItem);

                menu.Items.Add(new Forms.ToolStripSeparator());

                var resetItem = new Forms.ToolStripMenuItem("↩ 민트 자리로 부르기 (Reset position)");
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



        private static void UpdatePoseItemText(Forms.ToolStripMenuItem item, BunnyState state, int affinity, string fullTitle, string shortTitle)
        {
            if (BunnyProgression.IsUnlocked(state, affinity))
            {
                item.Text = fullTitle;
            }
            else
            {
                item.Text = $"🔒 {shortTitle} (호감도 {BunnyProgression.GetRequiredAffinity(state)}점 필요)";
            }
        }

        private static void UpdateGiftItemText(Forms.ToolStripMenuItem item, BunnyItem itemType, int affinity, string fullTitle, string shortTitle)
        {
            if (BunnyProgression.IsUnlocked(itemType, affinity))
            {
                item.Text = fullTitle;
            }
            else
            {
                item.Text = $"🔒 {shortTitle} (호감도 {BunnyProgression.GetRequiredAffinity(itemType)}점 필요)";
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
