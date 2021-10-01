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

namespace CadProjectorViewer
{
    /// <summary>
    /// Логика взаимодействия для CreateGridWindow.xaml
    /// </summary>
    public partial class CreateGridWindow : Window
    {
        public ProjectionScene projectionScene { get; set; } = new ProjectionScene();

        private ProjectorMesh _mesh;

        public CreateGridWindow(ProjectorMesh mesh)
        {
            InitializeComponent();
            this._mesh = mesh;
            this.DataContextChanged += CreateGridWindow_DataContextChanged;
            this.DataContext = new ProjectorMesh(ProjectorMesh.MakeMeshPoint(this._mesh.Points.GetLength(0), this._mesh.Points.GetLength(1)), this._mesh.Name);
        }

        private void CreateGridWindow_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            projectionScene.Clear();
            if (this.DataContext is ProjectorMesh mesh)
            {
                projectionScene.Add(mesh);
            }
        }


        private void WidthUpDn_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double?> e)
        {
            if (this.IsLoaded)
            {
                this.DataContext = new ProjectorMesh(ProjectorMesh.MakeMeshPoint((int)HeightUpDn.Value.Value, (int)WidthUpDn.Value.Value), this._mesh.Name);
            }
        }

        private void StepUpDn_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double?> e)
        {
            if (this.IsLoaded)
            {
                int Width = (int)(this._mesh.Size.X / StepUpDn.Value.Value) + 1;
                int Height = (int)(this._mesh.Size.Y / StepUpDn.Value.Value) + 1;
                WidthStepLabel.Content = "(" + Math.Round(this._mesh.Size.X / Width, 1) + ")";
                HeightStepLabel.Content = "(" + Math.Round(this._mesh.Size.Y / Height, 1) + ")";

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
                    this._mesh.Points = ProjectorMesh.MakeMeshPoint((int)HeightUpDn.Value.Value, (int)WidthUpDn.Value.Value);
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


}
