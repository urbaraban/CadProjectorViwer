using System;
using System.Collections.Generic;
using System.Globalization;
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
using System.Windows.Navigation;
using System.Windows.Shapes;
using AppSt = CadProjectorViewer.Properties.Settings;

namespace CadProjectorViewer.Panels.RightPanel.Configuration
{
    /// <summary>
    /// Логика взаимодействия для AppDefaultSettingPanel.xaml
    /// </summary>
    public partial class AppDefaultSettingPanel : UserControl
    {
        public AppDefaultSettingPanel()
        {
            InitializeComponent();
        }

        private void AttachRadioButton_Checked(object sender, RoutedEventArgs e)
        {
            if (sender is RadioButton radioButton && radioButton.DataContext != null)
            {
                AppSt.Default.stg_default_position = radioButton.DataContext.ToString();
            }
        }
    }

    public class AppSettingSave : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return (bool)value == true ? Brushes.YellowGreen : Brushes.Yellow;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value;
        }
    }
}
