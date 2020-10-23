using MahApps.Metro.Controls;
using MonchaCadViewer.CanvasObj;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace MonchaCadViewer.ToolsPanel
{
    /// <summary>
    /// Логика взаимодействия для CadObjectPanel.xaml
    /// </summary>
    public partial class CadObjectPanel : UserControl
    {
        public event EventHandler NeedUpdate;

        public CadObjectPanel()
        {
            InitializeComponent();
            this.DataContextChanged += CadObjectPanel_DataContextChanged;
        }

        public void DataContextUpdate(CadObject cadObject)
        {
            this.IsEnabled = true;
            XUpDn.Value = cadObject.X;
            XUpDn.SetBinding(NumericUpDown.ValueProperty, "X");
            XUpDn.DataContext = cadObject;

            YUpDn.Value = cadObject.Y;
            YUpDn.SetBinding(NumericUpDown.ValueProperty, "Y");
            YUpDn.DataContext = cadObject;

            MirrorCheck.DataContext = this.DataContext;
            MirrorCheck.IsChecked = cadObject.Mirror;
            MirrorCheck.SetBinding(CheckBox.IsCheckedProperty, "Mirror");

            RenderCheck.IsChecked = cadObject.Render;
            RenderCheck.SetBinding(CheckBox.IsCheckedProperty, "Render");
            RenderCheck.DataContext = this.DataContext;

            FixCheck.IsChecked = cadObject.IsFix;
            FixCheck.SetBinding(CheckBox.IsCheckedProperty, "IsFix");
            FixCheck.DataContext = this.DataContext;

            AngleUpDn.Value = cadObject.Angle;
            AngleUpDn.SetBinding(NumericUpDown.ValueProperty, "Angle");
            AngleUpDn.DataContext = this.DataContext;

            WidthUpDn.Value = cadObject.ScaleX;
            WidthUpDn.SetBinding(NumericUpDown.ValueProperty, "ScaleX");
            WidthUpDn.DataContext = this.DataContext;

            HeightUpDn.Value = cadObject.ScaleY;
            HeightUpDn.SetBinding(NumericUpDown.ValueProperty, "ScaleY");
            HeightUpDn.DataContext = this.DataContext;

        }

        private void DisconnectBinding()
        {
            this.IsEnabled = false;
            BindingOperations.ClearBinding(XUpDn, NumericUpDown.ValueProperty);
            BindingOperations.ClearBinding(YUpDn, NumericUpDown.ValueProperty);
            BindingOperations.ClearBinding(RenderCheck, CheckBox.IsCheckedProperty);
            BindingOperations.ClearBinding(FixCheck, CheckBox.IsCheckedProperty);
            BindingOperations.ClearBinding(MirrorCheck, CheckBox.IsCheckedProperty);
            BindingOperations.ClearBinding(AngleUpDn, NumericUpDown.ValueProperty);
            BindingOperations.ClearBinding(WidthUpDn, NumericUpDown.ValueProperty);
            BindingOperations.ClearBinding(HeightUpDn, NumericUpDown.ValueProperty);

            XUpDn.DataContext = null;
            XUpDn.Value = null;
            YUpDn.DataContext = null;
            YUpDn.Value = null;
            RenderCheck.DataContext = null;
            RenderCheck.IsChecked = false;
            MirrorCheck.DataContext = null;
            MirrorCheck.IsChecked = false;
            FixCheck.DataContext = null;
            FixCheck.IsChecked = false;
            AngleUpDn.DataContext = null;
            AngleUpDn.Value = null;
            WidthUpDn.DataContext = null;
            WidthUpDn.Value = null;
            HeightUpDn.DataContext = null;
            HeightUpDn.Value = null;
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

        private void XUpDn_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double?> e)
        {

        }

    }
}
