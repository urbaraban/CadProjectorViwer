using CadProjectorSDK.Tools;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Management;
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

namespace CadProjectorViewer
{
    /// <summary>
    /// Логика взаимодействия для RequestLicenseCode.xaml
    /// </summary>
    public partial class RequestLicenseCode : Window
    {
        LockKey LKey => (LockKey)this.DataContext;

        public RequestLicenseCode()
        {
            InitializeComponent();
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.NumPad0 && Keyboard.Modifiers == ModifierKeys.Control)
            {
                LKey.SetKey();
            }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
