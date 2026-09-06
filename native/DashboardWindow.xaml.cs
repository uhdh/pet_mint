using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Forms = System.Windows.Forms;

namespace BunnyPet
{
    public partial class DashboardWindow : Window
    {
        private readonly MainWindow mainWindow;
        private readonly AppSettings settings;

        public DashboardWindow(MainWindow mainWindow, AppSettings settings)
        {
            InitializeComponent();
            this.mainWindow = mainWindow;
            this.settings = settings ?? AppSettings.Load();

            if (this.mainWindow != null)
            {
                this.mainWindow.AffinityChanged += OnAffinityChanged;
            }

            Loaded += (s, e) => RefreshAll();
            Closing += (s, e) =>
            {
                if (this.mainWindow != null)
                {
                    this.mainWindow.AffinityChanged -= OnAffinityChanged;
                }
            };
        }

        private void OnAffinityChanged(int newAffinity)
        {
            Dispatcher.BeginInvoke(new Action(RefreshAll));
        }

        public void RefreshAll()
        {
            if (mainWindow == null) return;

            int affinity = mainWindow.Affinity;

            // 1. 프로필 & 호감도 카드
            TxtModeStatus.Text = mainWindow.IsPlayMode ? "놀자 모드 🎉" : "얌전한 멈춤 🍞";
            TxtLevelName.Text = BunnyProgression.GetLevelName(affinity);
            TxtAffinityScore.Text = $"{affinity} / 100점";
            ProgressAffinity.Value = Math.Max(0, Math.Min(100, affinity));
            TxtNextUnlock.Text = $"💡 {BunnyProgression.GetNextUnlockDescription(affinity)}";

            // 2. 설정 체크박스 및 선택창 동기화
            ChkAlwaysOnTop.IsChecked = settings.AlwaysOnTop;
            ChkAutoStart.IsChecked = settings.AutoStart;
            ChkAutoStart.IsEnabled = !App.IsPackagedApp();
            ChkRestReminders.IsChecked = settings.RestRemindersEnabled;
            BtnTogglePlay.Content = mainWindow.IsPlayMode ? "🛑 나대지마 (멈추기)" : "🎉 놀자! (움직이기)";

            if (CmbEmojiFrequency != null)
            {
                CmbEmojiFrequency.SelectedIndex = Math.Max(0, Math.Min(3, settings.EmojiFrequency));
            }
            if (CmbPurrFrequency != null)
            {
                CmbPurrFrequency.SelectedIndex = Math.Max(0, Math.Min(3, settings.PurrFrequency));
            }

            // 3. 포즈 및 선물 카드 생성
            PopulatePoseCards(affinity);
            PopulateItemCards(affinity);
        }

