using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;

namespace MonikAI
{
    /// <summary>
    ///     Interaction logic for SettingsWindow.xaml
    /// </summary>
    public partial class SettingsWindow : Window
    {
        private readonly MainWindow mainWindow;

        public SettingsWindow(MainWindow mainWindow)
        {
            this.mainWindow = mainWindow;
            this.InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // Settings window initialization code
            this.textBoxName.Text = string.IsNullOrWhiteSpace(MonikaiSettings.Default.UserName)
                ? Environment.UserName
                : MonikaiSettings.Default.UserName;
            this.checkBoxPotatoPC.IsChecked = MonikaiSettings.Default.PotatoPC;
            this.checkBoxAutoUpdate.IsChecked = MonikaiSettings.Default.AutoUpdate;

            var index = 0;
            foreach (var screen in Screen.AllScreens)
            {
                this.comboBoxScreen.Items.Add($"{screen.DeviceName} ({screen.Bounds.Width}x{screen.Bounds.Height})");

                if ((string.IsNullOrWhiteSpace(MonikaiSettings.Default.Screen) && screen.Primary) || (screen.DeviceName == MonikaiSettings.Default.Screen))
                {
                    this.comboBoxScreen.SelectedIndex = index;
                }
                index++;
            }
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
            // Not needed but left here for whatever reason you can come up with yourself
            // I'm just a comment, not someone to give meaning to life, you know?
            //this.mainWindow.SetPositionBottomRight(this.mainWindow.MonikaScreen);
        }

        private void buttonOk_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            MonikaiSettings.Default.AutoUpdate = this.checkBoxAutoUpdate.IsChecked.GetValueOrDefault(true);
            MonikaiSettings.Default.PotatoPC = this.checkBoxPotatoPC.IsChecked.GetValueOrDefault(false);
            MonikaiSettings.Default.UserName = this.textBoxName.Text;
            MonikaiSettings.Default.Screen = Screen.AllScreens.First(x => this.comboBoxScreen.SelectedItem.ToString().Contains(x.DeviceName)).DeviceName;

            MonikaiSettings.Default.Save();
        }
    }
}