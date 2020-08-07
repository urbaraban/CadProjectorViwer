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
                XUpDn.DataContext = cadObjectDC.Translate;
                XUpDn.Value = cadObjectDC.Translate.X;

                YUpDn.SetBinding(NumericUpDown.ValueProperty, "Y");
                YUpDn.DataContext = cadObjectDC.Translate; 
                YUpDn.Value = cadObjectDC.Translate.Y;

                MirrorCheck.SetBinding(CheckBox.IsCheckedProperty, "Mirror");
                MirrorCheck.DataContext = cadObjectDC;
                MirrorCheck.IsChecked = cadObjectDC.Mirror;

                RenderCheck.SetBinding(CheckBox.IsCheckedProperty, "Render");
                RenderCheck.DataContext = cadObjectDC; 
                RenderCheck.IsChecked = cadObjectDC.Render;

                FixCheck.SetBinding(CheckBox.IsCheckedProperty, "IsFix");
                FixCheck.DataContext = cadObjectDC; 
                FixCheck.IsChecked = cadObjectDC.IsFix;

                AngleUpDn.SetBinding(NumericUpDown.ValueProperty, "Angle");
                AngleUpDn.DataContext = cadObjectDC.Rotate; 
                AngleUpDn.Value = cadObjectDC.Rotate.Angle;

                WidthUpDn.SetBinding(NumericUpDown.ValueProperty, "ScaleX");
                WidthUpDn.DataContext = cadObjectDC.Scale; 
                WidthUpDn.Value = cadObjectDC.Scale.ScaleX;

                HeightUpDn.SetBinding(NumericUpDown.ValueProperty, "ScaleY");
                HeightUpDn.DataContext = cadObjectDC.Scale;
                HeightUpDn.Value = cadObjectDC.Scale.ScaleY;
                
                
            }
            else if (this.DataContext == null)
            {
                this.IsEnabled = false;
            }
        }

        private void CadObjectPanel_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (this.DataContext is CadObject cadObject)
                DataContextUpdate(cadObject);
        }
    }
}
