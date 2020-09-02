using MonchaSDK.Device;
using System;
using System.Windows;


namespace MonchaCadViewer
{
    /// <summary>
    /// Логика взаимодействия для CreateGridWindow.xaml
    /// </summary>
    public partial class CreateGridWindow : Window
    {
        private MonchaDevice _device;
        private MonchaDeviceMesh _mesh;
        public CreateGridWindow(MonchaDevice Device, MonchaDeviceMesh Mesh)
        {
            InitializeComponent();
            this._device = Device;
            this._mesh = Mesh;
            WidthLabel.Content = this._device.BSize.X;
            HeightLabel.Content = this._device.BSize.Y;
            WidthUpDn.Value = this._device.BaseMesh.GetLength(1);
            HeightUpDn.Value = this._device.BaseMesh.GetLength(0);
        }


        private void WidthUpDn_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double?> e)
        {
            if (this.IsLoaded)
            {
                this._mesh.Points = MonchaDeviceMesh.MakeMeshPoint((int)HeightUpDn.Value.Value, (int)WidthUpDn.Value.Value);
            }
        }

        private void StepUpDn_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double?> e)
        {
            if (this.IsLoaded)
            {
                int Width = (int)(this._device.BSize.X / StepUpDn.Value.Value);
                int Height = (int)(this._device.BSize.Y / StepUpDn.Value.Value);
                WidthStepLabel.Content = "(" + Math.Round(this._device.BSize.X / Width, 1) + ")";
                HeightStepLabel.Content = "(" + Math.Round(this._device.BSize.Y / Height, 1) + ")";

                WidthUpDn.Value = Width;
                HeightUpDn.Value = Height;

                this._mesh = new MonchaDeviceMesh(MonchaDeviceMesh.MakeMeshPoint(Height, Width), this._mesh.Name);
            }
        }

        private void MonchaToggle_Toggled(object sender, RoutedEventArgs e)
        {
            if (MonchaToggle.IsOn)
            {
                WidthUpDn.Maximum = 20;
                HeightUpDn.Maximum = 20;
            }
            else
            {
                WidthUpDn.Maximum = 999;
                HeightUpDn.Maximum = 999;
            }
        }
    }

  
}
