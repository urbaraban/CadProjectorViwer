using MahApps.Metro.Controls;
using MonchaCadViewer.CanvasObj;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace MonchaCadViewer.Panels
{
    /// <summary>
    /// Логика взаимодействия для CadObjectPanel.xaml
    /// </summary>
    public partial class CadObjectPanel : UserControl
    {
        public CadObjectPanel()
        {
            InitializeComponent();
            this.DataContextChanged += CadObjectPanel_DataContextChanged;
        }

        public void DataContextUpdate(CadObject cadObject)
        {
            this.IsEnabled = true;
            XUpDn.DataContext = cadObject;
            XUpDn.SetBinding(NumericUpDown.ValueProperty, "X");
            

            YUpDn.DataContext = cadObject;
            YUpDn.SetBinding(NumericUpDown.ValueProperty, "Y");
            

            ZUpDn.DataContext = cadObject;
            ZUpDn.SetBinding(NumericUpDown.ValueProperty, "Z");
            
            MirrorCheck.DataContext = cadObject;
            MirrorCheck.SetBinding(CheckBox.IsCheckedProperty, "Mirror");
            

            RenderCheck.DataContext = cadObject;
            RenderCheck.SetBinding(CheckBox.IsCheckedProperty, "Render");
            

            FixCheck.DataContext = cadObject;
            FixCheck.SetBinding(CheckBox.IsCheckedProperty, "IsFix");
            

            AngleZUpDn.DataContext = cadObject;
            AngleZUpDn.SetBinding(NumericUpDown.ValueProperty, "AngleZ");


            AngleYUpDn.DataContext = cadObject;
            AngleYUpDn.SetBinding(NumericUpDown.ValueProperty, "AngleY");


            AngleXUpDn.DataContext = cadObject;
            AngleXUpDn.SetBinding(NumericUpDown.ValueProperty, "AngleX");
            

            
            WidthUpDn.Interval = MonchaCadViewer.Properties.Settings.Default.stg_scale_percent == true ? 1 : 0.01;
            Binding bindingScaleX = new Binding("ScaleX");
            bindingScaleX.Converter = new ScaleConverter(MonchaCadViewer.Properties.Settings.Default.stg_scale_percent, MonchaCadViewer.Properties.Settings.Default.stg_scale_invert);
            WidthUpDn.SetBinding(NumericUpDown.ValueProperty, bindingScaleX);
            WidthUpDn.DataContext = cadObject;
            //WidthUpDn.Value = cadObject.ScaleX;

            HeightUpDn.DataContext = cadObject;
            HeightUpDn.Interval = MonchaCadViewer.Properties.Settings.Default.stg_scale_percent == true ? 1 : 0.01;
            Binding bindingScaleY = new Binding("ScaleY");
            bindingScaleY.Converter = new ScaleConverter(MonchaCadViewer.Properties.Settings.Default.stg_scale_percent, MonchaCadViewer.Properties.Settings.Default.stg_scale_invert);
            HeightUpDn.SetBinding(NumericUpDown.ValueProperty, bindingScaleY);
            HeightUpDn.DataContext = cadObject;
            //HeightUpDn.Value = cadObject.ScaleY;

            DeepUpDn.DataContext = cadObject;
            DeepUpDn.Interval = MonchaCadViewer.Properties.Settings.Default.stg_scale_percent == true ? 1 : 0.01;
            Binding bindingScaleZ = new Binding("ScaleZ");
            bindingScaleZ.Converter = new ScaleConverter(MonchaCadViewer.Properties.Settings.Default.stg_scale_percent, MonchaCadViewer.Properties.Settings.Default.stg_scale_invert);
            DeepUpDn.SetBinding(NumericUpDown.ValueProperty, bindingScaleZ);
           
            //DeepUpDn.Value = cadObject.ScaleZ;

        }

        private void DisconnectBinding()
        {
            this.IsEnabled = false;
            BindingOperations.ClearBinding(XUpDn, NumericUpDown.ValueProperty);
            BindingOperations.ClearBinding(YUpDn, NumericUpDown.ValueProperty);
            BindingOperations.ClearBinding(ZUpDn, NumericUpDown.ValueProperty);
            BindingOperations.ClearBinding(RenderCheck, CheckBox.IsCheckedProperty);
            BindingOperations.ClearBinding(FixCheck, CheckBox.IsCheckedProperty);
            BindingOperations.ClearBinding(MirrorCheck, CheckBox.IsCheckedProperty);
            BindingOperations.ClearBinding(AngleXUpDn, NumericUpDown.ValueProperty);
            BindingOperations.ClearBinding(AngleYUpDn, NumericUpDown.ValueProperty);
            BindingOperations.ClearBinding(AngleZUpDn, NumericUpDown.ValueProperty);
            BindingOperations.ClearBinding(WidthUpDn, NumericUpDown.ValueProperty);
            BindingOperations.ClearBinding(HeightUpDn, NumericUpDown.ValueProperty);
            BindingOperations.ClearBinding(DeepUpDn, NumericUpDown.ValueProperty);

            XUpDn.DataContext = null;
            XUpDn.Value = null;
            YUpDn.DataContext = null;
            YUpDn.Value = null;
            ZUpDn.DataContext = null;
            ZUpDn.Value = null;
            RenderCheck.DataContext = null;
            RenderCheck.IsChecked = false;
            MirrorCheck.DataContext = null;
            MirrorCheck.IsChecked = false;
            FixCheck.DataContext = null;
            FixCheck.IsChecked = false;
            AngleXUpDn.DataContext = null;
            AngleXUpDn.Value = null;
            AngleYUpDn.DataContext = null;
            AngleYUpDn.Value = null;
            AngleZUpDn.DataContext = null;
            AngleZUpDn.Value = null;
            WidthUpDn.DataContext = null;
            WidthUpDn.Value = null;
            HeightUpDn.DataContext = null;
            HeightUpDn.Value = null;
            DeepUpDn.DataContext = null;
            DeepUpDn.Value = null;
        }

        private void CadObjectPanel_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (this.DataContext is CadObject cadObject)
            {
                DataContextUpdate(cadObject);
            }
            else
            {
                DisconnectBinding();
            }
        }
    }
}
