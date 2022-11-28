using CadProjectorViewer.EthernetServer;
using CadProjectorViewer.EthernetServer.Servers;
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

namespace CadProjectorViewer.Panels.RightPanel
{
    /// <summary>
    /// Логика взаимодействия для HubPage.xaml
    /// </summary>
    public partial class HubPage : Page
    {
        public HubPage()
        {
            InitializeComponent();
        }
    }

    public class GetQrConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is ToCutServerObject serverObject)
            {
                return TCPTools.GetQR(serverObject.IpAdress, serverObject.Port);
            }
            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return null;
        }
    }
}
