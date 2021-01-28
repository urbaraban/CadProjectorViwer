using MahApps.Metro.Controls;
using MonchaSDK;
using MonchaSDK.Device;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace MonchaCadViewer.DeviceManager
{
    /// <summary>
    /// Логика взаимодействия для LaserMeter.xaml
    /// </summary>
    public partial class LaserMeterWindows : Window
    {
        private IPAddress _address;
        private VLTLaserMeters _lasermeter;

        public LaserMeterWindows(VLTLaserMeters laserMeters)
        {
            InitializeComponent();

            this._lasermeter = laserMeters;

            if (this._lasermeter.IP != null)
            {
                string[] iparr = this._lasermeter.IP.ToString().Split('.');

                for (int k = 0; k < iparr.Length; k++)
                {
                    if (iparr[k].Length < 3)
                        for (int i = 0; i < 3 - iparr[k].Length; i++)
                            iparr[k] = iparr[k].Insert(0, " ");
                }

                ipBox.Text = string.Concat(iparr);
            }

            TimerUpDn.DataContext = this._lasermeter;
            TimerUpDn.SetBinding(MahApps.Metro.Controls.NumericUpDown.ValueProperty, "Interval");

            DistanceUpDn.DataContext = this._lasermeter;
            DistanceUpDn.SetBinding(MahApps.Metro.Controls.NumericUpDown.ValueProperty, "Distance");

            AutoPlayCheck.DataContext = this._lasermeter;
            AutoPlayCheck.SetBinding(CheckBox.IsCheckedProperty, "AutoPlay");
        }
    

        private void DimBtn_Click(object sender, RoutedEventArgs e)
        {
            DimLbl.Content = this._lasermeter.GetDim();
        }

        private void ipBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            if (IPAddress.TryParse(ipBox.Text.Replace("_", string.Empty), out this._address))
            {
                if (VLTLaserMeters.CheckLaser(this._address))
                {
                    DimBtn.IsEnabled = true;
                    DimBtn.Background = Brushes.YellowGreen;
                    this._lasermeter.IP = this._address;
                }
                else
                {
                    DimBtn.IsEnabled = false;
                    DimBtn.Background = Brushes.White;
                }

            }
        }

        private void SetBtn_Click(object sender, RoutedEventArgs e)
        {
            DistanceUpDn.Value = this._lasermeter.RealDistance;
        }

        private void CancelBtn_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void OkBtn_Click(object sender, RoutedEventArgs e)
        {
            if (!MonchaHub.CheckDeviceInHub(this._lasermeter.IP))
            {
                MonchaHub.LMeters.Add(this._lasermeter);
                
            }
            else
            {

            }

            this.Close();
        }
    }
}
