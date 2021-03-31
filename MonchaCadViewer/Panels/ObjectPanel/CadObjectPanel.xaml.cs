﻿using MahApps.Metro.Controls;
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

            MirrorCheck.DataContext = cadObject;
            MirrorCheck.IsChecked = cadObject.Mirror;
            MirrorCheck.SetBinding(CheckBox.IsCheckedProperty, "Mirror");

            RenderCheck.IsChecked = cadObject.Render;
            RenderCheck.SetBinding(CheckBox.IsCheckedProperty, "Render");
            RenderCheck.DataContext = cadObject;

            FixCheck.IsChecked = cadObject.IsFix;
            FixCheck.SetBinding(CheckBox.IsCheckedProperty, "IsFix");
            FixCheck.DataContext = cadObject;

            AngleUpDn.Value = cadObject.AngleZ;
            AngleUpDn.SetBinding(NumericUpDown.ValueProperty, "AngleZ");
            AngleUpDn.DataContext = cadObject;

            WidthUpDn.Value = cadObject.ScaleX;
            WidthUpDn.Interval = MonchaCadViewer.Properties.Settings.Default.stg_scale_percent == true ? 1 : 0.01;
            Binding bindingScaleX = new Binding("ScaleX");
            bindingScaleX.Converter = new ScaleConverter(MonchaCadViewer.Properties.Settings.Default.stg_scale_percent, MonchaCadViewer.Properties.Settings.Default.stg_scale_invert);
            WidthUpDn.SetBinding(NumericUpDown.ValueProperty, bindingScaleX);
            WidthUpDn.DataContext = cadObject;

            HeightUpDn.Value = cadObject.ScaleY;
            HeightUpDn.Interval = MonchaCadViewer.Properties.Settings.Default.stg_scale_percent == true ? 1 : 0.01;
            Binding bindingScaleY = new Binding("ScaleY");
            bindingScaleY.Converter = new ScaleConverter(MonchaCadViewer.Properties.Settings.Default.stg_scale_percent, MonchaCadViewer.Properties.Settings.Default.stg_scale_invert);
            HeightUpDn.SetBinding(NumericUpDown.ValueProperty, bindingScaleY);
            HeightUpDn.DataContext = cadObject;

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
    }
}