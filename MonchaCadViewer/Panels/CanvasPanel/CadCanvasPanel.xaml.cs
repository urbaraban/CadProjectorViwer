using CadProjectorSDK.Device;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using AppSt = CadProjectorViewer.Properties.Settings;
using System.Globalization;
using CadProjectorSDK.Device.Mesh;
using CadProjectorSDK.Scenes;
using System.Collections.ObjectModel;
using CadProjectorSDK.Tools;
using Microsoft.Xaml.Behaviors.Core;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using CadProjectorViewer.ViewModel;

namespace CadProjectorViewer.Panels.CanvasPanel
{
    /// <summary>
    /// Логика взаимодействия для CadCanvasPanel.xaml
    /// </summary>
    public partial class CadCanvasPanel : UserControl, INotifyPropertyChanged
    {
        public virtual ChangeSizeDelegate SizeChange { get; set; }
        public delegate void ChangeSizeDelegate();

        private Point StartMovePoint;
        private Point StartMousePoint;

        private RenderDeviceModel ViewModel => (RenderDeviceModel)this.DataContext;

        private double X
        {
            get => this.Translate.X;
            set => this.Translate.X = value;
        }
        private double Y
        {
            get => this.Translate.Y;
            set => this.Translate.Y = value;
        }


        public CadCanvasPanel()
        {
            InitializeComponent();
            UpdateTransform(null, true);
        }


        public void UpdateTransform(TransformGroup transformGroup, bool ResetPosition)
        {
            if (transformGroup != null)
            {
                CanvasGrid.RenderTransform = TransformGroup;
                this.Scale = this.TransformGroup.Children[0] != null ? 
                    (ScaleTransform)this.TransformGroup.Children[0] : new ScaleTransform();

                this.Rotate = this.TransformGroup.Children[1] != null ? 
                    (RotateTransform)this.TransformGroup.Children[1] : new RotateTransform();

                this.Translate = this.TransformGroup.Children[2] != null ? 
                    (TranslateTransform)this.TransformGroup.Children[2] : new TranslateTransform();
            }
            else ResetTransform();
        }

        public void ResetTransform()
        {
            this.TransformGroup = new TransformGroup()
            {
                Children = new TransformCollection()
                    {
                        new ScaleTransform(),
                        new RotateTransform(),
                        new TranslateTransform()
                    }
            };
            this.Scale = (ScaleTransform)this.TransformGroup.Children[0];
            this.Rotate = (RotateTransform)this.TransformGroup.Children[1];
            this.Translate = (TranslateTransform)this.TransformGroup.Children[2];
            CanvasBox.RenderTransform = this.TransformGroup;
        }

        public TransformGroup TransformGroup { get; set; }
        
        public double ScaleValue
        {
            get => Scale != null ? Scale.ScaleX : 1;
            set
            {
                if (Scale != null)
                {
                    Scale.ScaleX = value;
                    Scale.ScaleY = value;
                    OnPropertyChanged(nameof(ScaleValue));
                }
            }
        }
        public ScaleTransform Scale { get; set; }

        public RotateTransform Rotate { get; set; }
        public TranslateTransform Translate { get; set; }


        protected override void OnMouseLeftButtonUp(MouseButtonEventArgs e)
        {
            base.OnMouseLeftButtonUp(e);

            this.ReleaseMouseCapture();
        }

        protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
        {
            base.OnMouseLeftButtonDown(e);

            if (Keyboard.Modifiers == ModifierKeys.Control)
            {
                this.StartMousePoint = e.GetPosition(this.CanvasBox);
                this.StartMovePoint = new Point(this.Translate.X, this.Translate.Y);
            }
            else if (ViewModel.RenderingDisplay is ProjectionScene scene &&
                scene.AlreadyAction != null)
            {
                Point point = e.GetPosition(this.CanvasGrid);
                if (scene.AlreadyAction.CanAction == false)
                {
                    scene.AlreadyAction.Run(point);
                }
                else
                {
                    scene.AlreadyAction.NextAction(point);
                }

            }
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            if (ViewModel.RenderingDisplay is ProjectionScene scene)
            {
                Point m_point = e.GetPosition(this.CanvasGrid);
                scene.MousePosition.MX = m_point.X;
                scene.MousePosition.MY = m_point.Y;

                if (e.LeftButton == MouseButtonState.Pressed && Keyboard.Modifiers == ModifierKeys.Control)
                {
                    Point tPoint = e.GetPosition(CanvasBox);

                    //double prop = Math.Min(CanvasGrid.ActualWidth / CanvasGrid.ActualWidth, CanvasGrid.ActualHeight / CanvasGrid.ActualHeight);

                    this.X = (this.StartMovePoint.X + (tPoint.X - this.StartMousePoint.X)) / this.ScaleValue;
                    this.Y = (this.StartMovePoint.Y + (tPoint.Y - this.StartMousePoint.Y)) / this.ScaleValue;

                    this.CaptureMouse();
                }
                else if (scene.AlreadyAction != null)
                {
                    if (scene.AlreadyAction.CanAction == true)
                    {
                        scene.AlreadyAction.Run(m_point);
                    }
                }
            }
        }

