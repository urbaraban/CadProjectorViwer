using MahApps.Metro.Controls;
using MonchaCadViewer.CanvasObj;
using MonchaCadViewer.Panels.DevicePanel;
using MonchaSDK;
using MonchaSDK.Device;
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
using System.Windows.Navigation;

using AppSt = MonchaCadViewer.Properties.Settings;

namespace MonchaCadViewer.Panels
{
    /// <summary>
    /// Логика взаимодействия для DeviceTab.xaml
    /// </summary>
    public partial class DeviceTab : UserControl
    {
        private MonchaDevice _device;
        public MonchaDevice Device { get => this._device; }

        public event EventHandler<MonchaDevice> DeviceChange;


        public DeviceTab()
        {
            InitializeComponent();

            DeviceCombo.DisplayMemberPath = "HWIdentifier";
            DeviceCombo.SelectedValuePath = "HWIdentifier";
            DeviceCombo.ItemsSource = MonchaHub.Devices;
            DeviceCombo.DataContext = MonchaHub.Devices;

            LaserMetersCombo.DisplayMemberPath = "HWIdentifier";
            LaserMetersCombo.SelectedValuePath = "HWIdentifier";
            LaserMetersCombo.ItemsSource = MonchaHub.LMeters;
            LaserMetersCombo.DataContext = MonchaHub.LMeters;

            this.DataContextChanged += DevicePanel_DataContextChanged;
        }


        private void DevicePanel_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (this.DataContext is MonchaDevice device)
            {
                this._device = device;
                DeviceCombo.SelectedItem = device;
                DeviceChange?.Invoke(this, this._device);
            }
        }


        /*private void BindingDeviceSetting(MonchaDevice monchaDevice)
        {

            DistanceUpDn.DataContext = monchaDevice.Size;
            DistanceUpDn.SetBinding(NumericUpDown.ValueProperty, "Z");
        }*/

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

        private void LaserMetersCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (DeviceCombo.SelectedItem is MonchaDevice monchaDevice)
            {
                //monchaDevice.ProjectionSetting.ReconnectLMeter(LaserMetersCombo.SelectedItem as VLTLaserMeters);

                DistanceUpDn.DataContext = monchaDevice.ProjectionSetting;
                DistanceUpDn.SetBinding(NumericUpDown.ValueProperty, "Distance");

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
            DeviceSettingDialog deviceSettingDialog = new DeviceSettingDialog((MonchaDevice)DeviceCombo.SelectedItem);
            deviceSettingDialog.Show();
        }


        private void MeshSettingBtn_Click(object sender, RoutedEventArgs e)
        {
            CreateGridWindow createGridWindow = new CreateGridWindow(Device, Device.SelectMesh);
            createGridWindow.Show();
        }

        private void MeshListBtn_Click(object sender, RoutedEventArgs e)
        {
            MeshesDialog meshesDialog = new MeshesDialog() { DataContext = Device };
            meshesDialog.Show();
        }
    }
}
