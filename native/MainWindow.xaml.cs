using System;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace BunnyPet
{
    public partial class MainWindow : Window
    {
        private const int WindowWidth = 107;
        private const int WindowHeight = 100;
        private const int WalkStep = 2;
        private const double DragThreshold = 2;

        private readonly BunnyStateMachine machine;
        private readonly DispatcherTimer behaviorTimer;
        private readonly DispatcherTimer walkingTimer;
        private readonly Random random = new Random();
        private readonly TranslateTransform translate = new TranslateTransform();
        private readonly RotateTransform rotate = new RotateTransform();
        private readonly ScaleTransform scale = new ScaleTransform(1, 1);
        private readonly string[] phrases =
        {
            "오늘도 같이 있어요",
            "코를 살짝 눌러 주세요",
            "간식 생각 중…",
            "옆에 있어도 될까요?",
            "쓰다듬어 줘서 고마워요"
        };

        private Point lastPointer;
        private double dragDistance;
        private bool dragging;
        private bool dragMoved;
        private bool allowClose;
        private volatile bool resourcesDisposed;

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
            StopAnimation();
            Hearts.Children.Clear();
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
            var oldLeft = Left;
            Left += machine.Direction * WalkStep;
            ClampToWorkArea();
            if (Math.Abs(Left - oldLeft) < WalkStep) DirectionTransform.ScaleX = machine.TurnAround();
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

        private void ReactToPetting()
        {
            machine.Touch(DateTime.UtcNow);
            StopWalking();
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
            ShowMessage(phrases[random.Next(phrases.Length)], 2100);
            ScheduleBehavior(2300);
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

        private void ShowMessage(string text, int duration)
        {
            Message.Text = text;
            Message.Visibility = Visibility.Visible;
            MessageBorder.BeginAnimation(OpacityProperty, null);
            MessageBorder.Opacity = 1;
            var fade = new DoubleAnimation(1, 0, TimeSpan.FromMilliseconds(160))
            {
                BeginTime = TimeSpan.FromMilliseconds(duration)
            };
            fade.Completed += delegate { Message.Visibility = Visibility.Collapsed; };
            MessageBorder.BeginAnimation(OpacityProperty, fade);
        }

        private void OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton != MouseButton.Left) return;
            machine.Touch(DateTime.UtcNow);
            dragging = true;
            dragMoved = false;
            dragDistance = 0;
            lastPointer = GetPointerPosition(e);
            Root.CaptureMouse();
            e.Handled = true;
        }

        private void OnMouseMove(object sender, MouseEventArgs e)
        {
            if (!dragging) return;
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
            ShowMessage("무슨 소리였지?", 2100);
            ScheduleBehavior(3000);
            e.Handled = true;
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
