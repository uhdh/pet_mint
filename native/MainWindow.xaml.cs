using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Forms = System.Windows.Forms;

namespace BunnyPet
{
    public partial class MainWindow : Window
    {
        private const int WindowWidth = 107;
        private const int WindowHeight = 100;
        private const int WalkStep = 2;
        private const double DragThreshold = 8;

        private readonly BunnyStateMachine machine;
        private readonly DispatcherTimer behaviorTimer;
        private readonly DispatcherTimer walkingTimer;
        private readonly DispatcherTimer clockTimer;
        private readonly DispatcherTimer restTimer;
        private readonly DispatcherTimer awarenessTimer;
        private readonly DispatcherTimer messageTimer;
        private readonly GlobalInputWatcher inputWatcher;
        private readonly Random random = new Random();
        private readonly TranslateTransform translate = new TranslateTransform();
        private readonly RotateTransform rotate = new RotateTransform();
        private readonly ScaleTransform scale = new ScaleTransform(1, 1);
        private readonly string[] phrases =
        {
            "💕", "🥰", "💖", "💗", "💓", "💞", "😻", "🌸", "✨", "❤️", "🧡", "💛", "🤍", "😍", "😚", "💘"
        };
        private readonly string[] cursorPhrases =
        {
            "👀", "❓", "👋", "🐰", "😳", "✨", "⭐", "💡", "🔍", "🧐", "🤍", "🐾", "😮", "💫", "👽", "👾"
        };
        private readonly string[] dollPhrases =
        {
            "💢", "😡", "😤", "😒", "🥺", "😱", "🙄", "💔", "⚡", "👿", "😣", "😾", "😠", "💥"
        };
        private readonly string[] hayPhrases =
        {
            "🌾", "😋", "🥕", "🤤", "🥣", "🌿", "🍀", "🍽️", "🥗", "👅", "🍴", "🌱"
        };
        private readonly string[] bagPhrases =
        {
            "🎒", "🎈", "🎉", "🗺️", "🧭", "🥪", "👟", "🏕️", "🏃", "✨", "🎊", "🏖️"
        };
        private readonly string[] housePhrases =
        {
            "🏠", "💤", "🛋️", "🌙", "⭐", "🕯️", "🏡", "🛌", "😴", "☁️", "🛏️", "🪵"
        };
        private readonly string[] chairPhrases =
        {
            "🪑", "😌", "🛋️", "☕", "🍃", "🌸", "💆", "✨", "🧋", "🍵", "🌼", "🫖"
        };
        private readonly string[] restingPhrases =
        {
            "🍞", "🍞", "💤", "💤", "😴", "☁️", "🥐", "🥱", "🌾", "🌙", "✨"
        };
        private readonly string[] purrPhrases =
        {
            "🥰", "💖", "💕", "🌸", "😻", "💓", "💗"
        };
        private readonly string[] playPhrases =
        {
            "🎉", "🏃", "✨", "🐾", "🎈", "👀", "🥳", "👽"
        };
        private readonly string[] scoldPhrases =
        {
            "🍞", "🥺", "😳", "💤", "🤐", "🤫"
        };
        private readonly string[] begPhrases =
        {
            "🌾", "😋", "🥕", "🤤", "🍽️", "🥺", "👀"
        };
        private readonly string[] angryPhrases =
        {
            "💢", "😡", "😤", "⚡", "👿", "😾", "😠"
        };
        private readonly string[] frontPhrases =
        {
            "🐰", "👀", "✨", "🤍", "🐾", "⭐", "👽", "🛸"
        };
        private readonly string[] introPhrases =
        {
            "✨", "🤍", "🕊️", "🐰", "⭐", "🎉", "💖", "🥰"
        };

        private readonly Dictionary<string, BitmapImage> emojiBitmaps = new Dictionary<string, BitmapImage>();
        private BunnyItem currentItem = BunnyItem.None;
        private Point lastPointer;
        private double dragDistance;
        private bool dragging;
        private bool dragMoved;
        private double strokeDistance;
        private DateTime lastStrokeTime = DateTime.MinValue;
        private DateTime lastPetReactionUtc = DateTime.MinValue;
        private bool allowClose;
        private volatile bool resourcesDisposed;
        private bool climbing;
        private int climbDirection = -1;
        private bool restRemindersEnabled = true;
        private int lastAnnouncedHour = -1;
        private DateTime lastCursorReactionUtc = DateTime.MinValue;
        private DateTime lastKeyReactionUtc = DateTime.MinValue;

        private readonly List<BitmapImage> confusedFrames = new List<BitmapImage>();
        private DispatcherTimer confusedAnimTimer;
        private int confusedFrameIndex = 0;
        private bool isPlayingConfusedAnim = false;
        private Action confusedCompletedCallback = null;

        public bool ExitOnClose { get; set; }

        public MainWindow()
        {
            InitializeComponent();
            machine = new BunnyStateMachine(DateTime.UtcNow);
            BunnyImage.RenderTransform = new TransformGroup
            {
                Children = new TransformCollection { translate, rotate, scale }
            };

            behaviorTimer = new DispatcherTimer(DispatcherPriority.Background)
            {
                Interval = TimeSpan.FromMilliseconds(RandomBetween(3500, 7600))
            };
            behaviorTimer.Tick += OnBehaviorTick;
            walkingTimer = new DispatcherTimer(DispatcherPriority.Background)
            {
                Interval = TimeSpan.FromMilliseconds(80)
            };
            walkingTimer.Tick += OnWalkingTick;
            clockTimer = new DispatcherTimer(DispatcherPriority.Background)
            {
                Interval = TimeSpan.FromSeconds(30)
            };
            clockTimer.Tick += OnClockTick;
            restTimer = new DispatcherTimer(DispatcherPriority.Background)
            {
                Interval = TimeSpan.FromMinutes(20)
            };
            restTimer.Tick += OnRestTick;
            awarenessTimer = new DispatcherTimer(DispatcherPriority.Background)
            {
                Interval = TimeSpan.FromMilliseconds(150)
            };
            awarenessTimer.Tick += OnAwarenessTick;
            messageTimer = new DispatcherTimer(DispatcherPriority.Normal);
            messageTimer.Tick += OnMessageTimerTick;
            inputWatcher = new GlobalInputWatcher();
            inputWatcher.KeyPressed += OnGlobalKeyPressed;
            InitEmojiBitmaps();
            InitConfusedFrames();
            Loaded += OnLoaded;
            Closing += OnClosing;
        }

