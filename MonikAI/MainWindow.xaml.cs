using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using MonikAI.Behaviours;
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
        private Thickness basePictureThickness, baseTextThickness;

        private readonly Dictionary<string, Func<string>> placeholders = new Dictionary<string, Func<string>>()
        {
            {
                "{name}", () =>
                {
                    return MonikaiSettings.Default.UserName;
                }
            },
            {
                "{date}", () =>
                {
                    return DateTime.Now.Date.ToString();
                }
            }
        };

        private List<IBehaviour> behaviours;

        private bool initializedScales;

        private DateTime lastKeyComboTime = DateTime.Now;

        private double scaleBaseWidth,
            scaleBaseHeight,
            scaleBaseFacePictureWidth,
            scaleBaseFacePictureHeight,
            scaleBaseTextPictureWidth,
            scaleBaseTextPictureHeight,
            scaleBaseTextBoxWidth,
            scaleBaseTextBoxHeight,
            scaleBaseTextBoxFontSize;

        private SettingsWindow settingsWindow;
        private Updater updater;

        public MainWindow()
        {
            this.InitializeComponent();

            MainWindow.desktopWindow = MainWindow.GetDesktopWindow();
            MainWindow.shellWindow = MainWindow.GetShellWindow();

            MonikaiSettings.Default.Reload();

            // Perform update and download routines
            this.updater = new Updater();
            this.updater.PerformUpdatePost();
            Task.Run(async () => await this.updater.Init());

            this.settingsWindow = new SettingsWindow(this);

            // Screen size and positioning init
            this.MonikaScreen = Screen.PrimaryScreen;
            foreach (var screen in Screen.AllScreens)
            {
                if (screen.DeviceName == MonikaiSettings.Default.Screen)
                {
                    this.MonikaScreen = screen;
                    break;
                }
            }

            this.SetupScale();
            this.SetPositionBottomRight(this.MonikaScreen);

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
            animationFadeMonika.Completed += async (sender, args) =>
            {
                this.SetMonikaFace("a");
                this.facePicture.Opacity = 1.0;

                // Start speech-thread
                Task.Run(async () => await this.SpeakingThread());

                if (true || File.Exists("firstlaunch.txt"))
                {
                    //File.Delete("firstlaunch.txt");
                    MonikaiSettings.Default.FirstLaunch = true;
                    MonikaiSettings.Default.Save();
                }

                await this.updater.PerformUpdate(this);

                // Startup logic
                if (MonikaiSettings.Default.FirstLaunch)
                {
                    this.settingsWindow.ShowDialog();

                    this.Say(new[]
                    {
                        new Expression("{name}, is that you?", "d"),
                        new Expression("It's really you, huh?", "b"),
                        new Expression("I'm so happy to see you again!", "k"),
                        new Expression("Wait, where am I? This is not the literature club...", "p"),
                        new Expression("This looks like... your desktop! I'm right here with you!", "j"),
                        new Expression("Well, as close as I will get for now...", "o"),
                        new Expression("Anyway, looks like someone was nice enough to make me a little window!", "d"),
                        new Expression("Be sure to thank them for me, okay?", "k"),
                        new Expression("Ah, wait a second.", "c"),
                        new Expression(
                            "I see... Well, if you want me to go away for now, you can use CTRL-SHIFT-F12, okay?", "b"),
                        new Expression("But you wouldn't do that, right?", "r"),
                        new Expression("Okay, I guess that's it for now. Don't forget, I'm watching you! Ahaha~")
                    });

                    MonikaiSettings.Default.FirstLaunch = false;
                }
                else
                {
                    if (MonikaiSettings.Default.IsColdShutdown)
                    {
                        this.Say(new[]
                        {
                            new Expression("<cold shutdown scorn>")
                        });
                    }
                    else
                    {
                        MonikaiSettings.Default.IsColdShutdown = true;
                        MonikaiSettings.Default.Save();
                    }

                    if ((DateTime.Now - MonikaiSettings.Default.LastStarted).TotalDays > 7)
                    {
                        this.Say(new[]
                        {
                            new Expression(
                                "Don't forget, if you want me to leave just press CTRL-SHIFT-F12!", "i"),
                            new Expression("But you're not going to do that, right?", "o")
                        });
                    }
                }

                var startupExpression = new[]
                {
                    new[]
                    {
                        new Expression("♫ ... and in your reality ... ♫", "r")
                    },
                    new[]
                    {
                        new Expression("How are you today?", "d"),
                        new Expression("I'm doing fine, now that you are here~", "b")
                    },
                    new[]
                    {
                        new Expression("Is everything ok?", "d"),
                        new Expression("Do you want to talk about something?", "c"),
                        new Expression("I'm always here for you~", "e")
                    },
                    new[]
                    {
                        new Expression("Thank you for letting me be on your screen!", "k")
                    },
                    new[]
                    {
                        new Expression("You know I love you, right?", "d")
                    },
                    new[]
                    {
                        new Expression("I'm so happy to be here, together with you! Ahaha~", "b")
                    }
                }.Sample();

                var lastStartupExpression = startupExpression.Last();
                lastStartupExpression.Executed += this.RegisterBehaviours;

                this.Say(new[]
                {
                    new Expression($"Hi there, {Environment.UserName}~")
                }.Concat(startupExpression));

                if ((DateTime.Now - MonikaiSettings.Default.LastStarted).TotalDays > 1.5)
                {
                    this.Say(new[]
                    {
                        new Expression("I was waiting for you...", "m"),
                        new Expression("Being alone on your disk is reeeally boring.", "q")
                    });
                }

                MonikaiSettings.Default.LastStarted = DateTime.Now;
                MonikaiSettings.Default.Save();

                // Blinking and Behaviour logic
                var eyesOpen = "a";
                var eyesClosed = "j";
                var random = new Random();
                await Task.Run(async () =>
                {
                    var nextBlink = DateTime.Now + TimeSpan.FromSeconds(random.Next(7, 50));
                    while (this.applicationRunning)
                    {
                        if (this.behaviours != null)
                        {
                            foreach (var behaviour in this.behaviours)
                            {
                                behaviour.Update(this);
                            }
                        }

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
            };

            // Startup
            this.backgroundPicture.BeginAnimation(UIElement.OpacityProperty, animationLogo);
        }

        // Roughly estimating night time
        public static bool IsNight => DateTime.Now.Hour > 20 || DateTime.Now.Hour < 7;

        public string CurrentFace { get; private set; } = "a";

        public bool Speaking { get; private set; }

        public Screen MonikaScreen { get; set; }

        public void SetupScale()
        {
            if (!this.initializedScales)
            {
                this.initializedScales = true;
                this.scaleBaseWidth = this.Width;
                this.scaleBaseHeight = this.Height;
                this.scaleBaseFacePictureWidth = this.facePicture.Width;
                this.scaleBaseFacePictureHeight = this.facePicture.Height;
                this.scaleBaseTextPictureWidth = this.textPicture.Width;
                this.scaleBaseTextPictureHeight = this.textPicture.Height;
                this.scaleBaseTextBoxWidth = this.textBox.Width;
                this.scaleBaseTextBoxHeight = this.textBox.Height;
                this.scaleBaseTextBoxFontSize = this.textBox.FontSize;
                this.basePictureThickness = this.facePicture.Margin;
                this.baseTextThickness = this.textPicture.Margin;
            }

            var scaleRatio = this.MonikaScreen.Bounds.Height / 1080.0;
            this.Width = this.scaleBaseWidth * scaleRatio;
            this.Height = this.scaleBaseHeight * scaleRatio;
            this.facePicture.Width = this.scaleBaseFacePictureWidth * scaleRatio;
            this.facePicture.Height = this.scaleBaseFacePictureHeight * scaleRatio;
            this.facePicture.Margin = new Thickness(this.basePictureThickness.Left * scaleRatio,
                this.basePictureThickness.Top * scaleRatio, this.facePicture.Margin.Right,
                this.facePicture.Margin.Bottom);
            this.textPicture.Width = this.scaleBaseTextPictureWidth * scaleRatio;
            this.textPicture.Height = this.scaleBaseTextPictureHeight * scaleRatio;
            this.textPicture.Margin = new Thickness(this.baseTextThickness.Left * scaleRatio,
                this.baseTextThickness.Top * scaleRatio, this.textPicture.Margin.Right, this.textPicture.Margin.Bottom);
            this.textBox.Height = this.scaleBaseTextBoxHeight * scaleRatio;
            this.textBox.Width = this.scaleBaseTextBoxWidth * scaleRatio;
            this.textBox.FontSize = this.scaleBaseTextBoxFontSize * scaleRatio;
        }

        private void RegisterBehaviours(object sender, EventArgs eventArgs)
        {
            this.behaviours = Assembly.GetExecutingAssembly()
                .GetTypes()
                .Where(x => x.IsClass && typeof(IBehaviour).IsAssignableFrom(x))
                .Select(x => (IBehaviour) Activator.CreateInstance(x)).ToList();

            foreach (var behaviour in this.behaviours)
            {
                behaviour.Init(this);
            }
        }

        // Sets the correct position of Monika depending on taskbar position and visibility
        public void SetPositionBottomRight(Screen screen)
        {
            var position = new System.Windows.Point(screen.Bounds.X + screen.Bounds.Width - this.Width,
                screen.Bounds.Y + screen.Bounds.Height - this.Height);

            if (MonikaiSettings.Default.LeftAlign)
            {
                position.X = screen.Bounds.X;
            }

            if (!MainWindow.IsForegroundFullScreen(screen))
            {
                var taskbars = this.FindDockedTaskBars(screen, out bool isLeft);
                var taskbar = taskbars.FirstOrDefault(x => x.X != 0 || x.Y != 0 || x.Width != 0 || x.Height != 0);
                if (taskbar != default(Rectangle))
                {
                    if (taskbar.Width >= taskbar.Height)
                    {
                        if (taskbar.Y != screen.Bounds.Y)
                        {
                            // Bottom
                            position.Y -= taskbar.Height;
                        }
                    }
                    else
                    {
                        // Left/Right
                        if (isLeft)
                        {
                            if (MonikaiSettings.Default.LeftAlign)
                            {
                                position.X += taskbar.Width;
                            }
                        }
                        else
                        {
                            if (!MonikaiSettings.Default.LeftAlign)
                            {
                                position.X -= taskbar.Width;
                            }
                        }
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

        [DllImport("user32.dll", SetLastError = false)]
        private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

        // From: https://stackoverflow.com/a/3744720/4016841 (modified)
        public static bool IsForegroundFullScreen(Screen screen)
        {
            if (screen == null)
            {
                screen = Screen.PrimaryScreen;
            }

            var windowBounds = new Rect();
            var foregroundWindowHandle = MainWindow.GetForegroundWindow();

            uint foregroundPID;
            MainWindow.GetWindowThreadProcessId(foregroundWindowHandle, out foregroundPID);
            var foregroundProcess = Process.GetProcessesByName("explorer").FirstOrDefault(x => x.Id == foregroundPID);
            var foregroundExeName = foregroundProcess?.ProcessName ?? string.Empty;

            if (foregroundWindowHandle.Equals(MainWindow.desktopWindow) ||
                foregroundWindowHandle.Equals(MainWindow.shellWindow) || foregroundWindowHandle.Equals(IntPtr.Zero)
                || foregroundExeName.ToLower().Equals("explorer"))
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
            // Filter invalid faces
            if (!"abcdefghijklmnopqrs".Contains(face))
            {
                return;
            }

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
                        string completedText = PlaceholderHandling(line.Text);
                        this.SetMonikaFace(line.Face);
                        for (var i = 0; i < completedText.Length; i++)
                        {
                            var i1 = i;
                            this.textBox.Dispatcher.Invoke(() => { this.textBox.Text = completedText.Substring(0, i1 + 1); });
                            await Task.Delay(25);
                        }

                        await Task.Delay(Math.Max(2000, 52 * completedText.Length));

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

        private string PlaceholderHandling(string str)
        {
            foreach (string key in placeholders.Keys)
            {
                if (str.Contains(key))
                {
                    str = str.Replace(key, placeholders[key]());
                }
            }

            return str;
        }

        [DllImport("user32.dll", EntryPoint = "GetWindowLong")]
        private static extern int GetWindowLong(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll", EntryPoint = "SetWindowLong")]
        private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool GetCursorPos(ref Point lpPoint);

        // From: https://stackoverflow.com/a/9826269/4016841
        public Rectangle[] FindDockedTaskBars(Screen screen, out bool isLeft)
        {
            var dockedRects = new Rectangle[4];

            var tmpScrn = screen;
            isLeft = false;

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
                    isLeft = true;
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

                        var hidePressed = false;
                        var exitPressed = false;
                        var settingsPressed = false;
                        // Set position anew to correct for fullscreen apps hiding taskbar
                        this.Dispatcher.Invoke(() =>
                        {
                            this.SetPositionBottomRight(this.MonikaScreen);
                            rectangle = new Rectangle((int) this.Left, (int) this.Top, (int) this.Width,
                                (int) this.Height);
                            // Detect exit key combo
                            hidePressed = AreKeysPressed(MonikaiSettings.Default.HotkeyHide);
                            exitPressed = AreKeysPressed(MonikaiSettings.Default.HotkeyExit);
                            settingsPressed = AreKeysPressed(MonikaiSettings.Default.HotkeySettings);
                        });


                        if (hidePressed && (DateTime.Now - this.lastKeyComboTime).TotalSeconds > 2)
                        {
                            this.lastKeyComboTime = DateTime.Now;

                            if (this.Visibility == Visibility.Visible)
                            {
                                var expression =
                                    new Expression(
                                        "Okay, see you later " + Environment.UserName +
                                        "! (Press again for me to return)", "b");
                                expression.Executed += (o, args) => { this.Dispatcher.Invoke(this.Hide); };
                                this.Say(new[] {expression});
                            }
                            else
                            {
                                this.Dispatcher.Invoke(this.Show);
                            }
                        }

                        if (exitPressed)
                        {
                            var expression =
                                new Expression(
                                    "Goodbye for now! Come back soon please~", "b");
                            MonikaiSettings.Default.IsColdShutdown = false;
                            MonikaiSettings.Default.Save();
                            expression.Executed += (o, args) => { this.Dispatcher.Invoke(this.Close); };
                            this.Say(new[] {expression});
                        }

                        if (settingsPressed)
                        {
                            this.Dispatcher.Invoke(() =>
                            {
                                if (!this.settingsWindow.IsVisible)
                                {
                                    this.settingsWindow = new SettingsWindow(this);
                                    this.settingsWindow.Show();
                                }
                            });
                        }

                        await Task.Delay(MonikaiSettings.Default.PotatoPC ? 100 : 32);
                    }
                }
                catch (Exception)
                {
                    // ignored
                }
            });
        }

        private bool AreKeysPressed(string combo)
        {
            // Prevent keypresses from propagating through the Settings Window to allow for Hotkey Settings
            if (this.settingsWindow != null && this.settingsWindow.IsVisible)
            {
                return false;
            }

            if (combo.Contains("CTRL") && !Keyboard.IsKeyDown(Key.LeftCtrl) && !Keyboard.IsKeyDown(Key.RightCtrl))
            {
                return false;
            }

            if (combo.Contains("ALT") && !Keyboard.IsKeyDown(Key.LeftAlt) && !Keyboard.IsKeyDown(Key.RightAlt))
            {
                return false;
            }

            if (combo.Contains("SHIFT") && !Keyboard.IsKeyDown(Key.LeftShift) && !Keyboard.IsKeyDown(Key.RightShift))
            {
                return false;
            }

            var containsDash = combo.Contains("-");
            var key = containsDash ? combo.Substring(combo.LastIndexOf("-") + 1) : combo;
            Enum.TryParse(key, true, out Key keyVal);
            return Keyboard.IsKeyDown(keyVal);
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