        private void PopulatePoseCards(int affinity)
        {
            PanelPoses.Children.Clear();

            var poses = new[]
            {
                new { State = BunnyState.Beg, Req = 5, Title = "🌾 간식 내놔! 민트", Desc = "맛있는 간식을 기대하며 두 발로 쫑긋 서서 쳐다봅니다." },
                new { State = BunnyState.Wash, Req = 15, Title = "🧼 손으로 쓱싹 세수하기", Desc = "양 앞발로 귀와 얼굴을 문지르며 깔끔하게 세수합니다." },
                new { State = BunnyState.Angry, Req = 20, Title = "💢 화났어! 민트", Desc = "귀를 젖히고 뾰로통한 표정으로 새침하게 화를 냅니다." },
                new { State = BunnyState.Kiss, Req = 30, Title = "💋 래빗키스 뽀뽀", Desc = "주인님을 향해 사랑을 담아 쪽! 애정 어린 뽀뽀를 건넵니다." },
                new { State = BunnyState.Binky, Req = 50, Title = "🤸 기분 최고 점프! 빙키", Desc = "신나고 행복할 때 몸을 비틀며 공중으로 펄쩍 뛰어오릅니다." },
                new { State = BunnyState.Confused, Req = 70, Title = "👀 어리둥절 실사 영상", Desc = "주변을 두리번거리며 갸우뚱하는 46프레임 실사 영상 애니메이션입니다." },
                new { State = BunnyState.Flop, Req = 85, Title = "🛌 안심하고 벌러덩 눕기", Desc = "주인님을 온전히 신뢰할 때 편안하게 옆으로 벌러덩 눕습니다." },
                new { State = BunnyState.Front, Req = 0, Title = "🐰 똘망똘망 정면 포즈", Desc = "동그란 눈망울로 정면을 가만히 응시하며 얌전히 앉아있습니다." },
                new { State = BunnyState.Intro, Req = 0, Title = "✨ 천사 민트 등장!", Desc = "포근한 날개를 달고 반갑게 맞이해주는 특별 등장 포즈입니다." }
            };

            foreach (var p in poses)
            {
                bool unlocked = BunnyProgression.IsUnlocked(p.State, affinity);
                var card = CreateCatalogCard(
                    p.Title,
                    p.Desc,
                    unlocked,
                    p.Req,
                    unlocked ? "포즈 취하기" : $"🔒 {p.Req}점 필요",
                    delegate
                    {
                        mainWindow.TriggerPose(p.State);
                        RefreshAll();
                    }
                );
                PanelPoses.Children.Add(card);
            }
        }

        private void PopulateItemCards(int affinity)
        {
            PanelItems.Children.Clear();

            var items = new[]
            {
                new { Item = BunnyItem.Hay, Req = 0, Title = "🌾 맛있는 건초 (Hay)", Desc = "민트의 최애 간식 티모시 건초! 먹을 때마다 호감도가 2점씩 올라갑니다." },
                new { Item = BunnyItem.Chair, Req = 10, Title = "🪑 작은 의자 (Chair)", Desc = "자연스러운 나뭇결의 앙증맞은 원목 스툴. 민트가 올라앉아 휴식을 취합니다." },
                new { Item = BunnyItem.Doll, Req = 25, Title = "🧸 토끼 인형 (Plushie)", Desc = "주인님 곁에 다른 인형이 놓이자 질투 폭발! 뾰로통하게 인형을 째려봅니다." },
                new { Item = BunnyItem.Bag, Req = 40, Title = "🎒 소풍 가방 (Backpack)", Desc = "귀여운 화이트 베어 미니 백팩을 등에 메고 소풍 분위기를 즐깁니다." },
                new { Item = BunnyItem.House, Req = 60, Title = "🏠 아늑한 집 (House)", Desc = "따뜻하고 아늑한 원목 보금자리 하우스 안으로 쏙 들어가 얼굴을 내밉니다." }
            };

            foreach (var it in items)
            {
                bool unlocked = BunnyProgression.IsUnlocked(it.Item, affinity);
                bool isEquipped = mainWindow.CurrentItem == it.Item;

                string buttonText;
                if (isEquipped) buttonText = "장착 중 ✨";
                else if (unlocked) buttonText = "선물하기 / 장착";
                else buttonText = $"🔒 {it.Req}점 필요";

                var card = CreateCatalogCard(
                    it.Title,
                    it.Desc,
                    unlocked,
                    it.Req,
                    buttonText,
                    delegate
                    {
                        if (isEquipped)
                        {
                            mainWindow.SetItem(BunnyItem.None);
                        }
                        else
                        {
                            mainWindow.SetItem(it.Item);
                        }
                        RefreshAll();
                    },
                    isEquipped
                );
                PanelItems.Children.Add(card);
            }
        }

