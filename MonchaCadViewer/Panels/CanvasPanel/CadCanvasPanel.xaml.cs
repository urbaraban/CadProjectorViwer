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
using CadProjectorSDK.CadObjects.Interface;
using CadProjectorSDK.Device.Mesh;
using CadProjectorSDK.Scenes;

namespace CadProjectorViewer.Panels.CanvasPanel
{
    /// <summary>
    /// Логика взаимодействия для CadCanvasPanel.xaml
    /// </summary>
    public partial class CadCanvasPanel : UserControl
    {
        public CadRect3D Size => ProjectorHub.Size;

        private Point StartMovePoint;
        private Point StartMousePoint;
        private bool WasMove = false;

        private Visibility _showadorner = Visibility.Hidden;

        private ProjectionScene projectionScene => (ProjectionScene)this.DataContext;

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
            this.DataContext = new ProjectionScene();
            UpdateTransform(null, true);
        }

        protected override void OnMouseLeftButtonUp(MouseButtonEventArgs e)
        {
            base.OnMouseLeftButtonUp(e);
            if (this.projectionScene.ActiveDrawingObject == null)
            {
                this.projectionScene.SceneAction = SceneAction.NoAction;
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
            if (this.projectionScene.ActiveDrawingObject != null)
            {
                this.projectionScene.ActiveDrawingObject.Init();
                this.projectionScene.ActiveDrawingObject = null;
            }

            if (Keyboard.Modifiers == ModifierKeys.Control)
            {
                this.StartMousePoint = e.GetPosition(this.CanvasBox);
                this.StartMovePoint = new Point(this.Translate.X, this.Translate.Y);
            }
            else if (this.projectionScene.SceneAction == SceneAction.Rectangle)
            {
                Point point = e.GetPosition(CanvasGrid);
                CadRect3D cadRectangle = new CadRect3D(new CadPoint3D(point, ProjectorHub.Size), new CadPoint3D(point, ProjectorHub.Size), string.Empty);
                this.projectionScene.Add(cadRectangle);
            }
            else if (this.projectionScene.SceneAction == SceneAction.Mask)
            {
                this.projectionScene.SceneAction = SceneAction.NoAction;
                CadRect3D lRect = new CadRect3D()
                {
                    P1 = new CadPoint3D(e.GetPosition(this.CanvasGrid), ProjectorHub.Size, true),
                    P2 = new CadPoint3D(e.GetPosition(this.CanvasGrid), ProjectorHub.Size, true),
                    NameID = "Mask",
                    ShowName = true,
                    Render = false
                };
                this.projectionScene.Add(lRect);
                this.projectionScene.Masks.Add(lRect);
                this.projectionScene.ActiveDrawingObject = lRect;
            }
            else if (this.projectionScene.SceneAction == SceneAction.Line)
            {
                CadLine line = new CadLine(new CadPoint3D(e.GetPosition(this.CanvasGrid)), new CadPoint3D(e.GetPosition(this.CanvasGrid)), true);
                this.projectionScene.Add(line);
                this.projectionScene.ActiveDrawingObject = line;
            }
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            projectionScene.MousePosition = new CadPoint3D(e.GetPosition(this.CanvasGrid));

            if (e.LeftButton == MouseButtonState.Pressed && Keyboard.Modifiers == ModifierKeys.Control)
            {
                this.projectionScene.SceneAction = SceneAction.MoveCanvas;
                this.WasMove = true;
                Point tPoint = e.GetPosition(CanvasBox);

                double prop = Math.Min(CanvasGrid.ActualWidth / CanvasBox.ActualWidth, CanvasGrid.ActualHeight / CanvasBox.ActualHeight);

                Translate.X = (this.StartMovePoint.X + (tPoint.X - this.StartMousePoint.X)) * prop;
                Translate.Y = (this.StartMovePoint.Y + (tPoint.Y - this.StartMousePoint.Y)) * prop;

                this.CaptureMouse();
            }
            else if (projectionScene != null)
            {
                if (projectionScene.ActiveDrawingObject != null)
                {
                    projectionScene.ActiveDrawingObject.SetTwoPoint(e.GetPosition(this.CanvasGrid));
                }
            }
        }


        private void Canvas_MouseMove(object sender, MouseEventArgs e)
        {
            Point tempPoint = e.GetPosition(CanvasGrid);
            projectionScene.MousePosition.X = tempPoint.X;
            projectionScene.MousePosition.Y = tempPoint.Y;
            CoordinateLabel.Content =
                $"X: { Math.Round(tempPoint.X, 2) }; Y:{ Math.Round(tempPoint.Y, 2) }";
        }

       
        private void Canvas_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (sender is Canvas canvas)
            {
                if (canvas.Background is DrawingBrush drawingBrush)
                {
                    double cell = (int)(Math.Min(ProjectorHub.Size.Width, ProjectorHub.Size.Height) / 10);
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
            /*foreach (MonchaDevice monchaDevice in ProjectorHub.Devices)
            {
                SolidColorBrush ColorBrush = new SolidColorBrush();
                ColorBrush.Color = Colors.Azure;
                this.projectionScene.Clear();
                this.projectionScene.Add(new CadRectangle(monchaDevice.Size, monchaDevice.HWIdentifier, false)
                { BackColorBrush = new SolidColorBrush()
                    { Color = Color.FromArgb(100, (byte)rnd.Next(256), (byte)rnd.Next(256), (byte)rnd.Next(256)) }
                });

                foreach (LDeviceMesh mesh in monchaDevice.SelectedMeshes)
                {
                    this.projectionScene.Add(
                        new CadRectangle(mesh.Size, $"{monchaDevice.HWIdentifier} - {mesh.Name}", false)
                        {
                            BackColorBrush = new SolidColorBrush()
                            { Color = Color.FromArgb(100, (byte)rnd.Next(256), (byte)rnd.Next(256), (byte)rnd.Next(256)) }
                        });
                }
            }*/
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
}