        public void DisposeResources()
        {
            resourcesDisposed = true;
            allowClose = true;
            CancelPointer();
            behaviorTimer.Stop();
            walkingTimer.Stop();
            clockTimer.Stop();
            restTimer.Stop();
            awarenessTimer.Stop();
            messageTimer.Stop();
            inputWatcher.KeyPressed -= OnGlobalKeyPressed;
            inputWatcher.Dispose();
            StopAnimation();
            StopConfusedAnimation();
            confusedFrames.Clear();
            Hearts.Children.Clear();
        }

        public bool IsPlayMode => machine.PlayMode;

        public void TogglePlayMode()
        {
            SetPlayMode(!machine.PlayMode);
        }

        public void SetPlayMode(bool play)
        {
            machine.SetPlayMode(play);
            StopWalking();
            (Application.Current as App)?.UpdatePlayToggle(play);

            if (play)
            {
                // '놀자': 신나서 움직이기 시작 & 감정 표현 활성화
                DirectionTransform.ScaleX = machine.Direction;
                SetVisualState(BunnyState.Happy);
                AddHeart();
                string phrase = playPhrases[random.Next(playPhrases.Length)];
                ShowMessage(phrase, 2200);
                ScheduleBehavior(1400);
            }
            else
            {
                // '나대지마': 얌전히 멈추기 자세(식빵)로 복귀하고 이모티콘 보내는 것도 즉시 중단
                HideMessage();
                SetVisualState(BunnyState.Idle);
                ScheduleBehavior(RandomBetween(4000, 7500));
            }
        }

        public void SetRestRemindersEnabled(bool value)
        {
            restRemindersEnabled = value;
            restTimer.IsEnabled = value;
        }

        public void SetPaused(bool value)
        {
            machine.SetPaused(value);
            StopWalking();
            SetVisualState(value ? BunnyState.Sleep : BunnyState.Idle);
            ScheduleBehavior(value ? 30000 : 1400);
        }

        public void ResetPosition()
        {
            var workArea = SystemParameters.WorkArea;
            Left = workArea.Right - WindowWidth - 4;
            Top = workArea.Bottom - WindowHeight - 4;
            ClampToWorkArea();
        }

        public void SetAlwaysOnTop(bool value)
        {
            Topmost = value;
        }

        public BunnyItem CurrentItem => currentItem;

        public void SetItem(BunnyItem item)
        {
            currentItem = item;
            ItemHouse.Visibility = item == BunnyItem.House ? Visibility.Visible : Visibility.Collapsed;
            ItemChair.Visibility = item == BunnyItem.Chair ? Visibility.Visible : Visibility.Collapsed;
            ItemDoll.Visibility = item == BunnyItem.Doll ? Visibility.Visible : Visibility.Collapsed;
            ItemHay.Visibility = item == BunnyItem.Hay ? Visibility.Visible : Visibility.Collapsed;
            ItemBag.Visibility = item == BunnyItem.Bag ? Visibility.Visible : Visibility.Collapsed;

            if (item == BunnyItem.House)
            {
                DirectionLayer.Width = 52;
                DirectionLayer.Height = 58;
                DirectionLayer.Margin = new Thickness(0, 0, 2, 22);
                DirectionTransform.ScaleX = 1;
            }
            else if (item == BunnyItem.Chair)
            {
                DirectionLayer.Width = 84;
                DirectionLayer.Height = 76;
                DirectionLayer.Margin = new Thickness(0, 0, 0, 26);
                DirectionTransform.ScaleX = 1;
            }
            else
            {
                DirectionLayer.Width = 97;
                DirectionLayer.Height = 88;
                DirectionLayer.Margin = new Thickness(0);
            }

            ReactToItemEquip(item);
        }

        private void ReactToItemEquip(BunnyItem item)
        {
            if (resourcesDisposed) return;
            machine.Touch(DateTime.UtcNow);
            StopWalking();

            switch (item)
            {
                case BunnyItem.Doll:
                    DirectionTransform.ScaleX = -1;
                    machine.SetDirection(-1);
                    SetVisualState(BunnyState.Angry);
                    ShowMessage(dollPhrases[random.Next(dollPhrases.Length)], 2600, true);
                    ScheduleBehavior(3000);
                    break;

                case BunnyItem.Hay:
                    DirectionTransform.ScaleX = 1;
                    machine.SetDirection(1);
                    SetVisualState(BunnyState.Beg);
                    AddHeart();
                    ShowMessage(hayPhrases[random.Next(hayPhrases.Length)], 2500, true);
                    ScheduleBehavior(2800);
                    break;

                case BunnyItem.Bag:
                    DirectionTransform.ScaleX = 1;
                    machine.SetDirection(1);
                    SetVisualState(BunnyState.Idle);
                    AddHeart();
                    ShowMessage(bagPhrases[random.Next(bagPhrases.Length)], 2500, true);
                    ScheduleBehavior(2800);
                    break;

                case BunnyItem.House:
                    SetVisualState(BunnyState.Front);
                    AddHeart();
                    ShowMessage(housePhrases[random.Next(housePhrases.Length)], 2500, true);
                    ScheduleBehavior(3000);
                    break;

                case BunnyItem.Chair:
                    DirectionTransform.ScaleX = 1;
                    machine.SetDirection(1);
                    SetVisualState(BunnyState.Idle);
                    AddHeart();
                    ShowMessage(chairPhrases[random.Next(chairPhrases.Length)], 2500, true);
                    ScheduleBehavior(2800);
                    break;

                case BunnyItem.None:
                    ShowMessage("✨", 1800, true);
                    SetVisualState(BunnyState.Idle);
                    ScheduleBehavior(2000);
                    break;
            }
        }

