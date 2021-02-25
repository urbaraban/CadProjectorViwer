using MahApps.Metro.Controls;
using MonchaCadViewer.CanvasObj;
using MonchaCadViewer.ToolsPanel.DevicePanel;
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

namespace MonchaCadViewer.ToolsPanel
{
    /// <summary>
    /// Логика взаимодействия для DeviceTab.xaml
    /// </summary>
    public partial class DeviceTab : UserControl
    {
        private MonchaDevice _device;
        public MonchaDevice Device { get => this._device; }

        public event EventHandler<MonchaDevice> DeviceChange;

        public event EventHandler NeedUpdate;

        public event EventHandler<List<FrameworkElement>> DrawObjects;

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

        private void DeviceCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (DeviceCombo.SelectedItem is MonchaDevice tempdevice)
            {
                BindingDeviceSetting(tempdevice);
            }
        }

        private void BindingDeviceSetting(MonchaDevice monchaDevice)
        {
            //LaserMeter
            if (monchaDevice.ProjectionSetting.LMeter != null)
            {
                LaserMetersCombo.SelectedValue = monchaDevice.ProjectionSetting.LMeter.HWIdentifier;

                LaserMeterToggle.DataContext = monchaDevice.ProjectionSetting.LMeter;
                LaserMeterToggle.SetBinding(ToggleSwitch.IsOnProperty, "IsTurn");

                monchaDevice.ProjectionSetting.LMeter.ChangeDimention += LMeter_ChangeDimention;
            }

            CommonSettingToggle.DataContext = monchaDevice;
            CommonSettingToggle.SetBinding(ToggleSwitch.IsOnProperty, "OwnedSetting");

            DistanceUpDn.DataContext = monchaDevice.ProjectionSetting;
            DistanceUpDn.SetBinding(NumericUpDown.ValueProperty, "Distance");

            //Device
            ScanRateRealSlider.Maximum = 40000;
            ScanRateRealSlider.Minimum = 500;
            ScanRateRealSlider.DataContext = monchaDevice;
            ScanRateRealSlider.SetBinding(Slider.ValueProperty, "ScanRateReal");

            ScanRateCalc.Maximum = 40000;
            ScanRateCalc.Minimum = 500;
            ScanRateCalc.DataContext = monchaDevice;
            ScanRateCalc.SetBinding(Slider.ValueProperty, "ScanRateCalc");

            FPSUpDn.DataContext = monchaDevice;
            FPSUpDn.SetBinding(NumericUpDown.ValueProperty, "FPS");

            InvertXtoggle.DataContext = monchaDevice;
            InvertXtoggle.SetBinding(ToggleSwitch.IsOnProperty, "InvertedX");

            InvertYtoggle.DataContext = monchaDevice;
            InvertYtoggle.SetBinding(ToggleSwitch.IsOnProperty, "InvertedY");

            AlphaSlider.DataContext = monchaDevice;
            AlphaSlider.SetBinding(Slider.ValueProperty, "Alpha");

            //ObjectReadySetting
            RedUpDn.DataContext = monchaDevice.ProjectionSetting;
            RedUpDn.SetBinding(NumericUpDown.ValueProperty, "Red");

            RedToggle.DataContext = monchaDevice.ProjectionSetting;
            RedToggle.SetBinding(ToggleSwitch.IsOnProperty, "RedOn");

            GreenUpDn.DataContext = monchaDevice.ProjectionSetting;
            GreenUpDn.SetBinding(NumericUpDown.ValueProperty, "Green");

            GreenToggle.DataContext = monchaDevice.ProjectionSetting;
            GreenToggle.SetBinding(ToggleSwitch.IsOnProperty, "GreenOn");

            BlueUpDn.DataContext = monchaDevice.ProjectionSetting;
            BlueUpDn.SetBinding(NumericUpDown.ValueProperty, "Blue");

            BlueToggle.DataContext = monchaDevice.ProjectionSetting;
            BlueToggle.SetBinding(ToggleSwitch.IsOnProperty, "BlueOn");

            AngleWaitSlider.DataContext = monchaDevice.ProjectionSetting;
            AngleWaitSlider.SetBinding(Slider.ValueProperty, "StartLineWait");

            EndBlankSlider.DataContext = monchaDevice.ProjectionSetting;
            EndBlankSlider.SetBinding(Slider.ValueProperty, "EndBlanckWait");

            StartBlankSlider.DataContext = monchaDevice.ProjectionSetting;
            StartBlankSlider.SetBinding(Slider.ValueProperty, "StartBlankWait");

            CRSUpDn.DataContext = monchaDevice.ProjectionSetting.PointStep;
            CRSUpDn.SetBinding(NumericUpDown.ValueProperty, "MX");

            monchaDevice.PropertyChanged += ProjectionSetting_PropertyChanged;
            monchaDevice.ProjectionSetting.PropertyChanged += ProjectionSetting_PropertyChanged;
            monchaDevice.ProjectionSetting.PointStep.PropertyChanged += ProjectionSetting_PropertyChanged;
        }

        private void ProjectionSetting_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            NeedUpdate?.Invoke(this, null);
        }

        private void LMeter_ChangeDimention(object sender, double e)
        {
           /* DistanceLabel.Invoke(() => {
                DistanceLabel.Content = e;
            });*/
        }

        private void ScanRateAnchor_Click(object sender, RoutedEventArgs e)
        {
            AppSt.Default.int_scananchor = ScanRateAnchor.IsChecked.Value;
        }

        private void LaserMeterToggle_Toggled(object sender, RoutedEventArgs e)
        {
            if (LaserMetersCombo.SelectedItem is VLTLaserMeters laserMeters)
            {
                laserMeters.Turn(LaserMeterToggle.IsOn);
            }
        }

        private void DeviceCombo_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (DeviceCombo.SelectedIndex == -1) DeviceCombo.SelectedIndex = 0;
        }

        private void LaserMetersCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (DeviceCombo.SelectedItem is MonchaDevice monchaDevice)
            {
                monchaDevice.ProjectionSetting.ReconnectLMeter(LaserMetersCombo.SelectedItem as VLTLaserMeters);

                DistanceUpDn.DataContext = monchaDevice.ProjectionSetting;
                DistanceUpDn.SetBinding(NumericUpDown.ValueProperty, "Distance");

            }
        }

        private void ClearCalcMeshBtn_Click(object sender, RoutedEventArgs e)
        {
               this._device.CalculateMesh.Points = null;
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
            deviceSettingDialog.DrawObjects += DeviceSettingDialog_DrawObjects;
            deviceSettingDialog.Show();
        }

        private void DeviceSettingDialog_DrawObjects(object sender, List<FrameworkElement> e)
        {
            DrawObjects?.Invoke(this, e);
        }

        private void CommonSettingToggle_Toggled(object sender, RoutedEventArgs e)
        {
            BindingDeviceSetting(this.Device);
        }
    }
}
