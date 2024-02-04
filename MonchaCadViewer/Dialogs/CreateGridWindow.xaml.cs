using CadProjectorSDK.CadObjects;
using CadProjectorSDK.CadObjects.Abstract;
using CadProjectorSDK.Device.Mesh;
using CadProjectorSDK.Scenes;
using CadProjectorViewer.ViewModel.Scene;
using Microsoft.Xaml.Behaviors.Core;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;

namespace CadProjectorViewer
{
    /// <summary>
    /// Логика взаимодействия для CreateGridWindow.xaml
    /// </summary>
    public partial class CreateGridWindow : Window
    {
        private CadAnchor[,] TemplatePoint;
        private ProjectorMesh _mesh;

        public CreateGridWindow(ProjectorMesh mesh)
        {
            InitializeComponent();
            this._mesh = mesh;
            this.DataContext = mesh;
            TemplatePoint = mesh.Points;
        }


        private void StepUpDn_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double?> e)
        {
            if (this.IsLoaded)
            {
                int ColumnCount = (int)(this._mesh.Size.Width / StepUpDn.Value.Value) + 1;
                int StrokeCOunt = (int)(this._mesh.Size.Height / StepUpDn.Value.Value) + 1;
                WidthStepLabel.Content = "(" + Math.Round(this._mesh.Size.Width / Width, 1) + ")";
                HeightStepLabel.Content = "(" + Math.Round(this._mesh.Size.Height / Height, 1) + ")";

                _mesh.ColumnCount = ColumnCount;
                _mesh.StrokeCount = StrokeCOunt;
            }
        }


        protected override void OnClosing(CancelEventArgs e)
        {
            base.OnClosing(e);
            MessageBoxResult messageBoxResult = MessageBox.Show("Сохранить сетку?", "Внимание", MessageBoxButton.YesNoCancel, MessageBoxImage.Warning);
            switch (messageBoxResult)
            {
                case MessageBoxResult.Yes:
                    break;
                case MessageBoxResult.No:
                    this._mesh.Points = TemplatePoint;
                    break;
                case MessageBoxResult.Cancel:
                    e.Cancel = true;
                    break;
            }
        }

        private void NumericUpDown_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double?> e)
        {
            if (DataContext is ProjectorMesh mesh)
            {
                mesh.CalculationMorph();
            }
        }

        //public ICommand SelectNextCommand => _mesh != null ? new ActionCommand(_mesh.SelectNext) : null;
    }
    public class ToSceneObjConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var scene = new ProjectionScene() { Size = new CadRect3D(1000, 1000, 0) };
            if (value is UidObject obj)
            {
                scene.Size.Width = obj.Bounds.Width;
                scene.Size.Height = obj.Bounds.Height;
                scene.Add(obj);
            }
            return new SceneModel(scene);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

}