        private void OnClosing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (allowClose) return;
            if (ExitOnClose)
            {
                e.Cancel = false;
                Dispatcher.BeginInvoke(new Action(delegate
                {
                    var app = Application.Current as App;
                    if (app != null) app.QuitFromWindow();
                }));
                return;
            }
            e.Cancel = true;
            Hide();
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            ResetPosition();
            Focus();
            machine.SetPlayMode(false);
            clockTimer.Start();
            restTimer.IsEnabled = restRemindersEnabled;
            awarenessTimer.Start();
            PlayIntroGreeting();
        }

        private void OnBehaviorTick(object sender, EventArgs e)
        {
            behaviorTimer.Stop();
            if (dragging || isPlayingConfusedAnim)
            {
                ScheduleBehavior(900);
                return;
            }

            if (currentItem == BunnyItem.House)
            {
                StopWalking();
                SetVisualState(BunnyState.Front);
                if (random.NextDouble() < 0.25)
                {
                    AddHeart();
                }
                ScheduleBehavior(RandomBetween(4000, 8000));
                return;
            }

            if (!machine.PlayMode)
            {
                StopWalking();
                SetVisualState(BunnyState.Idle);

                double roll = random.NextDouble();
                if (roll < 0.30)
                {
                    // 멈추기 자세에서는 가끔 갸르릉 하기 (진동 및 하트만, 이모티콘 말풍선 X)
                    TriggerPurring();
                }
                else
                {
                    AnimateSleeping();
                }

                ScheduleBehavior(RandomBetween(4500, 8500));
                return;
            }

            StopWalking();
            var next = machine.ChooseNext(random.NextDouble(), DateTime.UtcNow);
            if (next == BunnyState.Stand)
            {
                double roll = random.NextDouble();
                if (roll < 0.35)
                {
                    // '놀자' 상태에서 두리번거릴 때 실사 어리둥절 애니메이션 재생!
                    PlayConfusedAnimation(() =>
                    {
                        ResetVisualToIdle();
                    });
                    return;
                }
                else if (roll < 0.60)
                {
                    // 실사 내놔! 포즈
                    SetVisualState(BunnyState.Beg);
                    ShowMessage(begPhrases[random.Next(begPhrases.Length)], 2600);
                    ScheduleBehavior(3200);
                    return;
                }
                else if (roll < 0.80)
                {
                    // 실사 화났어! 포즈
                    SetVisualState(BunnyState.Angry);
                    ShowMessage(angryPhrases[random.Next(angryPhrases.Length)], 2600);
                    ScheduleBehavior(3200);
                    return;
                }
                else if (roll < 0.90)
                {
                    // 정면 똘망 민트
                    SetVisualState(BunnyState.Front);
                    ShowMessage(frontPhrases[random.Next(frontPhrases.Length)], 2600);
                    ScheduleBehavior(3200);
                    return;
                }
            }
            SetVisualState(next);
            if (next == BunnyState.Walk) StartWalking();
            ScheduleBehavior(next == BunnyState.Sleep
                ? RandomBetween(9500, 18000)
                : RandomBetween(3600, 7800));
        }

        private void ScheduleBehavior(int milliseconds)
        {
            behaviorTimer.Stop();
            behaviorTimer.Interval = TimeSpan.FromMilliseconds(milliseconds);
            behaviorTimer.Start();
        }

        private void StartWalking()
        {
            StopWalking();
            if (currentItem == BunnyItem.House) return;
            climbing = false;
            if (random.NextDouble() < 0.24) DirectionTransform.ScaleX = machine.TurnAround();
            walkingTimer.Start();
        }

        private void StopWalking()
        {
            walkingTimer.Stop();
        }

        private void OnWalkingTick(object sender, EventArgs e)
        {
            if (machine.State != BunnyState.Walk || dragging) return;
            var area = GetCurrentWorkArea();
            if (climbing)
            {
                var oldTop = Top;
                Top = ClimbMath.Clamp(Top + climbDirection * WalkStep, area.Top, area.Bottom - WindowHeight);
                if (Math.Abs(Top - oldTop) < WalkStep)
                {
                    if (climbDirection < 0) climbDirection = 1;
                    else
                    {
                        climbing = false;
                        Top = area.Bottom - WindowHeight;
                        DirectionTransform.ScaleX = machine.TurnAround();
                    }
                }
                return;
            }
            var oldLeft = Left;
            Left += machine.Direction * WalkStep;
            ClampToWorkArea();
            if (Math.Abs(Left - oldLeft) < WalkStep)
            {
                if (ClimbMath.ShouldClimb(random.NextDouble()))
                {
                    climbing = true;
                    climbDirection = -1;
                    Left = machine.Direction < 0 ? area.Left : area.Right - WindowWidth;
                }
                else DirectionTransform.ScaleX = machine.TurnAround();
            }
        }

        public void SetVisualState(BunnyState next)
        {
            machine.SetState(next);
            BunnyImage.Source = new BitmapImage(new Uri("pack://application:,,,/BunnyPet;component/Assets/" + ImageName(next), UriKind.Absolute));
            StopAnimation();
            switch (next)
            {
                case BunnyState.Idle:
                    if (!machine.PlayMode) AnimateSleeping();
                    else AnimateBreathing();
                    break;
                case BunnyState.Walk: AnimateHopping(); break;
                case BunnyState.Stand: AnimateCurious(); break;
                case BunnyState.Sleep: AnimateSleeping(); break;
                case BunnyState.Happy: AnimateHappy(); break;
                case BunnyState.Drag: AnimateDangling(); break;
                case BunnyState.Beg: AnimateCurious(); break;
                case BunnyState.Angry: AnimatePurring(); break;
                case BunnyState.Front: AnimateBreathing(); break;
                case BunnyState.Intro: AnimateHappy(); break;
            }
        }

