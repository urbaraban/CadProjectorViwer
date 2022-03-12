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
using System.Windows.Input;
using Microsoft.Xaml.Behaviors.Core;

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

        public ICommand SelectNextCommand => _mesh != null ? new ActionCommand(_mesh.SelectNext) : null;

        private void NumericUpDown_GotFocus(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement element && element.DataContext is DoubleValue CorrectValue)
            {
                if (_mesh != null && _mesh.SelectedPoint != null)
                {
                    Tuple<int, int> index = _mesh.IndexOf(_mesh.SelectedPoint);
                    int WidthIndex = Array.IndexOf(_mesh.ColumnCorrect, CorrectValue);
                    int HeightIndex = Array.IndexOf(_mesh.StrokeCorrect, CorrectValue);
                   // _mesh.Points[HeightIndex > -1 ? HeightIndex : index.Item1, WidthIndex > -1 ? WidthIndex : index.Item2].Select(true);
                }
            }
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
