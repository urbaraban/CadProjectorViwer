using MahApps.Metro.Controls;
using MonchaCadViewer.CanvasObj;
using MonchaSDK;
using MonchaSDK.Device;
using MonchaSDK.Object;
using MonchaSDK.Setting;
using System;
using System.Collections.Generic;
using System.ComponentModel;
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

namespace MonchaCadViewer.ToolsPanel
{
    /// <summary>
    /// Логика взаимодействия для multiplierpanel.xaml
    /// </summary>
    public partial class multiplierpanel : UserControl
    {
        public event EventHandler NeedUpdate;

        private CadObject cadObject;

        public multiplierpanel()
        {
            InitializeComponent();

            this.DataContextChanged += Multiplierpanel_DataContextChanged;

        }

        private void Multiplierpanel_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (this.DataContext is CadObject cadObject)
            {
                this.IsEnabled = true;
                this.cadObject = cadObject;

                OtherSettingSwitch.DataContext = cadObject;
                OtherSettingSwitch.SetBinding(ToggleSwitch.IsOnProperty, "OwnedSetting");

                BindingCadObject(cadObject.ProjectionSetting);
            }
            else
            {
                this.IsEnabled = false;
                CRSUpDn.DataContext = null;
                RadiusSlider.DataContext = null;
                MultiplierSlider.DataContext = null;
            }
        }

        private void BindingCadObject(LProjectionSetting projectionSetting)
        {
            CRSUpDn.DataContext = projectionSetting.PointStep;
            CRSUpDn.SetBinding(NumericUpDown.ValueProperty, "MX");
            projectionSetting.PointStep.PropertyChanged += M_ChangePoint;

            RadiusSlider.Value = projectionSetting.RadiusEdge;
            RadiusSlider.DataContext = projectionSetting;
            RadiusSlider.SetBinding(Slider.ValueProperty, "RadiusEdge");
            MonchaHub.Size.PropertyChanged += Size_ChangePoint;

            MultiplierSlider.Value = projectionSetting.StartLineWait;
            MultiplierSlider.DataContext = projectionSetting;
            MultiplierSlider.SetBinding(Slider.ValueProperty, "StartLineWait");

            DistanceUpDn.DataContext = projectionSetting;
            DistanceUpDn.SetBinding(NumericUpDown.ValueProperty, "Distance");

            RedUpDn.DataContext = projectionSetting;
            RedUpDn.SetBinding(NumericUpDown.ValueProperty, "Red");

            RedToggle.DataContext = projectionSetting;
            RedToggle.SetBinding(ToggleSwitch.IsOnProperty, "RedOn");

            GreenUpDn.DataContext = projectionSetting;
            GreenUpDn.SetBinding(NumericUpDown.ValueProperty, "Green");

            GreenToggle.DataContext = projectionSetting;
            GreenToggle.SetBinding(ToggleSwitch.IsOnProperty, "GreenOn");

            BlueUpDn.DataContext = projectionSetting;
            BlueUpDn.SetBinding(NumericUpDown.ValueProperty, "Blue");

            BlueToggle.DataContext = projectionSetting;
            BlueToggle.SetBinding(ToggleSwitch.IsOnProperty, "BlueOn");

            DeviceLayerCombo.Items.Clear();
            DeviceLayerCombo.DisplayMemberPath = "HWIdentifier";
            DeviceLayerCombo.Items.Add(null);
            foreach(MonchaDevice device in MonchaHub.Devices)
            {
                DeviceLayerCombo.Items.Add(device);
            }
           
            projectionSetting.PropertyChanged += ProjectionSetting_PropertyChanged;
            projectionSetting.PointStep.PropertyChanged += ProjectionSetting_PropertyChanged;
        }

        private void ProjectionSetting_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            this.NeedUpdate?.Invoke(this, null);
        }

        private void Size_ChangePoint(object sender, PropertyChangedEventArgs e)
        {
            RadiusSlider.Maximum = Math.Max(MonchaHub.Size.MX, MonchaHub.Size.MY);
        }

        private void M_ChangePoint(object sender, PropertyChangedEventArgs e)
        {
            CRSUpDn.SetBinding(NumericUpDown.ValueProperty, "PointStep");
        }

        private void OtherSettingSwitch_Toggled(object sender, RoutedEventArgs e)
        {
            CRSUpDn.DataContext = null;
            RadiusSlider.DataContext = null;
            MultiplierSlider.DataContext = null;
            DistanceUpDn.DataContext = null;
            BindingOperations.ClearBinding(CRSUpDn, NumericUpDown.ValueProperty);
            BindingOperations.ClearBinding(RadiusSlider, Slider.ValueProperty);
            BindingOperations.ClearBinding(MultiplierSlider, Slider.ValueProperty);
            BindingOperations.ClearBinding(DistanceUpDn, NumericUpDown.ValueProperty);
            BindingCadObject(this.cadObject.ProjectionSetting);
        }

        private void DeviceLayerCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (this.DataContext is CadObject cadObject)
            {
                cadObject.ProjectionSetting.device = (MonchaDevice)DeviceLayerCombo.SelectedItem;
            }
        }
    }
}
