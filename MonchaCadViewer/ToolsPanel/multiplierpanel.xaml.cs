using MahApps.Metro.Controls;
using MonchaCadViewer.CanvasObj;
using MonchaSDK;
using MonchaSDK.Object;
using MonchaSDK.Setting;
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
                OtherSettingSwitch.SetBinding(ToggleSwitch.IsOnProperty, "OtherProjection");

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
            Console.WriteLine(projectionSetting != MonchaHub.ProjectionSetting);

            CRSUpDn.DataContext = projectionSetting.PointStep;
            CRSUpDn.SetBinding(NumericUpDown.ValueProperty, "MX");
            projectionSetting.PointStep.M.ChangePoint += M_ChangePoint;

            RadiusSlider.Value = projectionSetting.RadiusEdge;
            RadiusSlider.DataContext = projectionSetting;
            RadiusSlider.SetBinding(Slider.ValueProperty, "RadiusEdge");
            RadiusSlider.ValueChanged += RadiusSlider_ValueChanged;
            MonchaHub.Size.ChangePoint += Size_ChangePoint;

            MultiplierSlider.Value = projectionSetting.StartLineWait;
            MultiplierSlider.DataContext = projectionSetting;
            MultiplierSlider.SetBinding(Slider.ValueProperty, "StartLineWait");

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
        }

        private void RadiusSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            cadObject.ProjectionSetting.RadiusEdge = (int)RadiusSlider.Value;
            NeedUpdate?.Invoke(this, null);
        }

        private void Size_ChangePoint(object sender, LPoint3D e)
        {
            RadiusSlider.Maximum = Math.Max(e.MX, e.MY);
        }

        private void M_ChangePoint(object sender, LPoint3D e)
        {
            CRSUpDn.SetBinding(NumericUpDown.ValueProperty, "PointStep");
        }

        private void CRSUpDn_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double?> e)
        {
            NeedUpdate?.Invoke(this, null);
        }

        private void SettingUpDn_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double?> e)
        {
            MonchaHub.RefreshSize();
            MonchaHub.RefreshFrame();
        }

        private void SettingToggle_Toggled(object sender, RoutedEventArgs e)
        {
            MonchaHub.RefreshFrame();
        }


        private void OtherSettingSwitch_Toggled(object sender, RoutedEventArgs e)
        {
            CRSUpDn.DataContext = null;
            RadiusSlider.DataContext = null;
            MultiplierSlider.DataContext = null;
            BindingOperations.ClearBinding(CRSUpDn, NumericUpDown.ValueProperty);
            BindingOperations.ClearBinding(RadiusSlider, Slider.ValueProperty);
            BindingOperations.ClearBinding(MultiplierSlider, Slider.ValueProperty);
            BindingCadObject(this.cadObject.OtherProjection == true ? this.cadObject.ProjectionSetting : MonchaHub.ProjectionSetting );
        }
    }
}
