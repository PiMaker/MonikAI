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
    /// Interaction logic for UnconspicousWindow.xaml
    /// </summary>
    public partial class UnconspicousWindow : Window
    {
        public UnconspicousWindow()
        {
            InitializeComponent();

            this.Loaded += async (sender, args) =>
            {
                await Task.Delay(5000);
                this.Dispatcher.Invoke(this.Close);
            };
        }
    }
}
