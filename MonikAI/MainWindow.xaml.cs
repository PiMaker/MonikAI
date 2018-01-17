using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using Point = System.Drawing.Point;

namespace MonikAI
{
    /// <summary>
    ///     Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private static IntPtr desktopWindow, shellWindow;
        private readonly BitmapImage backgroundDay;
        private readonly BitmapImage backgroundNight;

        private readonly Queue<IEnumerable<Expression>> saying = new Queue<IEnumerable<Expression>>();
        private bool applicationRunning = true;

        public MainWindow()
        {
            this.InitializeComponent();

            MainWindow.desktopWindow = MainWindow.GetDesktopWindow();
            MainWindow.shellWindow = MainWindow.GetShellWindow();

            var scaleRatio = Screen.PrimaryScreen.Bounds.Height / 1080.0;
            //scaleRatio = 1;
            this.Width *= scaleRatio;
            this.Height *= scaleRatio;
            this.facePicture.Width *= scaleRatio;
            this.facePicture.Height *= scaleRatio;
            this.facePicture.Margin = new Thickness(this.facePicture.Margin.Left * scaleRatio,
                this.facePicture.Margin.Top * scaleRatio, this.facePicture.Margin.Right, this.facePicture.Margin.Bottom);
            this.textPicture.Width *= scaleRatio;
            this.textPicture.Height *= scaleRatio;
            this.textPicture.Margin = new Thickness(this.textPicture.Margin.Left * scaleRatio,
                this.textPicture.Margin.Top * scaleRatio, this.textPicture.Margin.Right, this.textPicture.Margin.Bottom);
            this.textBox.Height *= scaleRatio;
            this.textBox.Width *= scaleRatio;
            this.textBox.FontSize *= scaleRatio;

            this.SetPositionBottomRight();

            // Init background images
            this.backgroundDay = new BitmapImage();
            this.backgroundDay.BeginInit();
            this.backgroundDay.UriSource = new Uri("pack://application:,,,/MonikAI;component/monika/1.png");
            this.backgroundDay.EndInit();

            this.backgroundNight = new BitmapImage();
            this.backgroundNight.BeginInit();
            this.backgroundNight.UriSource = new Uri("pack://application:,,,/MonikAI;component/monika/1-n.png");
            this.backgroundNight.EndInit();

            // Start animation
            var animationLogo = new DoubleAnimation(0.0, 1.0, new Duration(TimeSpan.FromSeconds(1.5)));
            animationLogo.AutoReverse = true;
            var animationFadeMonika = new DoubleAnimation(0.0, 1.0, new Duration(TimeSpan.FromSeconds(1.5)));
            animationFadeMonika.BeginTime = TimeSpan.FromSeconds(0.5);

            animationLogo.Completed += (sender, args) =>
            {
                var fadeImage = new BitmapImage();
                fadeImage.BeginInit();
                if (MainWindow.IsNight)
                {
                    fadeImage.UriSource = new Uri("pack://application:,,,/MonikAI;component/monika/1a-n.png");
                }
                else
                {
                    fadeImage.UriSource = new Uri("pack://application:,,,/MonikAI;component/monika/1a.png");
                }
                fadeImage.EndInit();
                this.backgroundPicture.Source = fadeImage;

                var clock = animationFadeMonika.CreateClock();
                this.backgroundPicture.ApplyAnimationClock(UIElement.OpacityProperty, clock);
            };

            // Blinking behaviour
            animationFadeMonika.Completed += (sender, args) =>
            {
                this.SetMonikaFace("a");
                this.facePicture.Opacity = 1.0;

                this.Say(new[] {new Expression($"Hi there, {Environment.UserName}~")});
                this.Say(new[]
                {
                    new Expression("I was waiting for you...", "m"),
                    new Expression("Being alone on your disk is reeeally boring.", "q")
                });
                this.Say(new[]
                {
                    new Expression("By the way, if you want me to leave you alone for now just press CTRL-SHIFT-F12!",
                        "i"),
                    new Expression("But you're not going to do that, right? ~", "o")
                });

                // Blinking logic
                var eyesOpen = "a";
                var eyesClosed = "j";
                var random = new Random();
                Task.Run(async () =>
                {
                    var nextBlink = DateTime.Now + TimeSpan.FromSeconds(random.Next(7, 50));
                    while (this.applicationRunning)
                    {
                        if (DateTime.Now >= nextBlink)
                        {
                            // Check if currently speaking, only blink if not in dialog
                            if (!this.Speaking)
                            {
                                this.SetMonikaFace(eyesClosed);
                                await Task.Delay(100);
                                this.SetMonikaFace(eyesOpen);
                            }

                            nextBlink = DateTime.Now + TimeSpan.FromSeconds(random.Next(7, 50));
                        }

                        await Task.Delay(250);
                    }
                });

                // Start speech-thread
                Task.Run(this.SpeakingThread);
            };

            // Startup
            this.backgroundPicture.BeginAnimation(UIElement.OpacityProperty, animationLogo);
        }