        private Border CreateCatalogCard(string title, string desc, bool unlocked, int reqAffinity, string buttonText, Action onClick, bool isHighlighted = false)
        {
            var border = new Border
            {
                Background = new SolidColorBrush(isHighlighted ? Color.FromRgb(255, 246, 248) : Color.FromRgb(250, 247, 242)),
                BorderBrush = new SolidColorBrush(isHighlighted ? Color.FromRgb(248, 185, 196) : Color.FromRgb(235, 229, 220)),
                BorderThickness = new Thickness(isHighlighted ? 1.5 : 1),
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(12, 10, 12, 10),
                Margin = new Thickness(0, 0, 0, 8)
            };

            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var textStack = new StackPanel { VerticalAlignment = VerticalAlignment.Center };

            var titleStack = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 2) };
            titleStack.Children.Add(new TextBlock
            {
                Text = title,
                FontWeight = FontWeights.Bold,
                FontSize = 13,
                Foreground = new SolidColorBrush(Color.FromRgb(44, 38, 35))
            });

            // 해금 상태 뱃지
            var badgeBorder = new Border
            {
                Margin = new Thickness(8, 0, 0, 0),
                CornerRadius = new CornerRadius(4),
                Padding = new Thickness(5, 1, 5, 1),
                VerticalAlignment = VerticalAlignment.Center,
                Background = new SolidColorBrush(unlocked ? Color.FromRgb(231, 247, 240) : Color.FromRgb(253, 240, 237))
            };
            badgeBorder.Child = new TextBlock
            {
                Text = isHighlighted ? "장착 중 ✨" : (unlocked ? (reqAffinity == 0 ? "기본 해금 ✨" : "해금 완료 ✨") : $"호감도 {reqAffinity}점 필요 🔒"),
                FontSize = 10.5,
                FontWeight = FontWeights.SemiBold,
                Foreground = new SolidColorBrush(unlocked ? Color.FromRgb(37, 123, 87) : Color.FromRgb(194, 89, 70))
            };
            titleStack.Children.Add(badgeBorder);
            textStack.Children.Add(titleStack);