        protected override void OnMouseWheel(MouseWheelEventArgs e)
        {
            if (Keyboard.Modifiers == ModifierKeys.Control)
            {
                Point centre = e.GetPosition(CanvasBox);
                this.Scale.CenterX = centre.X;
                this.Scale.CenterY = centre.Y;
                if (this.Scale.ScaleY + (double)e.Delta / 5000 > 1)
                {
                    this.ScaleValue += (double)e.Delta / 3000;
                }
                else
                {
                    this.ScaleValue = 1;
                    this.X = 0;
                    this.Y = 0;
                }
                CanvasBox.InvalidateVisual();
                SizeChange?.Invoke();
            }
        }

        private void Canvas_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (sender is Canvas canvas && ViewModel.RenderingDisplay is ProjectionScene scene)
            {
                if (canvas.Background is DrawingBrush drawingBrush)
                {
                    double cell = (int)(Math.Min(scene.Size.Width, scene.Size.Height) / 10);
                    drawingBrush.Viewport = new Rect(0, 0, cell, cell);
                }
               
            }
        }

        private void AdornerShowBtn_Click(object sender, RoutedEventArgs e)
        {
            CoordinateLabel.Visibility = CoordinateLabel.Visibility == Visibility.Visible ? Visibility.Hidden : Visibility.Visible;
        }

        private void ShowDeviceRect_Click(object sender, RoutedEventArgs e)
        {
            if (ViewModel.RenderingDisplay is ProjectionScene scene)
            {
                Random rnd = new Random();
                foreach (LProjector monchaDevice in scene.Projectors)
                {
                    SolidColorBrush ColorBrush = new SolidColorBrush();
                    ColorBrush.Color = Colors.Azure;
                    scene.Clear();
                    scene.Add(monchaDevice.Size);

                    foreach (ProjectorMesh mesh in monchaDevice.SelectedMeshes)
                    {
                        scene.Add(mesh.Size);
                    }
                }
            }

        }

        public ICommand RefreshFrameCommand => new ActionCommand(() => {
            if (ViewModel.RenderingDisplay is ProjectionScene scene)
            {
                scene.RefreshScene();
            }
        });
        public ICommand CancelActionCommand => new ActionCommand(() => {
            if (ViewModel.RenderingDisplay is ProjectionScene scene)
            {
                scene.Break();
            }
        });
        public ICommand CancelSizeChange => new ActionCommand(() => {
            this.ScaleValue = 1;
        });

        public double Thinkess()
        {
            return Math.Max(CanvasGrid.Width, CanvasGrid.Height) / this.Scale.ScaleX * 0.01;
        }

        #region INotifyPropertyChanged
        public event PropertyChangedEventHandler PropertyChanged;

        public async void OnPropertyChanged([CallerMemberName] string prop = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop));
        }
        #endregion
    }

    public class CursorActionConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value != null) 
                return Cursors.Cross;
            return Cursors.Arrow;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class ScaleConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            double result = 0;
            if (value is double dvalue)
                result = dvalue / 1 * 100;
            return Math.Round(result, 0);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class InfoConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                string outstring = $"X: {Math.Round((double)values[0], 1)} Y:{Math.Round((double)values[1])}";
                if (values[2] is ObservableCollection<LProjector> devices)
                {
                    foreach (LProjector lDevice in devices)
                    {
                        if (lDevice.RenderObjects.Count > 0)
                        {
                            outstring += $"\n {lDevice.NameID}: {LFrameConverter.GetAlreadyScan(lDevice.RenderObjects) * lDevice.FPS * 1.3} pts";
                        }
                    }
                }

                return outstring;
            }
            catch
            {
                return string.Empty;
            }
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