        // Roughly estimating night time
        public static bool IsNight => DateTime.Now.Hour > 20 || DateTime.Now.Hour < 7;

        public string CurrentFace { get; private set; } = "a";

        public bool Speaking { get; private set; }

        // Sets the correct position of Monika depending on taskbar position and visibility
        private void SetPositionBottomRight()
        {
            var position = new System.Windows.Point(Screen.PrimaryScreen.Bounds.Width - this.Width,
                Screen.PrimaryScreen.Bounds.Height - this.Height);

            if (!MainWindow.IsForegroundFullScreen())
            {
                var taskbars = this.FindDockedTaskBars();
                var taskbar = taskbars.FirstOrDefault(x => x.X != 0 || x.Y != 0 || x.Width != 0 || x.Height != 0);
                if (taskbar != default(Rectangle))
                {
                    if (taskbar.X == 0 && taskbar.Y != 0)
                    {
                        // Bottom
                        position.Y -= taskbar.Height;
                    }
                    else if (taskbar.X != 0 && taskbar.Y == 0)
                    {
                        // Right
                        position.X -= taskbar.Width;
                    }
                }
            }

            this.Top = position.Y;
            this.Left = position.X;
        }

        [DllImport("user32.dll")]
        private static extern bool GetWindowRect(HandleRef hWnd, [In] [Out] ref Rect rect);

        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll", SetLastError = false)]
        private static extern IntPtr GetDesktopWindow();

        [DllImport("user32.dll", SetLastError = false)]
        private static extern IntPtr GetShellWindow();

        // From: https://stackoverflow.com/a/3744720/4016841 (modified)
        public static bool IsForegroundFullScreen(Screen screen = null)
        {
            if (screen == null)
            {
                screen = Screen.PrimaryScreen;
            }

            var windowBounds = new Rect();
            var foregroundWindowHandle = MainWindow.GetForegroundWindow();

            if (foregroundWindowHandle.Equals(MainWindow.desktopWindow) ||
                foregroundWindowHandle.Equals(MainWindow.shellWindow) || foregroundWindowHandle.Equals(IntPtr.Zero))
            {
                return false;
            }

            MainWindow.GetWindowRect(new HandleRef(null, foregroundWindowHandle), ref windowBounds);
            return
                new Rectangle(windowBounds.left, windowBounds.top, windowBounds.right - windowBounds.left,
                    windowBounds.bottom - windowBounds.top).Contains(
                    screen.Bounds);
        }

        public void SetMonikaFace(string face)
        {
            this.CurrentFace = face;
            this.Dispatcher.Invoke(() =>
            {
                if (MainWindow.IsNight)
                {
                    face += "-n";
                    this.backgroundPicture.Source = this.backgroundNight;
                }
                else
                {
                    this.backgroundPicture.Source = this.backgroundDay;
                }

                face += ".png";

                var faceImg = new BitmapImage();
                faceImg.BeginInit();
                faceImg.UriSource = new Uri("pack://application:,,,/MonikAI;component/monika/" + face);
                faceImg.EndInit();

                this.facePicture.Source = faceImg;
            });
        }

        public void Say(IEnumerable<Expression> text)
        {
            this.saying.Enqueue(text);
        }

