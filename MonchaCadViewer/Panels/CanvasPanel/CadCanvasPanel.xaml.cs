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

namespace CadProjectorViewer.Panels.CanvasPanel
{
    /// <summary>
    /// Логика взаимодействия для CadCanvasPanel.xaml
    /// </summary>
    public partial class CadCanvasPanel : UserControl
    {
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

        protected override void OnMouseLeftButtonUp(MouseButtonEventArgs e)
        {
            base.OnMouseLeftButtonUp(e);
            if (this.SelectedScene.ActiveDrawingObject == null)
            {
                this.SelectedScene.SceneAction = SceneAction.NoAction;
            }
            this.ReleaseMouseCapture();
        }

        protected override void OnMouseWheel(MouseWheelEventArgs e)
        {
            if (Keyboard.Modifiers == ModifierKeys.Control)
            {
                Point centre = e.GetPosition(CanvasGrid);
                this.Scale.CenterX = centre.X;
                this.Scale.CenterY = centre.Y;
                if (this.Scale.ScaleY + (double)e.Delta / 1000 > 1)
                {
                    this.Scale.ScaleX += (double)e.Delta / 1000;
                    this.Scale.ScaleY += (double)e.Delta / 1000;
                }
                else
                {
                    this.Scale.ScaleX = 1;
                    this.Scale.ScaleY = 1;
                    this.X = 0;
                    this.Y = 0;
                }
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
            CanvasGrid.RenderTransform = this.TransformGroup;
        }

        public TransformGroup TransformGroup { get; set; }
        public ScaleTransform Scale { get; set; }
        public RotateTransform Rotate { get; set; }
        public TranslateTransform Translate { get; set; }



        protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
        {
            base.OnMouseLeftButtonDown(e);
            if (this.SelectedScene.ActiveDrawingObject != null)
            {
                this.SelectedScene.ActiveDrawingObject.Init();
                this.SelectedScene.ActiveDrawingObject = null;
            }

            if (Keyboard.Modifiers == ModifierKeys.Control)
            {
                this.StartMousePoint = e.GetPosition(this.CanvasGrid);
                this.StartMovePoint = new Point(this.Translate.X, this.Translate.Y);
            }
            else if (this.SelectedScene.SceneAction == SceneAction.Rectangle)
            {
                Point point = e.GetPosition(CanvasGrid);
                CadRect3D cadRectangle = new CadRect3D(new CadPoint3D(point, SelectedScene.Size), new CadPoint3D(point, SelectedScene.Size), false, string.Empty);
                this.SelectedScene.Add(cadRectangle);
            }
            else if (this.SelectedScene.SceneAction == SceneAction.Mask)
            {
                this.SelectedScene.SceneAction = SceneAction.NoAction;
                CadRect3D lRect = new CadRect3D(false)
                {
                    P1 = new CadPoint3D(e.GetPosition(this.CanvasGrid), SelectedScene.Size, true),
                    P2 = new CadPoint3D(e.GetPosition(this.CanvasGrid), SelectedScene.Size, true),
                    NameID = "Mask",
                    ShowName = true,
                    IsRender = false,
                };
                this.SelectedScene.Add(lRect);
                this.SelectedScene.Masks.Add(lRect);
                this.SelectedScene.ActiveDrawingObject = lRect;
            }
            else if (this.SelectedScene.SceneAction == SceneAction.Line)
            {
                Point point = e.GetPosition(this.CanvasGrid);
                CadLine line = new CadLine(new CadPoint3D(point), new CadPoint3D(point), true);
                this.SelectedScene.Add(line);
                this.SelectedScene.ActiveDrawingObject = line;
            }
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            if (SelectedScene != null)
            {
                SelectedScene.MousePosition = new CadPoint3D(e.GetPosition(this.CanvasGrid));

                if (e.LeftButton == MouseButtonState.Pressed && Keyboard.Modifiers == ModifierKeys.Control)
                {
                    this.SelectedScene.SceneAction = SceneAction.MoveCanvas;
                    this.WasMove = true;
                    Point tPoint = e.GetPosition(CanvasBox);

                    double prop = Math.Min(CanvasGrid.ActualWidth / CanvasGrid.ActualWidth, CanvasGrid.ActualHeight / CanvasGrid.ActualHeight);

                    Translate.X = (this.StartMovePoint.X + (tPoint.X - this.StartMousePoint.X)) * prop;
                    Translate.Y = (this.StartMovePoint.Y + (tPoint.Y - this.StartMousePoint.Y)) * prop;

                    this.CaptureMouse();
                }
                else
                {
                    if (SelectedScene.ActiveDrawingObject != null)
                    {
                        SelectedScene.ActiveDrawingObject.SetTwoPoint(e.GetPosition(this.CanvasGrid));
                    }
                }
            }
        }

        private void Canvas_MouseMove(object sender, MouseEventArgs e)
        {
            Point tempPoint = e.GetPosition(CanvasGrid);
            SelectedScene.MousePosition.X = tempPoint.X;
            SelectedScene.MousePosition.Y = tempPoint.Y;
            CoordinateLabel.Content =
                $"X: { Math.Round(tempPoint.X, 2) }; Y:{ Math.Round(tempPoint.Y, 2) }";
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
            foreach (LDevice monchaDevice in this.SelectedScene.Devices)
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

        private void RefreshBtn_Click(object sender, RoutedEventArgs e)
        {
            SelectedScene.Refresh();
        }
    }

    public class CadObjectConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is UidObject uidObject)
            {
                if (uidObject is ProjectorMesh mesh) return new CanvasMesh(mesh);
                else if (uidObject is CadLine cadLine) return new CanvasLine(cadLine);
                else if (uidObject is CadRect3D cadRectangle) return new CanvasRectangle(cadRectangle, cadRectangle.NameID);
                else if (uidObject is IGeometryObject geometry) return new GeometryPreview(uidObject);
                else if (uidObject is IPixelObject pixelObject) return new ImagePreview(uidObject);
            }
            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return null;
        }
    }

    public class CursorActionConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is SceneAction action)
            {
                switch (value)
                {
                    case SceneAction.NoAction:
                        return Cursors.Arrow;
                        break;
                    case SceneAction.MoveCanvas:
                        return Cursors.SizeAll;
                        break;
                    case SceneAction.Line:
                    case SceneAction.Rectangle:
                    case SceneAction.Mask:
                        return Cursors.Cross;
                        break;
                };
            }
            return Cursors.Arrow;
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
                if (values[2] is ObservableCollection<LDevice> devices)
                {
                    foreach (LDevice lDevice in devices)
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
