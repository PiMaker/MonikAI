using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace MonikAI
{
    /// <summary>
    /// Logique d'interaction pour ButtonWindow.xaml
    /// </summary>
    public partial class ButtonWindow : Window
    {
        private readonly MainWindow mainWindow;
        private SettingsWindow settingsWindow;

        private void Setting_Click(object sender, RoutedEventArgs e) //When Setiing Button is clicked
        {
            this.Close();
            mainWindow.Setting();
        }
        public ButtonWindow(MainWindow mainWindow)
        {
            this.mainWindow = mainWindow;
            this.InitializeComponent();
        }

        private void Exit_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
            mainWindow.Exit();
        }

        private void Hide_Click(object sender, RoutedEventArgs e)
        {
            if (mainWindow.Visibility == Visibility.Visible)
            {
                mainWindow.Hide();
            }
            else
            {
                mainWindow.Show();
            }
            this.Close();
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void ILuvU_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
            mainWindow.ILuvU();
        }
    }
}