        private async Task SpeakingThread()
        {
            while (this.applicationRunning)
            {
                if (this.saying.Count == 0)
                {
                    await Task.Delay(250);
                }
                else
                {
                    // Begin speech
                    var done = false;
                    this.Speaking = true;
                    this.Dispatcher.Invoke(() =>
                    {
                        var fadeIn = new DoubleAnimation(0.0, 1.0, new Duration(TimeSpan.FromSeconds(0.5)));
                        fadeIn.Completed += (sender, args) => done = true;
                        var clock = fadeIn.CreateClock();
                        this.textPicture.ApplyAnimationClock(UIElement.OpacityProperty, clock);
                        this.textBox.ApplyAnimationClock(UIElement.OpacityProperty, clock);
                    });

                    // Await fade in
                    while (!done)
                    {
                        await Task.Delay(5);
                    }

                    // Speak
                    var text = this.saying.Dequeue();
                    foreach (var line in text)
                    {
                        this.SetMonikaFace(line.Face);
                        for (var i = 0; i < line.Text.Length; i++)
                        {
                            var i1 = i;
                            this.textBox.Dispatcher.Invoke(() => { this.textBox.Text = line.Text.Substring(0, i1 + 1); });
                            await Task.Delay(25);
                        }

                        await Task.Delay(3500);

                        line.OnExecuted();
                    }

                    // End speech
                    done = false;
                    this.Dispatcher.Invoke(() =>
                    {
                        var fadeOut = new DoubleAnimation(1.0, 0.0, new Duration(TimeSpan.FromSeconds(0.5)));
                        fadeOut.Completed += (sender, args) =>
                        {
                            this.textBox.Dispatcher.Invoke(() => this.textBox.Text = "");
                            done = true;
                        };
                        var clock = fadeOut.CreateClock();
                        this.textPicture.ApplyAnimationClock(UIElement.OpacityProperty, clock);
                        this.textBox.ApplyAnimationClock(UIElement.OpacityProperty, clock);
                    });

                    // Await fade out
                    while (!done)
                    {
                        await Task.Delay(5);
                    }

                    this.SetMonikaFace("a");

                    this.Speaking = false;

                    await Task.Delay(1500);
                }
            }
        }

