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
using Emgu.CV;
using Emgu.CV.Structure;
using Emgu.CV.VideoStab;
using Emgu.Util;

namespace CadProjectorViewer.Dialogs
{
    /// <summary>
    /// Логика взаимодействия для CameraShowDialog.xaml
    /// </summary>
    public partial class CameraShowDialog : System.Windows.Window
    {
        public Mat Frame 
        {
            get => frame;
            set
            {
                frame = value;
            }
        }
        public Mat frame = new Mat();

        private CaptureFrameSource capture { get; set; }

        public CameraShowDialog()
        {
            InitializeComponent();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            VideoCapture videoCapture = new VideoCapture(AdressBox.Text);
            if (videoCapture.IsOpened == true)
            {
                capture = new CaptureFrameSource(videoCapture);
                capture.NextFrame(Frame);
            }

        }
    }
}
