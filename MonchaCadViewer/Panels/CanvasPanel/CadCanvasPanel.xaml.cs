using CadProjectorViewer.CanvasObj;
using CadProjectorSDK;
using CadProjectorSDK.Device;
using CadProjectorSDK.CadObjects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using AppSt = CadProjectorViewer.Properties.Settings;
using System.Globalization;
using CadProjectorSDK.CadObjects.Abstract;
using CadProjectorSDK.Device.Mesh;
using CadProjectorSDK.Scenes;
using CadProjectorViewer.StaticTools;
using System.IO;
using CadProjectorSDK.Interfaces;
using System.Collections.ObjectModel;
using CadProjectorSDK.Tools;
using Microsoft.Xaml.Behaviors.Core;
using CadProjectorSDK.Scenes.Actions;
using CadProjectorSDK.CadObjects.Interfaces;
using CadProjectorSDK.Render;

namespace CadProjectorViewer.Panels.CanvasPanel
{
    /// <summary>
    /// Логика взаимодействия для CadCanvasPanel.xaml
    /// </summary>
    public partial class CadCanvasPanel : UserControl
    {
        public virtual ChangeSizeDelegate SizeChange { get; set; }
        public delegate void ChangeSizeDelegate();

        private Point StartMovePoint;
        private Point StartMousePoint;
        private bool WasMove = false;

        private Visibility _showadorner = Visibility.Hidden;

        private ProjectionScene SelectedScene => (ProjectionScene)this.DataContext;

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

        protected override void OnMouseWheel(MouseWheelEventArgs e)
        {
            if (Keyboard.Modifiers == ModifierKeys.Control)
            {
                Point centre = e.GetPosition(CanvasBox);
                this.Scale.CenterX = centre.X;
                this.Scale.CenterY = centre.Y;
                if (this.Scale.ScaleY + (double)e.Delta / 5000 > 1)
                {
                    this.Scale.ScaleX += (double)e.Delta / 3000;
                    this.Scale.ScaleY += (double)e.Delta / 3000;
                }
                else
                {
                    this.Scale.ScaleX = 1;
                    this.Scale.ScaleY = 1;
                    this.X = 0;
                    this.Y = 0;
                }
                CanvasBox.InvalidateVisual();
                SizeChange?.Invoke();
            }
        }


        public void UpdateTransform(TransformGroup transformGroup, bool ResetPosition)
        {
            if (transformGroup != null)
            {
                CanvasGrid.RenderTransform = TransformGroup;
                this.Scale = this.TransformGroup.Children[0] != null ? (ScaleTransform)this.TransformGroup.Children[0] : new ScaleTransform();
                this.Rotate = this.TransformGroup.Children[1] != null ? (RotateTransform)this.TransformGroup.Children[1] : new RotateTransform();
                this.Translate = this.TransformGroup.Children[2] != null ? (TranslateTransform)this.TransformGroup.Children[2] : new TranslateTransform();
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
            else if (this.SelectedScene.AlreadyAction != null)
            {
                Point point = e.GetPosition(this.CanvasGrid);
                this.SelectedScene.AlreadyAction.NextAction(new CadPoint3D(point));
            }
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            if (SelectedScene != null)
            {
                Point m_point = e.GetPosition(this.CanvasGrid);
                SelectedScene.MousePosition.MX = m_point.X;
                SelectedScene.MousePosition.MY = m_point.Y;

                if (e.LeftButton == MouseButtonState.Pressed && Keyboard.Modifiers == ModifierKeys.Control)
                {
                    this.WasMove = true;
                    Point tPoint = e.GetPosition(CanvasBox);

                    //double prop = Math.Min(CanvasGrid.ActualWidth / CanvasGrid.ActualWidth, CanvasGrid.ActualHeight / CanvasGrid.ActualHeight);

                    this.X = (this.StartMovePoint.X + (tPoint.X - this.StartMousePoint.X)) / this.Scale.ScaleX;
                    this.Y = (this.StartMovePoint.Y + (tPoint.Y - this.StartMousePoint.Y)) / this.Scale.ScaleY;

                    this.CaptureMouse();
                }
                else if (SelectedScene.AlreadyAction != null)
                {
                    if (this.SelectedScene.AlreadyAction.CanAction == true)
                    {
                        this.SelectedScene.AlreadyAction.Run(SelectedScene.MousePosition);
                    }
                }

            }
        }


       
        private void Canvas_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (sender is Canvas canvas && SelectedScene != null)
            {
                if (canvas.Background is DrawingBrush drawingBrush)
                {
                    double cell = (int)(Math.Min(SelectedScene.Size.Width, SelectedScene.Size.Height) / 10);
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
            Random rnd = new Random();
            foreach (LProjector monchaDevice in this.SelectedScene.Projectors)
            {
                SolidColorBrush ColorBrush = new SolidColorBrush();
                ColorBrush.Color = Colors.Azure;
                SelectedScene.Clear();
                SelectedScene.Add(monchaDevice.Size);

                foreach (ProjectorMesh mesh in monchaDevice.SelectedMeshes)
                {
                    SelectedScene.Add(mesh.Size);
                }
            }
        }


        public ICommand RefreshFrameCommand => new ActionCommand(() => {
            SelectedScene.Projectors.RefreshDevices();
        });
        public ICommand CancelActionCommand => new ActionCommand(() => {
            SelectedScene.Break();
        });
        public ICommand CancelSizeChange => new ActionCommand(() => {
            this.Scale.ScaleX = 1;
            this.Scale.ScaleY = 1;
        });

        public double Thinkess()
        {
            return Math.Max(CanvasGrid.Width, CanvasGrid.Height) / this.Scale.ScaleX * 0.01;
        }

    }

    public class CursorActionConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null) return Cursors.Arrow;
            else return Cursors.Cross;
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
                result = 1 / dvalue * 100;
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