        private string ImageName(BunnyState state)
        {
            if (currentItem == BunnyItem.House)
            {
                return "bunny-front.png";
            }
            if (state == BunnyState.Intro)
            {
                return "bunny-intro.png";
            }
            if (!machine.PlayMode && (state == BunnyState.Idle || state == BunnyState.Sleep))
            {
                return "bunny-sleep.png";
            }
            switch (state)
            {
                case BunnyState.Walk: return "bunny-walk.png";
                case BunnyState.Stand: return "bunny-stand.png";
                case BunnyState.Sleep: return "bunny-sleep.png";
                case BunnyState.Happy: return "bunny-happy.png";
                case BunnyState.Drag: return "bunny-stand.png";
                case BunnyState.Beg: return "bunny-beg.png";
                case BunnyState.Angry: return "bunny-angry.png";
                case BunnyState.Front: return "bunny-front.png";
                case BunnyState.Intro: return "bunny-intro.png";
                default: return "bunny-idle.png";
            }
        }

        private void StopAnimation()
        {
            StopConfusedAnimation();
            translate.BeginAnimation(TranslateTransform.YProperty, null);
            rotate.BeginAnimation(RotateTransform.AngleProperty, null);
            scale.BeginAnimation(ScaleTransform.ScaleXProperty, null);
            scale.BeginAnimation(ScaleTransform.ScaleYProperty, null);
            translate.Y = 0;
            rotate.Angle = 0;
            scale.ScaleX = 1;
            scale.ScaleY = 1;
        }

        private void AnimateBreathing() { Animate(translate, TranslateTransform.YProperty, 0, 1, 2800); Animate(scale, ScaleTransform.ScaleXProperty, 1, 1.008, 2800); Animate(scale, ScaleTransform.ScaleYProperty, 1, 0.995, 2800); }
        private void AnimatePurring()
        {
            Animate(scale, ScaleTransform.ScaleYProperty, 1.0, 0.96, 110, 8);
            Animate(scale, ScaleTransform.ScaleXProperty, 1.0, 1.03, 110, 8);
            Animate(translate, TranslateTransform.YProperty, 0, 1.0, 110, 8);
        }
        private void AnimateHopping() { Animate(translate, TranslateTransform.YProperty, 0, -3, 480); Animate(rotate, RotateTransform.AngleProperty, 0, -1, 480); }
        private void AnimateCurious() { Animate(rotate, RotateTransform.AngleProperty, 0, -2, 1700); Animate(translate, TranslateTransform.YProperty, 0, -1, 1700); }
        private void AnimateSleeping() { Animate(scale, ScaleTransform.ScaleXProperty, 1, 1.012, 3400); Animate(scale, ScaleTransform.ScaleYProperty, 1, 0.992, 3400); }
        private void AnimateHappy() { Animate(translate, TranslateTransform.YProperty, 0, -4, 420, 4); Animate(rotate, RotateTransform.AngleProperty, 0, 2, 420, 4); }
        private void AnimateDangling() { Animate(rotate, RotateTransform.AngleProperty, -2, 2, 620); }

        public void TriggerPurring()
        {
            if (resourcesDisposed || dragging || !IsVisible) return;
            AnimatePurring();
            AddHeart();
            if (machine.PlayMode)
            {
                string phrase = purrPhrases[random.Next(purrPhrases.Length)];
                ShowMessage(phrase, 2200);
            }
            Task.Delay(1800).ContinueWith(_ =>
            {
                if (resourcesDisposed || Dispatcher.HasShutdownStarted || Dispatcher.HasShutdownFinished) return;
                try
                {
                    Dispatcher.BeginInvoke(new Action(delegate
                    {
                        if (!resourcesDisposed && !dragging && !machine.PlayMode && machine.State == BunnyState.Idle)
                        {
                            StopAnimation();
                            AnimateSleeping();
                        }
                    }));
                }
                catch (InvalidOperationException) { }
            });
        }

        private void ShowRestingEmoji()
        {
            if (resourcesDisposed || dragging || !IsVisible || !machine.PlayMode) return;
            string emoji = restingPhrases[random.Next(restingPhrases.Length)];
            ShowMessage(emoji, 2500);
        }

        private static void Animate(Animatable target, DependencyProperty property, double from, double to, int duration, int repeatCount = 0)
        {
            var animation = new DoubleAnimation(from, to, TimeSpan.FromMilliseconds(duration))
            {
                AutoReverse = true,
                RepeatBehavior = repeatCount > 0 ? new RepeatBehavior(repeatCount) : RepeatBehavior.Forever
            };
            target.BeginAnimation(property, animation);
        }

        public void ReactToPetting()
        {
            machine.Touch(DateTime.UtcNow);
            StopWalking();

            if (currentItem == BunnyItem.House)
            {
                AddHeart();
                string housePhrase = housePhrases[random.Next(housePhrases.Length)];
                ShowMessage(housePhrase, 2500, true);
                ScheduleBehavior(3000);
                return;
            }

            if (currentItem == BunnyItem.Doll)
            {
                DirectionTransform.ScaleX = -1;
                machine.SetDirection(-1);
                SetVisualState(BunnyState.Angry);
                string dollPhrase = dollPhrases[random.Next(dollPhrases.Length)];
                ShowMessage(dollPhrase, 2800, true);
                ScheduleBehavior(3000);
                return;
            }

            if (currentItem == BunnyItem.Hay)
            {
                DirectionTransform.ScaleX = 1;
                machine.SetDirection(1);
                SetVisualState(BunnyState.Beg);
                AddHeart();
                string hayPhrase = hayPhrases[random.Next(hayPhrases.Length)];
                ShowMessage(hayPhrase, 2500, true);
                ScheduleBehavior(2800);
                return;
            }

            if (!machine.PlayMode)
            {
                // 멈추기 자세에서 쓰다듬으면 조용히 기분 좋게 갸르릉하기 (하트 띄우기, 이모티콘 말풍선 X)
                AnimatePurring();
                AddHeart();
                Task.Delay(190).ContinueWith(_ =>
                {
                    if (resourcesDisposed || Dispatcher.HasShutdownStarted || Dispatcher.HasShutdownFinished) return;
                    try
                    {
                        Dispatcher.BeginInvoke(new Action(delegate
                        {
                            if (!resourcesDisposed) AddHeart();
                        }));
                    }
                    catch (InvalidOperationException) { }
                });
                Task.Delay(1800).ContinueWith(_ =>
                {
                    if (resourcesDisposed || Dispatcher.HasShutdownStarted || Dispatcher.HasShutdownFinished) return;
                    try
                    {
                        Dispatcher.BeginInvoke(new Action(delegate
                        {
                            if (!resourcesDisposed && !dragging && !machine.PlayMode && machine.State == BunnyState.Idle)
                            {
                                StopAnimation();
                                AnimateSleeping();
                            }
                        }));
                    }
                    catch (InvalidOperationException) { }
                });

                ScheduleBehavior(RandomBetween(4500, 8000));
                return;
            }

            SetVisualState(BunnyState.Happy);
            AddHeart();
            Task.Delay(190).ContinueWith(_ =>
            {
                if (resourcesDisposed || Dispatcher.HasShutdownStarted || Dispatcher.HasShutdownFinished) return;
                try
                {
                    Dispatcher.BeginInvoke(new Action(delegate
                    {
                        if (!resourcesDisposed) AddHeart();
                    }));
                }
                catch (InvalidOperationException) { }
            });

            string happyPhrase = GetItemPetPhrase();
            ShowMessage(happyPhrase, 2200);
            ScheduleBehavior(2400);
        }

