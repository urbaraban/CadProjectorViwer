using CadProjectorViewer.Panels.DevicePanel;
using CadProjectorSDK;
using CadProjectorSDK.Device;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using CadProjectorSDK.Device.Controllers;
using CadProjectorViewer.Dialogs;

namespace CadProjectorViewer.Panels
{
    /// <summary>
    /// Логика взаимодействия для DeviceTab.xaml
    /// </summary>
    public partial class DeviceTab : UserControl
    {
        private LProjector device => (LProjector)DeviceCombo.SelectedItem;

        public DeviceTab()
        {
            InitializeComponent();
        }

        private void LMeter_ChangeDimention(object sender, double e)
        {
           /* DistanceLabel.Invoke(() => {
                DistanceLabel.Content = e;
            });*/
        }

        private void LaserMeterToggle_Toggled(object sender, RoutedEventArgs e)
        {
            if (LaserMetersCombo.SelectedItem is VLTLaserMeters laserMeters)
            {
                laserMeters.Turn(LaserMeterToggle.IsOn);
            }
        }

        private void ScanRateRealSlider_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is Slider slider)
            {
                Point point = Mouse.GetPosition(slider);
                slider.Value = slider.Minimum + (point.X / slider.ActualWidth) * (slider.Maximum - slider.Minimum);
            }
        }

        private void DeviceSettingBtn_Click(object sender, RoutedEventArgs e)
        {
            if (DeviceCombo.SelectedItem != null)
            {

            }
        }

        private void EllipseSettingBtn_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button)
            {

            }
        }


        private void MeshSettingBtn_Click(object sender, RoutedEventArgs e)
        {

        }

        private void MeshListBtn_Click(object sender, RoutedEventArgs e)
        {

        }
    }
}
