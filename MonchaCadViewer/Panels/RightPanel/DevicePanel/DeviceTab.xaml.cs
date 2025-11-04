using CadProjectorSDK;
using CadProjectorSDK.Device;
using CadProjectorSDK.Device.Controllers;
using CadProjectorViewer.Dialogs;
using CadProjectorViewer.Panels.DevicePanel;
using CadProjectorViewer.Services;
using CadProjectorViewer.ViewModel.Devices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace CadProjectorViewer.Panels
{
    /// <summary>
    /// Логика взаимодействия для DeviceTab.xaml
    /// </summary>
    public partial class DeviceTab : UserControl
    {
        private ProjectorHub hub => (ProjectorHub)this.DataContext;
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
                DeviceSettingDialog deviceSettingDialog = new DeviceSettingDialog() { DataContext = (LProjector)DeviceCombo.SelectedItem };
                deviceSettingDialog.Show();
            }
        }

        private void SceneSettingBtn_Click(object sender, RoutedEventArgs e)
        {
            SceneSettingWindow sceneSettingWindow = new SceneSettingWindow() { DataContext = hub };
            sceneSettingWindow.Show();
        }

        private void EllipseSettingBtn_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button)
            {
                if (button.DataContext is LProjector projector)
                {
                    var dialog = new DeviceModulesDialog();
                    var context = new AddDeviceModule(projector.ModulesGroup);
                    dialog.DataContext = context;
                    dialog.Show();
                }
            }
        }


        private void MeshSettingBtn_Click(object sender, RoutedEventArgs e)
        {
            if (this.device.SelectedMesh != null)
            {
                CreateGridWindow createGridWindow = new CreateGridWindow(this.device.SelectedMesh);
                createGridWindow.Show();
            }
        }

        private void MeshListBtn_Click(object sender, RoutedEventArgs e)
        {
            MeshesDialog meshesDialog = new MeshesDialog() { DataContext = this.device };
            meshesDialog.Show();
        }

        private void NumericUpDown_PreviewTextInput(object sender, System.Windows.Input.TextCompositionEventArgs e)
        {
            InputValidation.NumberPerDotValidationTextBox(sender, e);
        }
    }
}
