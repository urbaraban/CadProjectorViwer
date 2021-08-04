using MonchaCadViewer.CanvasObj;
using MonchaSDK;
using MonchaSDK.Device;
using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using AppSt = MonchaCadViewer.Properties.Settings;

namespace MonchaCadViewer
{
    /// <summary>
    /// Логика взаимодействия для CreateGridWindow.xaml
    /// </summary>
    public partial class CreateGridWindow : Window
    {
        private ProjectionScene projectionScene = new ProjectionScene();

        private MonchaDevice _device;
        private LDeviceMesh _mesh;
        private CadCanvas cadCanvas;
        public CreateGridWindow(MonchaDevice Device, LDeviceMesh Mesh)
        {
            InitializeComponent();
            this._device = Device;
            this._mesh = Mesh;
            WidthLabel.Content = this._device.Size.X;
            HeightLabel.Content = this._device.Size.Y;
            NameBox.DataContext = Mesh;
            NameBox.SetBinding(TextBox.TextProperty, "Name");
        }


        private void WidthUpDn_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double?> e)
        {
            if (this.IsLoaded)
            {
                projectionScene.AddRange(
                    CadCanvas.GetMesh(
                    new LDeviceMesh(LDeviceMesh.MakeMeshPoint((int)HeightUpDn.Value.Value, (int)WidthUpDn.Value.Value), string.Empty),
                    MonchaHub.GetThinkess * AppSt.Default.anchor_size, false, MeshType.NONE).ToArray());
            }
        }

        private void StepUpDn_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double?> e)
        {
            if (this.IsLoaded)
            {
                int Width = (int)(this._device.Size.X / StepUpDn.Value.Value) + 1;
                int Height = (int)(this._device.Size.Y / StepUpDn.Value.Value) + 1;
                WidthStepLabel.Content = "(" + Math.Round(this._device.Size.X / Width, 1) + ")";
                HeightStepLabel.Content = "(" + Math.Round(this._device.Size.Y / Height, 1) + ")";

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

        protected override void OnClosing(CancelEventArgs e)
        {
            base.OnClosing(e);
            MessageBoxResult messageBoxResult = MessageBox.Show("Сохранить сетку?", "Внимание", MessageBoxButton.YesNoCancel, MessageBoxImage.Warning);
            switch (messageBoxResult)
            {
                case MessageBoxResult.Yes:
                    this._mesh.SubscribePoint(false);
                    this._mesh.Points = LDeviceMesh.MakeMeshPoint((int)HeightUpDn.Value.Value, (int)WidthUpDn.Value.Value);
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

            projectionScene.AddRange(
                     CadCanvas.GetMesh(
                     new LDeviceMesh(LDeviceMesh.MakeMeshPoint((int)HeightUpDn.Value.Value, (int)WidthUpDn.Value.Value), string.Empty),
                     MonchaHub.GetThinkess * AppSt.Default.anchor_size, false, MeshType.NONE).ToArray());
        }
    }


}
