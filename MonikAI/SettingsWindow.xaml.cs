using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using Microsoft.Win32.TaskScheduler;
using Newtonsoft.Json;
using Action = System.Action;
using MessageBox = System.Windows.MessageBox;
using Point = System.Drawing.Point;
using Task = System.Threading.Tasks.Task;

namespace MonikAI
{
    /// <summary>
    ///     Interaction logic for SettingsWindow.xaml
    /// </summary>
    public partial class SettingsWindow : Window
    {
        private const string AUTOSTART_TASK_NAME = "MonikAI_Startup";
        private readonly MainWindow mainWindow;

        private bool settingsLoaded;
        private ButtonWindow buttonWindow;

        public SettingsWindow(MainWindow mainWindow)
        {
            this.mainWindow = mainWindow;
            this.InitializeComponent();
        }

        public SettingsWindow(ButtonWindow buttonWindow)
        {
            this.buttonWindow = buttonWindow;
        }

        public bool IsPositioning { get; private set; }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            if (MonikaiSettings.Default.FirstLaunch)
            {
                MonikaiSettings.Default.AutoUpdate = true;
            }

            if (!string.IsNullOrWhiteSpace(MonikaiSettings.Default.LastUpdateConfig))
            {
                var localConfig =
                    JsonConvert.DeserializeObject<UpdateConfig>(MonikaiSettings.Default.LastUpdateConfig);
                this.textBlockVersion.Text = "Version p" + localConfig.ProgramVersion + "t" + localConfig.ResponsesVersion
#if DEBUG
                    + "d"
#endif
                    ;
            }

            // Settings window initialization code
            this.textBoxName.Text = string.IsNullOrWhiteSpace(MonikaiSettings.Default.UserName)
                ? Environment.UserName
                : MonikaiSettings.Default.UserName;

            this.checkBoxPotatoPC.IsChecked = MonikaiSettings.Default.PotatoPC;
            this.checkBoxAutoUpdate.IsChecked = MonikaiSettings.Default.AutoUpdate;

            this.sliderScale.Value = MonikaiSettings.Default.ScaleModifier;

            this.txtSettings.Text = MonikaiSettings.Default.HotkeySettings;
            this.txtExit.Text = MonikaiSettings.Default.HotkeyExit;
            this.txtHide.Text = MonikaiSettings.Default.HotkeyHide;

            this.comboBoxLanguage.Text = MonikaiSettings.Default.Language;

            if (MonikaiSettings.Default.LeftAlign)
            {
                this.radioLeft.IsChecked = true;
            }
            else
            {
                this.radioRight.IsChecked = true;
            }

            if (MonikaiSettings.Default.ManualPosition)
            {
                this.radioManual.IsChecked = true;
            }

            if (!string.IsNullOrEmpty(MonikaiSettings.Default.AutoStartTask))
            {
                this.buttonAutostart.Content = "Disable starting with Windows";
            }

            this.comboBoxNightMode.SelectedItem = MonikaiSettings.Default.DarkMode;

            var index = 0;
            this.comboBoxScreen.Items.Clear();
            foreach (var screen in Screen.AllScreens)
            {
                this.comboBoxScreen.Items.Add($"{screen.DeviceName} ({screen.Bounds.Width}x{screen.Bounds.Height})");

                if (string.IsNullOrWhiteSpace(MonikaiSettings.Default.Screen) && screen.Primary ||
                    screen.DeviceName == MonikaiSettings.Default.Screen)
                {
                    this.comboBoxScreen.SelectedIndex = index;
                }
                index++;
            }

            object selObj = null;
            foreach (string item in this.comboBoxIdle.Items)
            {
                if (item.ToLower() == MonikaiSettings.Default.IdleWait.ToLower())
                {
                    selObj = item;
                    break;
                }
            }

            // For some reason it's broken. Did they manually edit the settings? Lets force regular.
            if (selObj == null)
            {
                selObj = this.comboBoxIdle.Items[2];
                MonikaiSettings.Default.IdleWait = "Regular (120-300s)";
            }