        [DllImport("user32.dll", EntryPoint = "GetWindowLong")]
        private static extern int GetWindowLong(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll", EntryPoint = "SetWindowLong")]
        private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool GetCursorPos(ref Point lpPoint);

        // From: https://stackoverflow.com/a/9826269/4016841
        public Rectangle[] FindDockedTaskBars()
        {
            var dockedRects = new Rectangle[4];

            var tmpScrn = Screen.PrimaryScreen;

            var dockedRectCounter = 0;
            if (!tmpScrn.Bounds.Equals(tmpScrn.WorkingArea))
            {
                var leftDockedWidth = Math.Abs(Math.Abs(tmpScrn.Bounds.Left) - Math.Abs(tmpScrn.WorkingArea.Left));
                var topDockedHeight = Math.Abs(Math.Abs(tmpScrn.Bounds.Top) - Math.Abs(tmpScrn.WorkingArea.Top));
                var rightDockedWidth = tmpScrn.Bounds.Width - leftDockedWidth - tmpScrn.WorkingArea.Width;
                var bottomDockedHeight = tmpScrn.Bounds.Height - topDockedHeight - tmpScrn.WorkingArea.Height;

                if (leftDockedWidth > 0)
                {
                    dockedRects[dockedRectCounter].X = tmpScrn.Bounds.Left;
                    dockedRects[dockedRectCounter].Y = tmpScrn.Bounds.Top;
                    dockedRects[dockedRectCounter].Width = leftDockedWidth;
                    dockedRects[dockedRectCounter].Height = tmpScrn.Bounds.Height;
                    dockedRectCounter += 1;
                }

                if (rightDockedWidth > 0)
                {
                    dockedRects[dockedRectCounter].X = tmpScrn.WorkingArea.Right;
                    dockedRects[dockedRectCounter].Y = tmpScrn.Bounds.Top;
                    dockedRects[dockedRectCounter].Width = rightDockedWidth;
                    dockedRects[dockedRectCounter].Height = tmpScrn.Bounds.Height;
                    dockedRectCounter += 1;
                }
                if (topDockedHeight > 0)
                {
                    dockedRects[dockedRectCounter].X = tmpScrn.WorkingArea.Left;
                    dockedRects[dockedRectCounter].Y = tmpScrn.Bounds.Top;
                    dockedRects[dockedRectCounter].Width = tmpScrn.WorkingArea.Width;
                    dockedRects[dockedRectCounter].Height = topDockedHeight;
                    dockedRectCounter += 1;
                }
                if (bottomDockedHeight > 0)
                {
                    dockedRects[dockedRectCounter].X = tmpScrn.WorkingArea.Left;
                    dockedRects[dockedRectCounter].Y = tmpScrn.WorkingArea.Bottom;
                    dockedRects[dockedRectCounter].Width = tmpScrn.WorkingArea.Width;
                    dockedRects[dockedRectCounter].Height = bottomDockedHeight;
                    dockedRectCounter += 1;
                }
            }

            return dockedRects;
        }

        private void MainWindow_OnLoaded(object sender, RoutedEventArgs e)
        {
            var handle = new WindowInteropHelper(this).Handle;
            var initialStyle = MainWindow.GetWindowLong(handle, -20);
            MainWindow.SetWindowLong(handle, -20, initialStyle | 0x20 | 0x80000);

            Task.Run(async () =>
            {
                try
                {
                    var prev = new Point();

                    var rectangle = new Rectangle();
                    await this.Dispatcher.InvokeAsync(() =>
                    {
                        rectangle = new Rectangle((int) this.Left, (int) this.Top, (int) this.Width,
                            (int) this.Height);
                    });

                    while (this.applicationRunning)
                    {
                        var point = new Point();
                        MainWindow.GetCursorPos(ref point);

                        if (!point.Equals(prev))
                        {
                            prev = point;

                            var opacity = 1.0;
                            const double MIN_OP = 0.125;
                            const double FADE = 175;

                            if (rectangle.Contains(point))
                            {
                                opacity = MIN_OP;
                            }
                            else
                            {
                                if (point.Y <= rectangle.Bottom)
                                {
                                    if (point.Y >= rectangle.Y)
                                    {
                                        if (point.X < rectangle.X && rectangle.X - point.X < FADE)
                                        {
                                            opacity = MainWindow.Lerp(1.0, MIN_OP, (rectangle.X - point.X) / FADE);
                                        }
                                        else if (point.X > rectangle.Right && point.X - rectangle.Right < FADE)
                                        {
                                            opacity = MainWindow.Lerp(1.0, MIN_OP, (point.X - rectangle.Right) / FADE);
                                        }
                                    }
                                    else if (point.Y < rectangle.Y)
                                    {
                                        if (point.X >= rectangle.X && point.X <= rectangle.Right)
                                        {
                                            if (rectangle.Y - point.Y < FADE)
                                            {
                                                opacity = MainWindow.Lerp(1.0, MIN_OP, (rectangle.Y - point.Y) / FADE);
                                            }
                                        }
                                        else if (rectangle.X > point.X || rectangle.Right < point.X)
                                        {
                                            var distance =
                                                Math.Sqrt(
                                                    Math.Pow(
                                                        (point.X < rectangle.X ? rectangle.X : rectangle.Right) -
                                                        point.X, 2) +
                                                    Math.Pow(rectangle.Y - point.Y, 2));
                                            if (distance < FADE)
                                            {
                                                opacity = MainWindow.Lerp(1.0, MIN_OP, distance / FADE);
                                            }
                                        }
                                    }
                                }
                            }

                            this.Dispatcher.Invoke(() => { this.Opacity = opacity; });
                        }

                        // Set position anew to correct for fullscreen apps hiding taskbar
                        this.Dispatcher.Invoke(this.SetPositionBottomRight);

                        // Detect exit key combo
                        var keysPressed = false;
                        this.Dispatcher.Invoke(
                            () =>
                                keysPressed = (Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl)) &&
                                              (Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift)) &&
                                              Keyboard.IsKeyDown(Key.F12));
                        if (keysPressed)
                        {
                            var expression = new Expression("Okay, see you later " + Environment.UserName + "!", "b");
                            expression.Executed += (o, args) => { this.Dispatcher.Invoke(this.Close); };
                            this.Say(new[] {expression});
                            // Wait for exit
                            while (this.applicationRunning)
                            {
                                await Task.Delay(100);
                            }
                        }

                        await Task.Delay(32);
                    }
                }
                catch (Exception)
                {
                    // ignored
                }
            });
        }

        private static double Lerp(double firstFloat, double secondFloat, double by)
        {
            return firstFloat * by + secondFloat * (1 - by);
        }

        private void MainWindow_OnClosing(object sender, CancelEventArgs e)
        {
            this.applicationRunning = false;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct Rect
        {
            public readonly int left;
            public readonly int top;
            public readonly int right;
            public readonly int bottom;
        }
    }
}