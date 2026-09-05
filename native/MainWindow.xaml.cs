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
            "👀", "❓", "👋", "🐰", "😳", "✨", "⭐", "💡", "🔍", "🧐", "🤍", "🐾", "😮", "💫"
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
            Hearts.Children.Clear();
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
                    SetVisualState(BunnyState.Stand);
                    ShowMessage(dollPhrases[random.Next(dollPhrases.Length)], 2600);
                    ScheduleBehavior(3000);
                    break;

                case BunnyItem.Hay:
                    DirectionTransform.ScaleX = 1;
                    machine.SetDirection(1);
                    SetVisualState(BunnyState.Happy);
                    AddHeart();
                    ShowMessage(hayPhrases[random.Next(hayPhrases.Length)], 2500);
                    ScheduleBehavior(2800);
                    break;

                case BunnyItem.Bag:
                    SetVisualState(BunnyState.Happy);
                    ShowMessage(bagPhrases[random.Next(bagPhrases.Length)], 2500);
                    ScheduleBehavior(2800);
                    break;

                case BunnyItem.House:
                    SetVisualState(BunnyState.Idle);
                    ShowMessage(housePhrases[random.Next(housePhrases.Length)], 2500);
                    ScheduleBehavior(2800);
                    break;

                case BunnyItem.Chair:
                    SetVisualState(BunnyState.Idle);
                    AddHeart();
                    ShowMessage(chairPhrases[random.Next(chairPhrases.Length)], 2500);
                    ScheduleBehavior(2800);
                    break;

                case BunnyItem.None:
                    ShowMessage("✨", 1800);
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
            SetVisualState(BunnyState.Idle);
            ScheduleBehavior(RandomBetween(3500, 7600));
            ShowMessage("👋", 2000);
            clockTimer.Start();
            restTimer.IsEnabled = restRemindersEnabled;
            awarenessTimer.Start();
        }

        private void OnBehaviorTick(object sender, EventArgs e)
        {
            behaviorTimer.Stop();
            if (dragging)
            {
                ScheduleBehavior(900);
                return;
            }

            StopWalking();
            var next = machine.ChooseNext(random.NextDouble(), DateTime.UtcNow);
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

        private void SetVisualState(BunnyState next)
        {
            machine.SetState(next);
            BunnyImage.Source = new BitmapImage(new Uri("pack://application:,,,/Assets/" + ImageName(next), UriKind.Absolute));
            StopAnimation();
            switch (next)
            {
                case BunnyState.Idle: AnimateBreathing(); break;
                case BunnyState.Walk: AnimateHopping(); break;
                case BunnyState.Stand: AnimateCurious(); break;
                case BunnyState.Sleep: AnimateSleeping(); break;
                case BunnyState.Happy: AnimateHappy(); break;
                case BunnyState.Drag: AnimateDangling(); break;
            }
        }

        private static string ImageName(BunnyState state)
        {
            switch (state)
            {
                case BunnyState.Walk: return "bunny-walk.png";
                case BunnyState.Stand: return "bunny-stand.png";
                case BunnyState.Sleep: return "bunny-sleep.png";
                case BunnyState.Happy: return "bunny-happy.png";
                case BunnyState.Drag: return "bunny-stand.png";
                default: return "bunny-idle.png";
            }
        }

        private void StopAnimation()
        {
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
        private void AnimateHopping() { Animate(translate, TranslateTransform.YProperty, 0, -3, 480); Animate(rotate, RotateTransform.AngleProperty, 0, -1, 480); }
        private void AnimateCurious() { Animate(rotate, RotateTransform.AngleProperty, 0, -2, 1700); Animate(translate, TranslateTransform.YProperty, 0, -1, 1700); }
        private void AnimateSleeping() { Animate(scale, ScaleTransform.ScaleXProperty, 1, 1.012, 3400); Animate(scale, ScaleTransform.ScaleYProperty, 1, 0.992, 3400); }
        private void AnimateHappy() { Animate(translate, TranslateTransform.YProperty, 0, -4, 420, 4); Animate(rotate, RotateTransform.AngleProperty, 0, 2, 420, 4); }
        private void AnimateDangling() { Animate(rotate, RotateTransform.AngleProperty, -2, 2, 620); }

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

            if (currentItem == BunnyItem.Doll)
            {
                DirectionTransform.ScaleX = -1;
                machine.SetDirection(-1);
                SetVisualState(BunnyState.Stand);
                string phrase = dollPhrases[random.Next(dollPhrases.Length)];
                ShowMessage(phrase, 2800);
                ScheduleBehavior(3000);
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

            string petPhrase;
            if (currentItem == BunnyItem.Hay)
            {
                petPhrase = hayPhrases[random.Next(hayPhrases.Length)];
            }
            else if (currentItem == BunnyItem.Bag)
            {
                petPhrase = bagPhrases[random.Next(bagPhrases.Length)];
            }
            else if (currentItem == BunnyItem.House)
            {
                petPhrase = housePhrases[random.Next(housePhrases.Length)];
            }
            else if (currentItem == BunnyItem.Chair)
            {
                petPhrase = chairPhrases[random.Next(chairPhrases.Length)];
            }
            else
            {
                petPhrase = phrases[random.Next(phrases.Length)];
            }

            ShowMessage(petPhrase, 2200);
            ScheduleBehavior(2400);
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
                .Concat(new[] { "✨", "👋", "⏰", "🍵", "❗" })
                .Distinct();

            foreach (var em in allEmojis)
            {
                try
                {
                    var code = GetEmojiCode(em);
                    var uri = new Uri($"pack://application:,,,/Assets/emojis/{code}.png", UriKind.Absolute);
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

        private void ShowMessage(string text, int duration)
        {
            if (resourcesDisposed) return;
            messageTimer.Stop();

            if (!emojiBitmaps.TryGetValue(text, out var bmp))
            {
                try
                {
                    var code = GetEmojiCode(text);
                    var uri = new Uri($"pack://application:,,,/Assets/emojis/{code}.png", UriKind.Absolute);
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
            if (!IsVisible || dragging || now.Minute != 0 || now.Hour == lastAnnouncedHour) return;
            lastAnnouncedHour = now.Hour;
            ShowMessage(FormatHourPhrase(now), 2600);
        }

        private static string FormatHourPhrase(DateTime now)
        {
            return "⏰";
        }

        private void OnRestTick(object sender, EventArgs e)
        {
            if (!restRemindersEnabled || !IsVisible || dragging) return;
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

            SetVisualState(BunnyState.Stand);
            var phrase = cursorPhrases[random.Next(cursorPhrases.Length)];
            ShowMessage(phrase, 2000);
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
            StopWalking();
            SetVisualState(BunnyState.Stand);
            ShowMessage("❗", 2100);
            ScheduleBehavior(3000);
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

            var petItem = new MenuItem { Header = "🖐️ 민트 쓰다듬기" };
            petItem.Click += delegate { ReactToPetting(); };
            menu.Items.Add(petItem);
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

            var pauseItem = new MenuItem { Header = machine.Paused ? "▶ 민트 다시 움직이기" : "⏸ 민트 잠깐 멈추기" };
            pauseItem.Click += delegate
            {
                SetPaused(!machine.Paused);
            };
            menu.Items.Add(pauseItem);

            var topItem = new MenuItem { Header = "📌 항상 위에 표시", IsCheckable = true, IsChecked = Topmost };
            topItem.Click += delegate
            {
                SetAlwaysOnTop(topItem.IsChecked);
            };
            menu.Items.Add(topItem);

            var resetItem = new MenuItem { Header = "↩ 민트 자리로 부르기 (오른쪽 아래)" };
            resetItem.Click += delegate { ResetPosition(); };
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