            this.comboBoxIdle.SelectedItem = selObj;

            // Focus window
            this.Focus();
            this.Activate();

            this.settingsLoaded = true;
        }

        private void comboBoxScreen_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (this.comboBoxScreen.SelectedItem == null)
            {
                return;
            }

            this.mainWindow.MonikaScreen =
                Screen.AllScreens.First(x => this.comboBoxScreen.SelectedItem.ToString().Contains(x.DeviceName));
            this.mainWindow.SetupScale();
            this.mainWindow.SetPosition(this.mainWindow.MonikaScreen);
            this.mainWindow.SetupScale();
        }

        private void buttonOk_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            var restart = false;
            if ((string) this.comboBoxLanguage.SelectedItem != MonikaiSettings.Default.Language)
            {
                MessageBox.Show("Language settings have been changed. MonikAI will now restart.", "Note");
                restart = true;
            }

            MonikaiSettings.Default.AutoUpdate = this.checkBoxAutoUpdate.IsChecked.GetValueOrDefault(true);
            MonikaiSettings.Default.PotatoPC = this.checkBoxPotatoPC.IsChecked.GetValueOrDefault(false);
            MonikaiSettings.Default.DarkMode = (string)this.comboBoxNightMode.SelectedItem;
            MonikaiSettings.Default.UserName = this.textBoxName.Text;
            MonikaiSettings.Default.Language = (string) this.comboBoxLanguage.SelectedItem;
            if (this.comboBoxScreen.SelectedItem != null && Screen.AllScreens != null)
            {
                MonikaiSettings.Default.Screen =
                    Screen.AllScreens.First(x => this.comboBoxScreen.SelectedItem.ToString().Contains(x.DeviceName))
                        .DeviceName;
            }

            MonikaiSettings.Default.Save();

            if (restart)
            {
                Process.Start(Assembly.GetEntryAssembly().Location);

                MonikaiSettings.Default.IsColdShutdown = false;
                MonikaiSettings.Default.Save();
                Environment.Exit(0);
            }
        }

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool GetCursorPos(ref Point lpPoint);

        [DllImport("user32.dll")]
        public static extern short GetAsyncKeyState(ushort virtualKeyCode);

        private void radio_checked_changed(object sender, RoutedEventArgs e)
        {
            if (!this.settingsLoaded)
            {
                return;
            }

            MonikaiSettings.Default.LeftAlign = this.radioLeft.IsChecked.GetValueOrDefault(false);

            if (this.radioManual.IsChecked.GetValueOrDefault(false))
            {
                if (this.IsPositioning)
                {
                    return;
                }

                MonikaiSettings.Default.ManualPosition = true;
                MonikaiSettings.Default.ManualPositionX = 0;
                MonikaiSettings.Default.ManualPositionY = 0;
                this.IsPositioning = true;
                this.Dispatcher.Invoke(() => this.IsEnabled = false);

                MessageBox.Show(
                    "MonikAI will now follow your mouse cursor so you can position her wherever you want. Click the LEFT MOUSE BUTTON once you're satisfied with her position.",
                    "Manual Position");

                Task.Run(async () =>
                {
                    var mouseDown = SettingsWindow.GetAsyncKeyState(0x01) != 0;

                    do
                    {
                        var pos = Point.Empty;
                        SettingsWindow.GetCursorPos(ref pos);

                        MonikaiSettings.Default.ManualPositionX = pos.X;
                        MonikaiSettings.Default.ManualPositionY = pos.Y;

                        var prevMouseDown = mouseDown;
                        mouseDown = SettingsWindow.GetAsyncKeyState(0x01) != 0; // 0x01 is code for LEFT_MOUSE

                        if (mouseDown && !prevMouseDown)
                        {
                            break;
                        }

                        await Task.Delay(1);
                    } while (true);

                    this.Dispatcher.Invoke(() => this.IsEnabled = true);
                    this.IsPositioning = false;
                });
            }
            else
            {
                MonikaiSettings.Default.ManualPosition = false;
                this.mainWindow.SetupScale();
                this.mainWindow.SetPosition(this.mainWindow.MonikaScreen);
                this.mainWindow.SetupScale();
            }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            if (
                MessageBox.Show("Are you sure? This will reset all your settings.", "Confirm reset",
                    MessageBoxButton.OKCancel) == MessageBoxResult.OK)
            {
                MonikaiSettings.Default.Reset();
                this.Window_Loaded(this, null);
            }
        }

        // Settings hotkey
        private async void Button_Click_1(object sender, RoutedEventArgs e)
        {
            await this.HotkeySetTask(this.txtSettings,
                () => MonikaiSettings.Default.HotkeySettings = this.txtSettings.Text);
        }

        // Hide hotkey
        private async void Button_Click_2(object sender, RoutedEventArgs e)
        {
            await this.HotkeySetTask(this.txtHide, () => MonikaiSettings.Default.HotkeyHide = this.txtHide.Text);
        }

        // Exit hotkey
        private async void Button_Click_3(object sender, RoutedEventArgs e)
        {
            await this.HotkeySetTask(this.txtExit, () => MonikaiSettings.Default.HotkeyExit = this.txtExit.Text);
        }

        private async Task HotkeySetTask(TextBlock output, Action callback)
        {
            output.Dispatcher.Invoke(() => output.Text = "Press and HOLD any key combination");

            await this.WaitForKeyChange();

            var timer = DateTime.Now;
            var state = SettingsWindow.GetKeyboardState().ToList();
            var invalid = true;
            while ((DateTime.Now - timer).TotalSeconds < 0.75)
            {
                var newState = SettingsWindow.GetKeyboardState().ToList();

                var ctrlPressed = newState.Where(x => x.Item1 == "LeftCtrl" || x.Item1 == "RightCtrl").Any(x => x.Item2);
                var altPressed = newState.Where(x => x.Item1 == "LeftAlt" || x.Item1 == "RightAlt").Any(x => x.Item2);
                var shiftPressed =
                    newState.Where(x => x.Item1 == "LeftShift" || x.Item1 == "RightShift").Any(x => x.Item2);
                var otherKeysPressed =
                    newState.Where(
                        x =>
                            x.Item2 &&
                            !new[] {"LeftCtrl", "RightCtrl", "LeftAlt", "RightAlt", "LeftShift", "RightShift"}.Contains(
                                x.Item1)).ToList();
                invalid = otherKeysPressed.Count != 1;

                if (invalid || !state.SequenceEqual(newState))
                {
                    timer = DateTime.Now;
                }

                output.Dispatcher.Invoke(() =>
                {
                    if (invalid)
                    {
                        output.Text = "Invalid combination";
                    }
                    else
                    {
                        output.Text = otherKeysPressed.Single().Item1;
                        if (shiftPressed)
                        {
                            output.Text = "SHIFT-" + output.Text;
                        }
                        if (altPressed)
                        {
                            output.Text = "ALT-" + output.Text;
                        }
                        if (ctrlPressed)
                        {
                            output.Text = "CTRL-" + output.Text;
                        }
                    }
                });

                state = newState;

                await Task.Delay(10);
            }

            output.Dispatcher.Invoke(() => output.Foreground = Brushes.GreenYellow);
            await Task.Delay(500);
            output.Dispatcher.Invoke(() => output.Foreground = Brushes.Black);

            output.Dispatcher.Invoke(callback);
        }

        private async Task WaitForKeyChange()
        {
            var state = SettingsWindow.GetKeyboardState().ToList();
            while (state.SequenceEqual(SettingsWindow.GetKeyboardState()))
            {
                await Task.Delay(10);
            }
        }

        private static IEnumerable<Tuple<string, bool>> GetKeyboardState()
        {
            return Enum.GetNames(typeof(Key)).Select(x =>
            {
                var key = (Key) Enum.Parse(typeof(Key), x);
                return new Tuple<string, bool>(x, key != Key.None && Keyboard.IsKeyDown(key));
            });
        }

        /*
        // I disabled this because the workaround wasn't doing anything, but let's leave it in, maybe it will become useful at some point in the future
        private void Button_Click_4(object sender, RoutedEventArgs e)
        {
            // Set DPI awareness or something
            if (MonikaiSettings.Default.DpiWorkaround)
            {
                MessageBox.Show("Workaround has been disabled! MonikAI will now exit, please restart it manually.", "Workaround");
                MonikaiSettings.Default.DpiWorkaround = false;
                MonikaiSettings.Default.Save();
                Environment.Exit(0);
            }
            else
            {
                if (MessageBox.Show(
                        "If you don't see Monika on one of your screens right now, MonikAI can activate a workaround that *might* fix your issue - your milage may vary however. Do you want to try the fix?",
                        "Workaround", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                {
                    MonikaiSettings.Default.DpiWorkaround = true;
                    MonikaiSettings.Default.Save();
                    MessageBox.Show("Workaround enabled. MonikAI will now exit, please restart it manually.", "Workaround");
                    Environment.Exit(0);
                }
            }
        }*/

        private void sliderScale_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (!this.settingsLoaded)
            {
                return;
            }

            // Scale modifier
            MonikaiSettings.Default.ScaleModifier = this.sliderScale.Value;
            this.mainWindow.SetupScale();
        }

        private void comboBoxIdle_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (this.comboBoxIdle.SelectedItem == null)
            {
                return;
            }

            MonikaiSettings.Default.IdleWait = (string) this.comboBoxIdle.SelectedItem;
        }

        private void Button_Autostart_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(MonikaiSettings.Default.AutoStartTask))
            {
                this.buttonAutostart.Content = "Disable starting with Windows";
                // Enable autostart
                // NOTE: We use the task scheduler to circumnavigate the UAC dialog which would get really annoying over time
                using (var ts = new TaskService())
                {
                    var definition = ts.NewTask();
                    definition.RegistrationInfo.Description = "Automatically starts MonikAI at startup.";
                    // Add delay to compensate for taskbar weirdness, probably not a good idea but hey
                    definition.Triggers.Add(new LogonTrigger {Delay = TimeSpan.FromSeconds(2)});
                    definition.Actions.Add(new ExecAction(Assembly.GetEntryAssembly().Location));
                    definition.Principal.RunLevel = TaskRunLevel.Highest;
                    ts.RootFolder.RegisterTaskDefinition(SettingsWindow.AUTOSTART_TASK_NAME, definition);
                }

                MonikaiSettings.Default.AutoStartTask = SettingsWindow.AUTOSTART_TASK_NAME;
                MessageBox.Show(
                    "Autostart has been enabled! Note that it points to this very executable (this MonikAI.exe, probably), so if you move or delete it the autostart will also stop working until you re-enable it!",
                    "Note");
            }
            else
            {
                this.buttonAutostart.Content = "Start with Windows!";
                using (var ts = new TaskService())
                {
                    ts.RootFolder.DeleteTask(SettingsWindow.AUTOSTART_TASK_NAME, false);
                }

                MonikaiSettings.Default.AutoStartTask = string.Empty;
            }

            // We need to save here, otherwise the user might cancel the dialog without saving and we end up in an invalid state, out of sync with the task scheduler
            MonikaiSettings.Default.Save();
        }

        private void comboBoxNightMode_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            MonikaiSettings.Default.DarkMode = (string)this.comboBoxNightMode.SelectedItem;
            this.mainWindow.SetMonikaFace("a");
        }

        private void Button_Click_4(object sender, RoutedEventArgs e)
        { 
            this.mainWindow.ILuvU();
            this.Close();
        }
        private async void Button_Click_5(object sender, RoutedEventArgs e)
        {
            await this.HotkeySetTask(this.txtButton, () => MonikaiSettings.Default.HotkeyButton = this.txtButton.Text);
        }
    }
}