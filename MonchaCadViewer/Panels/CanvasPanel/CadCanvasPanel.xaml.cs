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

namespace CadProjectorViewer.Panels.CanvasPanel
{
    /// <summary>
    /// Логика взаимодействия для CadCanvasPanel.xaml
    /// </summary>
    public partial class CadCanvasPanel : UserControl
    {
        public CadSize3D Size => ProjectorHub.Size;

        private Point StartMovePoint;
        private Point StartMousePoint;
        private bool WasMove = false;

        private Visibility _showadorner = Visibility.Hidden;

        private CanvasObj.ProjectionScene projectionScene => (CanvasObj.ProjectionScene)this.DataContext;


        public MouseAction MouseAction
        {
            get => this.mouseAction;
            set
            {
                mouseAction = value;
                Console.WriteLine(value.ToString());
                switch (value)
                {
                    case MouseAction.NoAction:
                        this.Cursor = Cursors.Arrow;
                        this.CaptureMouse();
                        this.ReleaseMouseCapture();
                        this.projectionScene.Cancel(); 
                        break;
                    case MouseAction.MoveCanvas:
                        this.Cursor = Cursors.SizeAll;
                        break;
                    case MouseAction.Line:
                    case MouseAction.Rectangle:
                    case MouseAction.Mask:
                        this.Cursor = Cursors.Cross;
                        break;
                };
            }
        }
        private MouseAction mouseAction = MouseAction.NoAction;

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

        public event EventHandler<String> Logging;

        /// <summary>
        /// Orientation flag for SelecNext void
        /// </summary>
        public bool InverseSelectFlag = false;

        public CadCanvasPanel()
        {
            InitializeComponent();
            this.DataContext = new CanvasObj.ProjectionScene();
            UpdateTransform(null, true);
        }


        private void CanvasGrid_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (this.projectionScene.ActiveDrawingObject == null)
            {
                this.MouseAction = MouseAction.NoAction;
            }
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

        private void CanvasGrid_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is IInputElement inputElement)
            {
                if (this.projectionScene.ActiveDrawingObject != null)
                {
                    this.projectionScene.ActiveDrawingObject.Init();
                    this.projectionScene.ActiveDrawingObject = null;
                }

                if (Keyboard.Modifiers == ModifierKeys.Control)
                {
                    this.StartMousePoint = e.GetPosition(CanvasBox);
                    this.StartMovePoint = new Point(this.Translate.X, this.Translate.Y);
                }
                else if (this.MouseAction == MouseAction.Rectangle)
                {
                    Point point = e.GetPosition(CanvasGrid);
                    CanvasObj.CadRectangle cadRectangle = new CanvasObj.CadRectangle(new CadPoint3D(point, ProjectorHub.Size), new CadPoint3D(point, ProjectorHub.Size), string.Empty, true);
                    this.projectionScene.Add(cadRectangle);
                }
                else if (this.mouseAction == MouseAction.Mask)
                {
                    this.MouseAction = MouseAction.NoAction;
                    CadSize3D lRect = new CadSize3D(new CadPoint3D(e.GetPosition(inputElement), ProjectorHub.Size, true), new CadPoint3D(e.GetPosition(inputElement), ProjectorHub.Size, true));
                    CanvasObj.CadRectangle Maskrectangle = new CanvasObj.CadRectangle(lRect, $"Mask_{this.projectionScene.Masks.Count}") { Render = false };
                    this.projectionScene.Add(Maskrectangle);
                    this.projectionScene.Masks.Add(Maskrectangle);
                    this.projectionScene.ActiveDrawingObject = Maskrectangle;
                }
                else if (this.mouseAction == MouseAction.Line)
                {
                    CanvasObj.CadLine line = new CanvasObj.CadLine(new CadPoint3D(e.GetPosition(inputElement)), new CadPoint3D(e.GetPosition(inputElement)), true);
                    this.projectionScene.Add(line);
                    this.projectionScene.ActiveDrawingObject = line;
                }
            }

        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed && Keyboard.Modifiers == ModifierKeys.Control)
            {
                this.MouseAction = MouseAction.MoveCanvas;
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


        public void MoveCanvasSet(double left, double top)
        {
            for (int i = 0; i < this.projectionScene.Objects.Count; i++)
            {
                if (this.projectionScene.Objects[i] is CanvasObj.CadObject cadObject)
                {
                    if (cadObject.IsSelected == true && cadObject.IsFix == false)
                    {
                        cadObject.X += left;
                        cadObject.Y += top;
                    }
                }
            }
        }

        public void FixPosition()
        {
            for (int i = 0; i < projectionScene.Objects.Count; i++)
            {
                if (projectionScene.Objects[i] is CanvasObj.CadObject cadObject1)
                {
                    if (cadObject1.IsSelected)
                    {
                        cadObject1.IsFix = !cadObject1.IsFix;
                    }
                }
            }

        }

        public void SelectNext()
        {
            for (int i = 0; i < projectionScene.Objects.Count; i++)
            {
                if (projectionScene.Objects[i] is CanvasObj.CadObject cadObject)
                {
                    if (cadObject.IsSelected)
                    {
                        cadObject.IsFix = true;
                        cadObject.IsSelected = false;

                        try
                        {
                            if (projectionScene.Objects[i + (InverseSelectFlag ? -1 : +1)] is CanvasObj.CadObject cadObject2)
                            {
                                cadObject2.IsSelected = true;
                                cadObject2.IsFix = false;
                            }
                        }
                        catch (Exception exeption)
                        {
                            Logging?.Invoke(this, ($"Main: {exeption.Message}"));
                            InverseSelectFlag = !InverseSelectFlag;
                            cadObject.IsSelected = true;
                        }
                        break;
                    }
                }
            }

        }


        private void Canvas_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (sender is Canvas canvas)
            {
                if (canvas.Background is DrawingBrush drawingBrush)
                {
                    double cell = (int)(Math.Min(ProjectorHub.Size.X, ProjectorHub.Size.Y) / 10);
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

    public enum MouseAction
    {
        NoAction,
        Rectangle,
        Line,
        Circle,
        MoveCanvas,
        Mask
    }
}
