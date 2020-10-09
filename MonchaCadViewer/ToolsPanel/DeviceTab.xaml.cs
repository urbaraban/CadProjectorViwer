using MahApps.Metro.Controls;
using MonchaCadViewer.CanvasObj;
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
using System.Windows.Shapes;
using Xceed.Wpf.Toolkit;

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
                //LaserMeter
                if (tempdevice.LMeter != null)
                {
                    LaserMetersCombo.SelectedValue = tempdevice.LMeter.HWIdentifier;

                    LaserMeterToggle.DataContext = tempdevice.LMeter;
                    LaserMeterToggle.SetBinding(ToggleSwitch.IsOnProperty, "IsTurn");

                    DistanceUpDn.DataContext = tempdevice.LMeter;
                    DistanceUpDn.SetBinding(NumericUpDown.ValueProperty, "Distance");

                    tempdevice.LMeter.ChangeDimention += LMeter_ChangeDimention;

                }

                //Device
                ScanRateRealSlider.Maximum = tempdevice.nETInfo.maxScanrate;
                ScanRateRealSlider.DataContext = tempdevice;
                ScanRateRealSlider.SetBinding(Slider.ValueProperty, "ScanRateReal");
                

                ScanRateCalc.DataContext = tempdevice;
                ScanRateCalc.SetBinding(Slider.ValueProperty, "ScanRateCalc");

                FPSUpDn.DataContext = tempdevice;
                FPSUpDn.SetBinding(NumericUpDown.ValueProperty, "FPS");

                InvertXtoggle.DataContext = tempdevice;
                InvertXtoggle.SetBinding(ToggleSwitch.IsOnProperty, "InvertedX");

                InvertYtoggle.DataContext = tempdevice;
                InvertYtoggle.SetBinding(ToggleSwitch.IsOnProperty, "InvertedY");

                AlphaSlider.DataContext = tempdevice;
                AlphaSlider.SetBinding(Slider.ValueProperty, "Alpha");

                //ObjectReadySetting
                RedUpDn.DataContext = tempdevice.ProjectionSetting;
                RedUpDn.SetBinding(NumericUpDown.ValueProperty, "Red");

                RedToggle.DataContext = tempdevice.ProjectionSetting;
                RedToggle.SetBinding(ToggleSwitch.IsOnProperty, "RedOn");

                GreenUpDn.DataContext = tempdevice.ProjectionSetting;
                GreenUpDn.SetBinding(NumericUpDown.ValueProperty, "Green");

                GreenToggle.DataContext = tempdevice.ProjectionSetting;
                GreenToggle.SetBinding(ToggleSwitch.IsOnProperty, "GreenOn");

                BlueUpDn.DataContext = tempdevice.ProjectionSetting;
                BlueUpDn.SetBinding(NumericUpDown.ValueProperty, "Blue");

                BlueToggle.DataContext = tempdevice.ProjectionSetting;
                BlueToggle.SetBinding(ToggleSwitch.IsOnProperty, "BlueOn");



                AngleWaitSlider.DataContext = tempdevice.ProjectionSetting;
                AngleWaitSlider.SetBinding(Slider.ValueProperty, "StartLineWait");

                EndBlankSlider.DataContext = tempdevice.ProjectionSetting;
                EndBlankSlider.SetBinding(Slider.ValueProperty, "EndBlanckWait");

                StartBlankSlider.DataContext = tempdevice.ProjectionSetting;
                StartBlankSlider.SetBinding(Slider.ValueProperty, "StartBlankWait");

                CRSUpDn.DataContext = tempdevice.ProjectionSetting.PointStep;
                CRSUpDn.SetBinding(NumericUpDown.ValueProperty, "MX");

                tempdevice.PropertyChanged += ProjectionSetting_PropertyChanged;
                tempdevice.ProjectionSetting.PropertyChanged += ProjectionSetting_PropertyChanged;
                tempdevice.ProjectionSetting.PointStep.PropertyChanged += ProjectionSetting_PropertyChanged;
            }
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
                monchaDevice.ReconnectLMeter(LaserMetersCombo.SelectedItem as VLTLaserMeters);

                DistanceUpDn.DataContext = monchaDevice.LMeter;
                DistanceUpDn.SetBinding(NumericUpDown.ValueProperty, "Distance");

            }
        }

        private void SetDistanceBtn_Click(object sender, RoutedEventArgs e)
        {
           /* SetDistanceBtn.Invoke(() =>
            {
                if (LaserMetersCombo.SelectedItem is VLTLaserMeters laserMeters)
                {
                    laserMeters.Distance = laserMeters.RealDistance;
                    DistanceUpDn.Value = laserMeters.Distance;
                }
            });*/

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

    }
}
