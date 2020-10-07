using MonchaCadViewer.CanvasObj;
using MonchaSDK;
using MonchaSDK.Device;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace MonchaCadViewer
{
    /// <summary>
    /// Логика взаимодействия для CreateGridWindow.xaml
    /// </summary>
    public partial class CreateGridWindow : Window
    {
        private MonchaDevice _device;
        private MonchaDeviceMesh _mesh;
        private CadCanvas cadCanvas;
        public CreateGridWindow(MonchaDevice Device, MonchaDeviceMesh Mesh)
        {
            InitializeComponent();
            this._device = Device;
            this._mesh = Mesh;
            WidthLabel.Content = this._device.BSize.X;
            HeightLabel.Content = this._device.BSize.Y;

            cadCanvas = new CadCanvas(MonchaHub.Size, true);
            cadCanvas.Background = Brushes.White;
            cadCanvas.Margin = new Thickness(20);
            cadCanvas.Focusable = true;
            cadCanvas.ContextMenu = new ContextMenu();
            //cadCanvas.ErrorMessageEvent += CadCanvas_ErrorMessageEvent;
            CadViewBox.Child = cadCanvas;
        }


        private void WidthUpDn_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double?> e)
        {
            if (this.IsLoaded)
            {
                cadCanvas.DrawMesh(new MonchaDeviceMesh(MonchaDeviceMesh.MakeMeshPoint((int)HeightUpDn.Value.Value, (int)WidthUpDn.Value.Value), string.Empty, false), this._device);
            }
        }

        private void StepUpDn_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double?> e)
        {
            if (this.IsLoaded)
            {
                int Width = (int)(this._device.BSize.X / StepUpDn.Value.Value) + 1;
                int Height = (int)(this._device.BSize.Y / StepUpDn.Value.Value) + 1;
                WidthStepLabel.Content = "(" + Math.Round(this._device.BSize.X / Width, 1) + ")";
                HeightStepLabel.Content = "(" + Math.Round(this._device.BSize.Y / Height, 1) + ")";

                WidthUpDn.Value = Width;
                HeightUpDn.Value = Height;
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
                WidthUpDn.Maximum = 50;
                HeightUpDn.Maximum = 50;
            }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            MessageBoxResult messageBoxResult = MessageBox.Show("Сохранить сетку?", "Внимание", MessageBoxButton.YesNoCancel, MessageBoxImage.Warning);
            switch (messageBoxResult)
            {
                case MessageBoxResult.Yes:
                    this._mesh.SubscribePoint(false);
                    this._mesh.Points = MonchaDeviceMesh.MakeMeshPoint((int)HeightUpDn.Value.Value, (int)WidthUpDn.Value.Value);
                    this._mesh.SubscribePoint(true);
                    break;
                case MessageBoxResult.No:
                    break;
                case MessageBoxResult.Cancel:
                    e.Cancel = true;
                    break;
            }
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            WidthUpDn.Value = this._mesh.GetLength(1);
            HeightUpDn.Value = this._mesh.GetLength(0);

            cadCanvas.DrawMesh(new MonchaDeviceMesh(MonchaDeviceMesh.MakeMeshPoint((int)HeightUpDn.Value.Value, (int)WidthUpDn.Value.Value), string.Empty, false), this._device);
        }
    }


}