        private string GetItemPetPhrase()
        {
            if (currentItem == BunnyItem.Hay) return hayPhrases[random.Next(hayPhrases.Length)];
            if (currentItem == BunnyItem.Bag) return bagPhrases[random.Next(bagPhrases.Length)];
            if (currentItem == BunnyItem.House) return housePhrases[random.Next(housePhrases.Length)];
            if (currentItem == BunnyItem.Chair) return chairPhrases[random.Next(chairPhrases.Length)];
            return phrases[random.Next(phrases.Length)];
        }

        private void AddHeart()
        {
            if (resourcesDisposed) return;
            var heart = new TextBlock
            {
                Text = "♥",
                Foreground = new SolidColorBrush(Color.FromRgb(233, 140, 155)),
                FontSize = 10,
                FontWeight = FontWeights.Black
            };
            var drift = RandomBetween(-21, 21);
            var turn = RandomBetween(-22, 22);
            Canvas.SetLeft(heart, 48);
            Canvas.SetTop(heart, 43);
            Hearts.Children.Add(heart);
            var transform = new TransformGroup();
            transform.Children.Add(new TranslateTransform());
            transform.Children.Add(new RotateTransform());
            heart.RenderTransform = transform;
            var move = (TranslateTransform)transform.Children[0];
            var angle = (RotateTransform)transform.Children[1];
            var animation = new DoubleAnimation(0, -31, TimeSpan.FromMilliseconds(1350)) { EasingFunction = new QuadraticEase() };
            animation.Completed += delegate { Hearts.Children.Remove(heart); };
            move.BeginAnimation(TranslateTransform.YProperty, animation);
            move.BeginAnimation(TranslateTransform.XProperty, new DoubleAnimation(0, drift, TimeSpan.FromMilliseconds(1350)));
            angle.BeginAnimation(RotateTransform.AngleProperty, new DoubleAnimation(0, turn, TimeSpan.FromMilliseconds(1350)));
            heart.BeginAnimation(OpacityProperty, new DoubleAnimation(1, 0, TimeSpan.FromMilliseconds(1350)));
        }

        private static string GetEmojiCode(string text)
        {
            var codes = new List<string>();
            for (int i = 0; i < text.Length; i += char.IsSurrogatePair(text, i) ? 2 : 1)
            {
                codes.Add(char.ConvertToUtf32(text, i).ToString("x"));
            }
            return string.Join("_", codes);
        }

        private void InitEmojiBitmaps()
        {
            var allEmojis = phrases
                .Concat(cursorPhrases)
                .Concat(dollPhrases)
                .Concat(hayPhrases)
                .Concat(bagPhrases)
                .Concat(housePhrases)
                .Concat(chairPhrases)
                .Concat(restingPhrases)
                .Concat(purrPhrases)
                .Concat(playPhrases)
                .Concat(scoldPhrases)
                .Concat(begPhrases)
                .Concat(angryPhrases)
                .Concat(frontPhrases)
                .Concat(new[] { "✨", "👋", "⏰", "🍵", "❗", "🍞", "💤", "👽", "👾", "🛸" })
                .Distinct();

            foreach (var em in allEmojis)
            {
                try
                {
                    var code = GetEmojiCode(em);
                    var uri = new Uri($"pack://application:,,,/BunnyPet;component/Assets/emojis/{code}.png", UriKind.Absolute);
                    var bmp = new BitmapImage();
                    bmp.BeginInit();
                    bmp.UriSource = uri;
                    bmp.CacheOption = BitmapCacheOption.OnLoad;
                    bmp.EndInit();
                    bmp.Freeze();
                    emojiBitmaps[em] = bmp;
                }
                catch { }
            }
        }

        private void InitConfusedFrames()
        {
            for (int i = 0; i < 46; i++)
            {
                try
                {
                    var uri = new Uri($"pack://application:,,,/BunnyPet;component/Assets/confused/confused_{i:03d}.png", UriKind.Absolute);
                    var bmp = new BitmapImage();
                    bmp.BeginInit();
                    bmp.UriSource = uri;
                    bmp.CacheOption = BitmapCacheOption.OnLoad;
                    bmp.EndInit();
                    bmp.Freeze();
                    confusedFrames.Add(bmp);
                }
                catch { }
            }
        }

        public void PlayConfusedAnimation(Action onCompleted = null)
        {
            if (confusedFrames.Count == 0 || resourcesDisposed)
            {
                onCompleted?.Invoke();
                return;
            }

            if (currentItem == BunnyItem.House)
            {
                SetItem(BunnyItem.None);
            }

            StopAnimation();
            StopWalking();
            DirectionTransform.ScaleX = 1;
            isPlayingConfusedAnim = true;
            confusedFrameIndex = 0;
            confusedCompletedCallback = onCompleted;

            BunnyImage.Source = confusedFrames[0];

            string[] confusedEmojis = { "👀", "❓", "🤔", "😳", "🐰", "👽" };
            ShowMessage(confusedEmojis[random.Next(confusedEmojis.Length)], 3200, true);

            if (confusedAnimTimer == null)
            {
                confusedAnimTimer = new DispatcherTimer(DispatcherPriority.Render);
                confusedAnimTimer.Interval = TimeSpan.FromMilliseconds(66);
                confusedAnimTimer.Tick += OnConfusedAnimTick;
            }
            else
            {
                confusedAnimTimer.Stop();
            }

            confusedAnimTimer.Start();
        }

