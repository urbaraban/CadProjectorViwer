using RtspClientSharp;
using RtspClientSharp.RawFrames.Video;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
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

namespace CadProjectorViewer.Dialogs
{
    /// <summary>
    /// Логика взаимодействия для IpCamDialog.xaml
    /// </summary>
    public partial class IpCamDialog : Window
    {

        public IpCamDialog()
        {
            InitializeComponent();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            var serverUri = new Uri("rtsp://192.168.1.77:554/ucast/11");
            var credentials = new NetworkCredential("admin", "123456");
            var connectionParameters = new ConnectionParameters(serverUri, credentials);
            connectionParameters.RtpTransport = RtpTransportProtocol.TCP;

        }
    }
}
