using MahApps.Metro.Controls;
using MonchaCadViewer.CanvasObj;
using System;
using System.Windows;
using System.Windows.Controls;


namespace MonchaCadViewer.ToolsPanel
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
            this.DataContext = cadObject;

            if (this.DataContext is CadObject cadObjectDC)
            {
                this.IsEnabled = true;
                XUpDn.SetBinding(NumericUpDown.ValueProperty, "X");
                XUpDn.DataContext = this.DataContext;
                XUpDn.Value = cadObjectDC.X;

                YUpDn.SetBinding(NumericUpDown.ValueProperty, "Y");
                YUpDn.DataContext = this.DataContext; 
                YUpDn.Value = cadObjectDC.Y;

                MirrorCheck.DataContext = this.DataContext;
                MirrorCheck.IsChecked = cadObjectDC.Mirror;
                MirrorCheck.SetBinding(CheckBox.IsCheckedProperty, "Mirror");


                RenderCheck.SetBinding(CheckBox.IsCheckedProperty, "Render");
                RenderCheck.DataContext = this.DataContext; 
                RenderCheck.IsChecked = cadObjectDC.Render;

                FixCheck.SetBinding(CheckBox.IsCheckedProperty, "IsFix");
                FixCheck.DataContext = this.DataContext; 
                FixCheck.IsChecked = cadObjectDC.IsFix;

                AngleUpDn.SetBinding(NumericUpDown.ValueProperty, "Angle");
                AngleUpDn.DataContext = this.DataContext; 
                AngleUpDn.Value = cadObjectDC.Angle;

                WidthUpDn.SetBinding(NumericUpDown.ValueProperty, "ScaleX");
                WidthUpDn.DataContext = this.DataContext; 
                WidthUpDn.Value = cadObjectDC.ScaleX;

                HeightUpDn.SetBinding(NumericUpDown.ValueProperty, "ScaleY");
                HeightUpDn.DataContext = this.DataContext;
                HeightUpDn.Value = cadObjectDC.ScaleY;
                
                
            }
            else if (this.DataContext == null)
            {
                this.IsEnabled = false;

                XUpDn.DataContext = null;
                XUpDn.Value = null;
                YUpDn.DataContext = null;
                YUpDn.Value = null;
                RenderCheck.DataContext = null;
                RenderCheck.IsChecked = false;
                FixCheck.DataContext = null;
                FixCheck.IsChecked = false;
                AngleUpDn.DataContext = null;
                AngleUpDn.Value = null;
                WidthUpDn.DataContext = null;
                WidthUpDn.Value = null;
                HeightUpDn.DataContext = null;
                HeightUpDn.Value = null;
            }
        }

        private void CadObjectPanel_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (this.DataContext is CadObject cadObject)
                DataContextUpdate(cadObject);
        }
    }
}