        private void OnConfusedAnimTick(object sender, EventArgs e)
        {
            if (resourcesDisposed || !isPlayingConfusedAnim)
            {
                confusedAnimTimer?.Stop();
                isPlayingConfusedAnim = false;
                return;
            }

            confusedFrameIndex++;
            if (confusedFrameIndex < confusedFrames.Count)
            {
                BunnyImage.Source = confusedFrames[confusedFrameIndex];
            }
            else
            {
                confusedAnimTimer.Stop();
                isPlayingConfusedAnim = false;
                var cb = confusedCompletedCallback;
                confusedCompletedCallback = null;
                cb?.Invoke();
            }
        }

        private void StopConfusedAnimation()
        {
            if (isPlayingConfusedAnim)
            {
                confusedAnimTimer?.Stop();
                isPlayingConfusedAnim = false;
                confusedCompletedCallback = null;
            }
        }

        public void ResetVisualToIdle()
        {
            SetVisualState(BunnyState.Idle);
            ScheduleBehavior(RandomBetween(2500, 5000));
        }

        public void TriggerPose(BunnyState state)
        {
            if (resourcesDisposed) return;
            StopAnimation();
            StopWalking();
            if (currentItem == BunnyItem.House)
            {
                SetItem(BunnyItem.None);
            }
            SetVisualState(state);
            switch (state)
            {
                case BunnyState.Beg:
                    ShowMessage(begPhrases[random.Next(begPhrases.Length)], 3000, true);
                    break;
                case BunnyState.Angry:
                    ShowMessage(angryPhrases[random.Next(angryPhrases.Length)], 3000, true);
                    break;
                case BunnyState.Front:
                    ShowMessage(frontPhrases[random.Next(frontPhrases.Length)], 3000, true);
                    break;
                case BunnyState.Intro:
                    AddHeart();
                    ShowMessage(introPhrases[random.Next(introPhrases.Length)], 3000, true);
                    break;
            }
            ScheduleBehavior(3600);
        }

        public void PlayIntroGreeting()
        {
            if (resourcesDisposed) return;
            DirectionTransform.ScaleX = 1;
            TriggerPose(BunnyState.Intro);
        }

        private void HideMessage()
        {
            if (messageTimer != null) messageTimer.Stop();
            if (MessageBorder != null)
            {
                MessageBorder.BeginAnimation(OpacityProperty, null);
                MessageBorder.Visibility = Visibility.Collapsed;
            }
        }

        public void ShowMessage(string text, int duration, bool force = false)
        {
            if (resourcesDisposed || (!machine.PlayMode && !force)) return;
            messageTimer.Stop();

            if (!emojiBitmaps.TryGetValue(text, out var bmp))
            {
                try
                {
                    var code = GetEmojiCode(text);
                    var uri = new Uri($"pack://application:,,,/BunnyPet;component/Assets/emojis/{code}.png", UriKind.Absolute);
                    var newBmp = new BitmapImage();
                    newBmp.BeginInit();
                    newBmp.UriSource = uri;
                    newBmp.CacheOption = BitmapCacheOption.OnLoad;
                    newBmp.EndInit();
                    newBmp.Freeze();
                    emojiBitmaps[text] = newBmp;
                    bmp = newBmp;
                }
                catch
                {
                    bmp = null;
                }
            }

            if (bmp != null)
            {
                MessageEmoji.Source = bmp;
                MessageEmoji.Visibility = Visibility.Visible;
                Message.Visibility = Visibility.Collapsed;
            }
            else
            {
                Message.Text = text;
                Message.Visibility = Visibility.Visible;
                MessageEmoji.Visibility = Visibility.Collapsed;
            }

            MessageBorder.Visibility = Visibility.Visible;
            MessageBorder.BeginAnimation(OpacityProperty, null);
            MessageBorder.Opacity = 1;

            messageTimer.Interval = TimeSpan.FromMilliseconds(duration);
            messageTimer.Start();
        }

        private void OnMessageTimerTick(object sender, EventArgs e)
        {
            messageTimer.Stop();
            if (resourcesDisposed) return;
            var fade = new DoubleAnimation(1, 0, TimeSpan.FromMilliseconds(180));
            fade.Completed += delegate
            {
                if (!messageTimer.IsEnabled)
                {
                    MessageBorder.Visibility = Visibility.Collapsed;
                }
            };
            MessageBorder.BeginAnimation(OpacityProperty, fade);
        }

        private void OnClockTick(object sender, EventArgs e)
        {
            var now = DateTime.Now;
            if (!IsVisible || dragging || !machine.PlayMode || now.Minute != 0 || now.Hour == lastAnnouncedHour) return;
            lastAnnouncedHour = now.Hour;
            ShowMessage(FormatHourPhrase(now), 2600);
        }

        private static string FormatHourPhrase(DateTime now)
        {
            return "⏰";
        }

        private void OnRestTick(object sender, EventArgs e)
        {
            if (!restRemindersEnabled || !IsVisible || dragging || !machine.PlayMode) return;
            ShowMessage("🍵", 3400);
        }

        private void OnAwarenessTick(object sender, EventArgs e)
        {
            if (dragging || !IsVisible || machine.Paused || machine.State == BunnyState.Happy || machine.State == BunnyState.Drag) return;
            try
            {
                var cursorScreen = Forms.Cursor.Position;
                var screenPoint = new Point(cursorScreen.X, cursorScreen.Y);
                var local = PointFromScreen(screenPoint);
                var dx = local.X - WindowWidth / 2.0;
                var dy = local.Y - WindowHeight / 2.0;
                var distance = Math.Sqrt(dx * dx + dy * dy);
                if (distance < 110)
                {
                    CheckAndReactToCursor(local.X, local.Y);
                }
            }
            catch (InvalidOperationException) { }
        }

