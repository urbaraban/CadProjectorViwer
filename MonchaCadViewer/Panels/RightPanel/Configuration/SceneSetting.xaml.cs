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

namespace CadProjectorViewer.Panels.RightPanel.Configuration
{
    /// <summary>
    /// Логика взаимодействия для SceneSetting.xaml
    /// </summary>
    public partial class SceneSetting : UserControl
    {
        public SceneSetting()
        {
            InitializeComponent();
        }
    }

    public class AttachConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return (string)value == (string)parameter;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return (bool)value == true ? parameter : string.Empty;
        }
    }
}
