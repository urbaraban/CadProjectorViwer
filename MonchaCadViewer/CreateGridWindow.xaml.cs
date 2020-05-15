using MonchaCadViewer.CanvasObj;
using MonchaSDK.Device;
using MonchaSDK.Object;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Media3D;

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
            WidthLabel.Content = this._device.Size.X;
            HeightLabel.Content = this._device.Size.Y;
            WidthUpDn.Value = this._device.BaseMesh.GetLength(1);
            HeightUpDn.Value = this._device.BaseMesh.GetLength(0);
        }


        private void WidthUpDn_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double?> e)
        {
            if (this.IsLoaded)
            {
                this._mesh = MonchaDeviceMesh.MakeMesh((int)HeightUpDn.Value.Value, (int)WidthUpDn.Value.Value);
            }
        }

        private void StepUpDn_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double?> e)
        {
            if (this.IsLoaded)
            {
                int Width = (int)(this._device.Size.X / StepUpDn.Value.Value);
                int Height = (int)(this._device.Size.Y / StepUpDn.Value.Value);
                WidthStepLabel.Content = "(" + Math.Round(this._device.Size.X / Width, 1) + ")";
                HeightStepLabel.Content = "(" + Math.Round(this._device.Size.Y / Height, 1) + ")";

                WidthUpDn.Value = Width;
                HeightUpDn.Value = Height;

                _mesh = MonchaDeviceMesh.MakeMesh(Height, Width);
            }
        }

    }

  
}