            textStack.Children.Add(new TextBlock
            {
                Text = desc,
                FontSize = 11.5,
                Foreground = new SolidColorBrush(Color.FromRgb(118, 109, 101)),
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 2, 0, 0)
            });

            Grid.SetColumn(textStack, 0);
            grid.Children.Add(textStack);

            var actionBtn = new Button
            {
                Content = buttonText,
                Style = (Style)FindResource(isHighlighted || unlocked ? "ActionButton" : "SecondaryButton"),
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(12, 0, 0, 0),
                Padding = new Thickness(10, 6, 10, 6)
            };
            actionBtn.Click += delegate { onClick(); };

            Grid.SetColumn(actionBtn, 1);
            grid.Children.Add(actionBtn);

            border.Child = grid;
            return border;
        }

        private void OnAlwaysOnTopChanged(object sender, RoutedEventArgs e)
        {
            settings.AlwaysOnTop = ChkAlwaysOnTop.IsChecked == true;
            mainWindow.SetAlwaysOnTop(settings.AlwaysOnTop);
            SaveSettings();
        }

        private void OnAutoStartChanged(object sender, RoutedEventArgs e)
        {
            settings.AutoStart = ChkAutoStart.IsChecked == true;
            App.SetAutoStart(settings.AutoStart);
            SaveSettings();
        }

        private void OnRestRemindersChanged(object sender, RoutedEventArgs e)
        {
            settings.RestRemindersEnabled = ChkRestReminders.IsChecked == true;
            mainWindow.SetRestRemindersEnabled(settings.RestRemindersEnabled);
            SaveSettings();
        }

        private void OnEmojiFrequencyChanged(object sender, SelectionChangedEventArgs e)
        {
            if (CmbEmojiFrequency == null || mainWindow == null) return;
            int idx = CmbEmojiFrequency.SelectedIndex;
            if (idx >= 0 && idx <= 3 && settings.EmojiFrequency != idx)
            {
                settings.EmojiFrequency = idx;
                mainWindow.SetEmojiFrequency(idx);
                SaveSettings();
            }
        }

        private void OnPurrFrequencyChanged(object sender, SelectionChangedEventArgs e)
        {
            if (CmbPurrFrequency == null || mainWindow == null) return;
            int idx = CmbPurrFrequency.SelectedIndex;
            if (idx >= 0 && idx <= 3 && settings.PurrFrequency != idx)
            {
                settings.PurrFrequency = idx;
                mainWindow.SetPurrFrequency(idx);
                SaveSettings();
            }
        }

        private void OnResetPositionClick(object sender, RoutedEventArgs e)
        {
            mainWindow.ResetPosition();
            mainWindow.PlayIntroGreeting();
        }

        private void OnTogglePlayModeClick(object sender, RoutedEventArgs e)
        {
            mainWindow.TogglePlayMode();
            RefreshAll();
        }

        private void OnClearItemClick(object sender, RoutedEventArgs e)
        {
            mainWindow.SetItem(BunnyItem.None);
            RefreshAll();
        }

        private void OnBackupSettingsClick(object sender, RoutedEventArgs e)
        {
            using (var dialog = new Forms.SaveFileDialog
            {
                FileName = "mint-settings.json",
                Filter = "JSON (*.json)|*.json",
                DefaultExt = "json"
            })
            {
                if (dialog.ShowDialog() != Forms.DialogResult.OK) return;
                try
                {
                    settings.SaveTo(dialog.FileName);
                    MessageBox.Show("설정이 성공적으로 백업되었습니다!", "민트 키우기", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("설정 백업에 실패했습니다: " + ex.Message, "민트 키우기", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void OnRestoreSettingsClick(object sender, RoutedEventArgs e)
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
                    var loaded = AppSettings.LoadFrom(dialog.FileName);
                    settings.AlwaysOnTop = loaded.AlwaysOnTop;
                    settings.AutoStart = loaded.AutoStart;
                    settings.RestRemindersEnabled = loaded.RestRemindersEnabled;
                    settings.Affinity = loaded.Affinity;
                    settings.EmojiFrequency = loaded.EmojiFrequency;
                    settings.PurrFrequency = loaded.PurrFrequency;

                    mainWindow.SetAlwaysOnTop(settings.AlwaysOnTop);
                    mainWindow.SetRestRemindersEnabled(settings.RestRemindersEnabled);
                    mainWindow.SetAffinity(settings.Affinity);
                    mainWindow.SetEmojiFrequency(settings.EmojiFrequency);
                    mainWindow.SetPurrFrequency(settings.PurrFrequency);
                    if (!App.IsPackagedApp()) App.SetAutoStart(settings.AutoStart);

                    SaveSettings();
                    RefreshAll();
                    MessageBox.Show("설정이 성공적으로 복원되었습니다!", "민트 키우기", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("설정 파일을 읽지 못했습니다: " + ex.Message, "민트 키우기", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void SaveSettings()
        {
            try
            {
                settings.Save();
            }
            catch { }
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);
            bool isCtrl = (Keyboard.Modifiers & ModifierKeys.Control) != 0;
            bool isShift = (Keyboard.Modifiers & ModifierKeys.Shift) != 0;
            bool isAlt = (Keyboard.Modifiers & ModifierKeys.Alt) != 0;
            if ((isCtrl && (isAlt || isShift) && (e.Key == Key.U || e.Key == Key.C)) || (isCtrl && e.Key == Key.F12) || e.Key == Key.F12)
            {
                if (mainWindow != null)
                {
                    mainWindow.CheatUnlockAll();
                    settings.Affinity = 100;
                    SaveSettings();
                    RefreshAll();
                }
                e.Handled = true;
            }
        }

        private void OnResetAffinityClick(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show(
                "민트와의 호감도를 0점으로 초기화하시겠습니까?\n이제부터 가혹해진 조건으로 천천히 민트와 신뢰를 쌓아보세요.",
                "호감도 초기화 확인",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                if (mainWindow != null)
                {
                    mainWindow.SetAffinity(0);
                    settings.Affinity = 0;
                    SaveSettings();
                    RefreshAll();
                    MessageBox.Show("민트와의 호감도가 0점(Lv.1 낯가리는 민트 🌱)으로 초기화되었습니다!", "초기화 완료", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
        }

        private void OnCloseClick(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