        private void CheckAndReactToCursor(double localX, double localY)
        {
            if (resourcesDisposed || dragging || !IsVisible) return;
            if (machine.Paused || machine.State == BunnyState.Happy || machine.State == BunnyState.Drag) return;

            var now = DateTime.UtcNow;
            if (now - lastCursorReactionUtc < TimeSpan.FromSeconds(2.5)) return;

            lastCursorReactionUtc = now;
            machine.Touch(now);
            StopWalking();

            var dx = localX - WindowWidth / 2.0;
            var faceDirection = dx < 0 ? -1 : 1;
            DirectionTransform.ScaleX = faceDirection;
            machine.SetDirection(faceDirection);

            if (!machine.PlayMode)
            {
                SetVisualState(BunnyState.Idle);
                ScheduleBehavior(3600);
                return;
            }

            SetVisualState(BunnyState.Stand);
            var phraseStand = cursorPhrases[random.Next(cursorPhrases.Length)];
            ShowMessage(phraseStand, 2000);
            ScheduleBehavior(2300);
        }

        private void OnMouseEnter(object sender, MouseEventArgs e)
        {
            var pos = e.GetPosition(this);
            CheckAndReactToCursor(pos.X, pos.Y);
        }

        private void OnGlobalKeyPressed()
        {
            if (resourcesDisposed) return;
            try
            {
                Dispatcher.BeginInvoke(new Action(ReactToTyping));
            }
            catch (InvalidOperationException) { }
        }

        private void ReactToTyping()
        {
            // ponytail: only Idle/Sleep don't loop-animate the rotate transform,
            // so a twitch there can't fight Walk/Stand/Happy's own rotate animation.
            if (resourcesDisposed || dragging || !IsVisible) return;
            if (machine.State != BunnyState.Idle && machine.State != BunnyState.Sleep) return;
            if (DateTime.UtcNow - lastKeyReactionUtc < TimeSpan.FromSeconds(6)) return;
            lastKeyReactionUtc = DateTime.UtcNow;
            Animate(rotate, RotateTransform.AngleProperty, 0, 7, 130, 2);
        }

