using MahApps.Metro.Controls;
using CadProjectorSDK;
using CadProjectorSDK.Device;
using CadProjectorSDK.Setting;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace CadProjectorViewer.DeviceManager
{
    /// <summary>
    /// Логика взаимодействия для LaserMeter.xaml
    /// </summary>
    public partial class LaserMeterWindows : Window
    {
        private IPAddress _address;
        private VLTLaserMeters LMeter;

        public LaserMeterWindows(VLTLaserMeters vLTLaserMeters)
        {
            InitializeComponent();

            this.LMeter = vLTLaserMeters;

            if (this.LMeter.IP != null)
            {
                string[] iparr = this.LMeter.IP.ToString().Split('.');

                for (int k = 0; k < iparr.Length; k++)
                {
                    if (iparr[k].Length < 3)
                        for (int i = 0; i < 3 - iparr[k].Length; i++)
                            iparr[k] = iparr[k].Insert(0, " ");
                }

                //ipBox.Text = string.Concat(iparr);
            }

            TimerUpDn.DataContext = this.LMeter;
            TimerUpDn.SetBinding(MahApps.Metro.Controls.NumericUpDown.ValueProperty, "Interval");

            AutoPlayCheck.DataContext = this.LMeter;
            AutoPlayCheck.SetBinding(CheckBox.IsCheckedProperty, "AutoPlay");
        }
    

        private void DimBtn_Click(object sender, RoutedEventArgs e) => DimLbl.Content = this.LMeter.GetDim();


        private void CancelBtn_Click(object sender, RoutedEventArgs e) => this.Close();


        private void OkBtn_Click(object sender, RoutedEventArgs e)
        {
          /*   if (!ProjectorHub.CheckDeviceInHub(this.LMeter.IP))
            {
                ProjectorHub.LMeters.Add(this.LMeter);
                
            }
            else
            {

            }

            this.Close();*/
        }
    }
}
