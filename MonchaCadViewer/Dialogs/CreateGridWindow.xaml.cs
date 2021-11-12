using CadProjectorViewer.CanvasObj;
using CadProjectorSDK;
using CadProjectorSDK.Device;
using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using AppSt = CadProjectorViewer.Properties.Settings;
using CadProjectorSDK.CadObjects;
using CadProjectorSDK.Device.Mesh;
using CadProjectorSDK.Scenes;
using System.Windows.Data;
using System.Globalization;
using CadProjectorSDK.CadObjects.Abstract;

namespace CadProjectorViewer
{
    /// <summary>
    /// Логика взаимодействия для CreateGridWindow.xaml
    /// </summary>
    public partial class CreateGridWindow : Window
    {
        private ProjectorMesh _mesh;

        public CreateGridWindow(ProjectorMesh mesh)
        {
            InitializeComponent();
            this._mesh = mesh;
            this.DataContext = new ProjectorMesh(ProjectorMesh.MakeMeshPoint(this._mesh.Points.GetLength(0), this._mesh.Points.GetLength(1), new CadRect3D(1,1,1)), this._mesh.Name);
        }

        private void WidthUpDn_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double?> e)
        {
            if (this.IsLoaded)
            {
                this.DataContext = new ProjectorMesh(ProjectorMesh.MakeMeshPoint((int)HeightUpDn.Value.Value, (int)WidthUpDn.Value.Value, new CadRect3D(1, 1, 1)), this._mesh.Name);
            }
        }

        private void StepUpDn_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double?> e)
        {
            if (this.IsLoaded)
            {
                int Width = (int)(this._mesh.Size.Width / StepUpDn.Value.Value) + 1;
                int Height = (int)(this._mesh.Size.Height / StepUpDn.Value.Value) + 1;
                WidthStepLabel.Content = "(" + Math.Round(this._mesh.Size.Width / Width, 1) + ")";
                HeightStepLabel.Content = "(" + Math.Round(this._mesh.Size.Height / Height, 1) + ")";

                WidthUpDn.Value = Width;
                HeightUpDn.Value = Height;
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
                    this._mesh.Points = ProjectorMesh.MakeMeshPoint((int)HeightUpDn.Value.Value, (int)WidthUpDn.Value.Value, this._mesh.Size);
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
        }
    }
    public class ToSceneObjConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is UidObject obj)
            {
                return new ProjectionScene(obj) { Size = new CadRect3D(1000, 1000, 0) };
            }
            return new ProjectionScene() { Size = new CadRect3D(1000, 1000, 0) };
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

}