        private void OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton != MouseButton.Left) return;
            machine.Touch(DateTime.UtcNow);
            dragging = true;
            dragMoved = false;
            dragDistance = 0;
            strokeDistance = 0;
            lastPointer = GetPointerPosition(e);
            Root.CaptureMouse();
            e.Handled = true;
        }

        private void OnMouseMove(object sender, MouseEventArgs e)
        {
            if (!dragging)
            {
                var pos = e.GetPosition(this);
                CheckAndReactToCursor(pos.X, pos.Y);

                // Mouse stroke / rubbing over bunny body detection
                var now = DateTime.UtcNow;
                if ((now - lastStrokeTime).TotalMilliseconds > 700)
                {
                    strokeDistance = 0;
                }
                lastStrokeTime = now;
                var currentPos = GetPointerPosition(e);
                if (lastPointer.X != 0 || lastPointer.Y != 0)
                {
                    strokeDistance += Math.Abs(currentPos.X - lastPointer.X) + Math.Abs(currentPos.Y - lastPointer.Y);
                }
                lastPointer = currentPos;
                if (strokeDistance > 70 && (now - lastPetReactionUtc).TotalSeconds > 2.2)
                {
                    lastPetReactionUtc = now;
                    strokeDistance = 0;
                    ReactToPetting();
                }
                return;
            }
            var current = GetPointerPosition(e);
            var dx = current.X - lastPointer.X;
            var dy = current.Y - lastPointer.Y;
            dragDistance += Math.Abs(dx) + Math.Abs(dy);
            if (dragDistance <= DragThreshold) return;
            if (!dragMoved)
            {
                dragMoved = true;
                StopWalking();
                SetVisualState(BunnyState.Drag);
            }
            Left += dx;
            Top += dy;
            ClampToWorkArea();
            lastPointer = current;
        }

        private void OnMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            FinishPointer();
            e.Handled = true;
        }

        private void FinishPointer()
        {
            if (!dragging) return;
            Root.ReleaseMouseCapture();
            dragging = false;
            if (dragMoved)
            {
                SetVisualState(BunnyState.Idle);
                ScheduleBehavior(1700);
            }
            else ReactToPetting();
        }

        private void OnMouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            machine.Touch(DateTime.UtcNow);
            FinishPointer();
            TogglePlayMode();
            e.Handled = true;
        }

        private void OnMouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (dragging && dragMoved)
            {
                FinishPointer();
                return;
            }
            CancelPointer();
            ShowContextMenu();
            e.Handled = true;
        }

        private void ShowContextMenu()
        {
            var menu = new ContextMenu();
            menu.PlacementTarget = this;

            var playToggleItem = new MenuItem
            {
                Header = machine.PlayMode ? "🛑 나대지마 (멈추기)" : "🎉 놀자! (움직이기)",
                FontWeight = FontWeights.Bold
            };
            playToggleItem.Click += delegate
            {
                TogglePlayMode();
            };
            menu.Items.Add(playToggleItem);
            menu.Items.Add(new Separator());

            var petItem = new MenuItem { Header = "🖐️ 민트 쓰다듬기" };
            petItem.Click += delegate { ReactToPetting(); };
            menu.Items.Add(petItem);

            var poseMenu = new MenuItem { Header = "📸 민트 특별 포즈" };

            var begItem = new MenuItem { Header = "🌾 간식 내놔! 민트 (실사 포즈)" };
            begItem.Click += delegate { TriggerPose(BunnyState.Beg); };
            poseMenu.Items.Add(begItem);

            var angryItem = new MenuItem { Header = "💢 화났어! 민트 (실사 포즈)" };
            angryItem.Click += delegate { TriggerPose(BunnyState.Angry); };
            poseMenu.Items.Add(angryItem);

            var frontItem = new MenuItem { Header = "🐰 똘망똘망 민트 (정면 포즈)" };
            frontItem.Click += delegate { TriggerPose(BunnyState.Front); };
            poseMenu.Items.Add(frontItem);

            var confusedItem = new MenuItem { Header = "👀 어리둥절 민트 (실사 영상)" };
            confusedItem.Click += delegate
            {
                if (currentItem == BunnyItem.House) SetItem(BunnyItem.None);
                PlayConfusedAnimation(() =>
                {
                    ResetVisualToIdle();
                });
            };
            poseMenu.Items.Add(confusedItem);

            var introItem = new MenuItem { Header = "✨ 천사 민트 등장! (실사 포즈)" };
            introItem.Click += delegate { PlayIntroGreeting(); };
            poseMenu.Items.Add(introItem);

            menu.Items.Add(poseMenu);
            menu.Items.Add(new Separator());

            var itemsMenu = new MenuItem { Header = "🎁 민트에게 선물하기" };

            var hayItem = new MenuItem { Header = "🌾 맛있는 건초 주기" };
            hayItem.Click += delegate { SetItem(BunnyItem.Hay); };
            itemsMenu.Items.Add(hayItem);

            var chairItem = new MenuItem { Header = "🪑 작은 의자 놓기" };
            chairItem.Click += delegate { SetItem(BunnyItem.Chair); };
            itemsMenu.Items.Add(chairItem);

            var dollItem = new MenuItem { Header = "🧸 토끼 인형 놓기" };
            dollItem.Click += delegate { SetItem(BunnyItem.Doll); };
            itemsMenu.Items.Add(dollItem);

            var bagItem = new MenuItem { Header = "🎒 소풍 가방 메어주기" };
            bagItem.Click += delegate { SetItem(BunnyItem.Bag); };
            itemsMenu.Items.Add(bagItem);

            var houseItem = new MenuItem { Header = "🏠 아늑한 집 지어주기" };
            houseItem.Click += delegate { SetItem(BunnyItem.House); };
            itemsMenu.Items.Add(houseItem);

            itemsMenu.Items.Add(new Separator());

            var clearItem = new MenuItem { Header = "❌ 아이템 치우기" };
            clearItem.Click += delegate { SetItem(BunnyItem.None); };
            itemsMenu.Items.Add(clearItem);

            menu.Items.Add(itemsMenu);
            menu.Items.Add(new Separator());

            var topItem = new MenuItem { Header = "📌 항상 위에 표시", IsCheckable = true, IsChecked = Topmost };
            topItem.Click += delegate
            {
                SetAlwaysOnTop(topItem.IsChecked);
            };
            menu.Items.Add(topItem);

            var resetItem = new MenuItem { Header = "↩ 민트 자리로 부르기 (오른쪽 아래)" };
            resetItem.Click += delegate
            {
                ResetPosition();
                PlayIntroGreeting();
            };
            menu.Items.Add(resetItem);

            menu.Items.Add(new Separator());

            var quitItem = new MenuItem { Header = "👋 민트 재우기 (종료)" };
            quitItem.Click += delegate
            {
                var app = Application.Current as App;
                if (app != null) app.QuitFromWindow();
                else Close();
            };
            menu.Items.Add(quitItem);

            menu.IsOpen = true;
        }

        private void OnKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key != Key.Escape) return;
            CancelPointer();
            StopWalking();
            SetVisualState(BunnyState.Idle);
            ResetPosition();
            ScheduleBehavior(1600);
            e.Handled = true;
        }

        private void ClampToWorkArea()
        {
            var area = GetCurrentWorkArea();
            Left = Math.Max(area.Left, Math.Min(Left, area.Right - WindowWidth));
            Top = Math.Max(area.Top, Math.Min(Top, area.Bottom - WindowHeight));
        }

        private Rect GetCurrentWorkArea()
        {
            var handle = new System.Windows.Interop.WindowInteropHelper(this).Handle;
            var monitor = MonitorFromWindow(handle, 2);
            var info = new MonitorInfo { cbSize = Marshal.SizeOf(typeof(MonitorInfo)) };
            if (monitor != IntPtr.Zero && GetMonitorInfo(monitor, ref info))
            {
                var fromDevice = GetFromDeviceTransform(handle);
                var topLeft = fromDevice.Transform(new Point(info.rcWork.Left, info.rcWork.Top));
                var bottomRight = fromDevice.Transform(new Point(info.rcWork.Right, info.rcWork.Bottom));
                return new Rect(topLeft, bottomRight);
            }
            return SystemParameters.WorkArea;
        }

        private Point GetPointerPosition(MouseEventArgs e)
        {
            var local = e.GetPosition(this);
            return new Point(Left + local.X, Top + local.Y);
        }

        private void CancelPointer()
        {
            if (!dragging) return;
            Root.ReleaseMouseCapture();
            dragging = false;
            dragMoved = false;
            dragDistance = 0;
        }

        private static Matrix GetFromDeviceTransform(IntPtr handle)
        {
            var source = PresentationSource.FromVisual(Application.Current.MainWindow);
            if (source != null && source.CompositionTarget != null)
                return source.CompositionTarget.TransformFromDevice;
            var dpi = handle == IntPtr.Zero ? 96u : GetDpiForWindow(handle);
            if (dpi == 0) dpi = 96;
            var scale = 96.0 / dpi;
            return new Matrix(scale, 0, 0, scale, 0, 0);
        }

        private int RandomBetween(int min, int max)
        {
            return random.Next(min, max + 1);
        }

        [DllImport("user32.dll")]
        private static extern IntPtr MonitorFromWindow(IntPtr hwnd, uint flags);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern bool GetMonitorInfo(IntPtr monitor, ref MonitorInfo info);

        [DllImport("user32.dll")]
        private static extern uint GetDpiForWindow(IntPtr hwnd);

        [StructLayout(LayoutKind.Sequential)]
        private struct RectNative { public int Left; public int Top; public int Right; public int Bottom; }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        private struct MonitorInfo
        {
            public int cbSize;
            public RectNative rcMonitor;
            public RectNative rcWork;
            public uint dwFlags;
        }
    }
}
