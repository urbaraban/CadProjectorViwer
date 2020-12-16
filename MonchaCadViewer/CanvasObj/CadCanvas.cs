using MonchaSDK;
using MonchaSDK.Device;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using MonchaSDK.Object;
using System.Windows.Shapes;
using System.ComponentModel;

namespace MonchaCadViewer.CanvasObj
{
    public class CadCanvas : Canvas
    {
        public event EventHandler<bool> SelectedObject;
        //public event EventHandler<string> ErrorMessageEvent;

        private object MouseOnObject = null;

        private bool _wasmove = false;
        private LPoint3D _size;
        private List<CadDot> anchors = new List<CadDot>();
        private int _status = 0;
        private bool _maincanvas;
        private bool _nofreecursor = true;


        private Rectangle _selectedRectangle = new Rectangle();

        public bool HorizontalMesh { get; set; } = false;

        public Point LastMouseDownPosition = new Point();

        public int Status
        {
            get => _status;
            set => _status = value;
        }

        public CadCanvas(LPoint3D Size, bool MainCanvas)
        {
            this._size = Size;
            this._maincanvas = MainCanvas;

            this.Name = "CCanvas";
            this.Background = Brushes.Transparent; //backBrush;
            this.Width = this._size.GetMPoint.X;
            this.Height = this._size.GetMPoint.Y;
            this.Focusable = false;

            this._selectedRectangle.Fill = Brushes.Transparent;
            this._selectedRectangle.Stroke = Brushes.DimGray;
            this._selectedRectangle.StrokeThickness = MonchaHub.GetThinkess() / 5;
            this._selectedRectangle.Visibility = Visibility.Hidden;
            this.Children.Add(this._selectedRectangle);

            this._size.PropertyChanged += _size_ChangePoint;
            this._size.M.PropertyChanged += _size_ChangePoint;

            this.ContextMenuClosing += CadCanvas_ContextMenuClosing;


            if (this._maincanvas)
            {
                this.MouseLeftButtonDown += Canvas_MouseLeftDown;
                this.MouseMove += CadCanvas_MouseMove;
                this.MouseUp += CadCanvas_MouseUp;
            }

            MonchaHub.ChangeSize += MonchaHub_ChangeSize;
        }

        public void UpdateProjection(bool force)
        {
            if ((this._maincanvas == true && SendProcessor.Processing == false) || force == true)
            {
                SendProcessor.Worker(this);
            }
        }

        private void MonchaHub_ChangeSize(object sender, LPoint3D e)
        {
            ResizeCanvas();
        }

        private void _size_ChangePoint(object sender, PropertyChangedEventArgs e)
        {
            ResizeCanvas();
        }

        public void ResizeCanvas()
        {
            if (this._size.X != 0 && this._size.Y != 0 && this._size.Z != 0 && this._size.M.X != 0)
            {
                if (this.Parent is Viewbox viewbox)
                {
                    viewbox.Width = this._size.GetMPoint.X;
                    viewbox.Height = this._size.GetMPoint.Y;
                }
                this.Width = this._size.GetMPoint.X;
                this.Height = this._size.GetMPoint.Y;
            }
        }

        public void DrawContour(Shape obj, bool maincanvas, bool add, bool mousemove)
        {
            if (add == false)
            {
                this.Clear();
            }

            if (obj is CadObject cadObject)
            {
                cadObject.MouseMove += CadObject_MouseMove;
                cadObject.MouseLeave += CadObject_MouseLeave;
            }

            this.Add(obj);

            if (maincanvas == true)
            {
                SelectedObject?.Invoke(null, false);
            }
        }

        private void CadObject_MouseLeave(object sender, MouseEventArgs e)
        {
            if (this.MouseOnObject == sender)
            {
                this.MouseOnObject = null;
            }
        }

        private void CadObject_MouseMove(object sender, MouseEventArgs e)
        {
            this.MouseOnObject = sender;
        }


        public void DrawRectangle(LPoint3D point1, LPoint3D point2)
        {
            CadDot cadDot1 = new CadDot(point1, MonchaHub.GetThinkess() * 3, true, true, false);
            cadDot1.Render = false;
            CadDot cadDot2 = new CadDot(point2, MonchaHub.GetThinkess() * 3, true, true, false);
            cadDot2.Render = false;
            this.Children.Add(cadDot1);
            this.Children.Add(cadDot2);

        }



        private void Canvas_MouseLeftDown(object sender, MouseButtonEventArgs e)
        {
            if (this._nofreecursor == false)
            {
                switch (_status)
                {
                    case 1:

                        break;
                    default:
                        if (this.MouseOnObject == null)
                        {
                            this.ClearSelectedObject(null);
                            LastMouseDownPosition = e.GetPosition(this);
                            Canvas.SetLeft(this._selectedRectangle, LastMouseDownPosition.X);
                            Canvas.SetTop(this._selectedRectangle, LastMouseDownPosition.Y);
                            this._selectedRectangle.Width = 0;
                            this._selectedRectangle.Height = 0;
                            this._selectedRectangle.Visibility = Visibility.Visible;
                        }
                        break;
                }
            }
        }

        private void CadCanvas_ContextMenuClosing(object sender, ContextMenuEventArgs e)
        {
            if (sender is CadCanvas canvas)
            {
                if (canvas.ContextMenu.DataContext is MenuItem cmindex)
                {
                    switch (cmindex.Header)
                    {
                        case "Freeze All":
                            foreach (object obj in canvas.Children)
                            {
                                if (obj is CadObject cadObject)
                                {
                                    cadObject.IsFix = true;
                                }
                            }
                            break;
                        case "Unselect All":
                            ClearSelectedObject(null);
                            break;
                    }
                }
            }
        }

        public void ClearSelectedObject(object noclearobj)
        {
            foreach (object obj in this.Children)
            {
                if (obj != noclearobj && obj is CadObject cadObject)
                {
                    cadObject.IsSelected = false;
                }
                else
                {

                }

            }
            SelectedObject?.Invoke(null, false);
           
        }

        private void CadCanvas_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (this._selectedRectangle.Visibility == Visibility.Visible)
            {
                this._selectedRectangle.Visibility = Visibility.Hidden;
                this.SelectInRectangle(this._selectedRectangle.RenderedGeometry);
                this.ReleaseMouseCapture();
            }

            this._wasmove = false;
        }

        private void SelectInRectangle(Geometry rectangle)
        {
            foreach (object obj in this.Children)
            {
                /*Task.Run(() => { */
                if (obj is CadObject cadObject)
                {
                    if (rectangle.Bounds.Contains(
                        new Point(/*cadObject.X +*/ cadObject.RenderedGeometry.Bounds.TopLeft.X - Canvas.GetLeft(this._selectedRectangle), 
                                /*cadObject.Y +*/ cadObject.RenderedGeometry.Bounds.TopLeft.Y - Canvas.GetTop(this._selectedRectangle))) &&
                        rectangle.Bounds.Contains(
                            new Point(/*cadObject.X +*/ cadObject.RenderedGeometry.Bounds.BottomRight.X - Canvas.GetLeft(this._selectedRectangle), 
                                /*cadObject.Y +*/ cadObject.RenderedGeometry.Bounds.BottomRight.Y - Canvas.GetTop(this._selectedRectangle))))
                    {
                        cadObject.IsSelected = true;
                    }
                    
                }
                /*});*/
            }
             
        }

        private void CadCanvas_MouseMove(object sender, MouseEventArgs e)
        {
            if (this._selectedRectangle.Visibility == Visibility.Visible)
            {
                this._wasmove = true;
                Point RealMousePosition = e.GetPosition(this);
                if (RealMousePosition.X < LastMouseDownPosition.X)
                {
                    Canvas.SetLeft(this._selectedRectangle, RealMousePosition.X);
                }
                else
                {
                    Canvas.SetLeft(this._selectedRectangle, LastMouseDownPosition.X);
                }

                if (RealMousePosition.Y < LastMouseDownPosition.Y)
                {
                    Canvas.SetTop(this._selectedRectangle, RealMousePosition.Y);
                }
                else
                {
                    Canvas.SetTop(this._selectedRectangle, LastMouseDownPosition.Y);
                }

                this._selectedRectangle.Width = Math.Abs(RealMousePosition.X - LastMouseDownPosition.X);
                this._selectedRectangle.Height = Math.Abs(RealMousePosition.Y - LastMouseDownPosition.Y);
                this.CaptureMouse();
            }
        }

        public void RemoveSelectObject()
        {
            for (int i = 0; i < this.Children.Count; i++)
            {
                if (this.Children[i] is CadObject cadObject && cadObject.IsSelected == true)
                {
                    this.Children.Remove(cadObject);
                    i--;
                }
            }
            SelectedObject?.Invoke(this, false);
            UpdateProjection(false);
        }

        public CadDot[,] GetMeshDot(int Height, int Width)
        {
            CadDot[,] mesh = new CadDot[Height, Width];
            for (int i = 0; i < Height; i++)
                for (int j = 0; j < Width; j++)
                {
                    mesh[i, j] = this.Children[i * Width + j] as CadDot;
                }
            return mesh;
        }

        //Рисуем квадраты в поле согласно схеме
        public void DrawMesh(MonchaDeviceMesh mesh, MonchaDevice _device)
        {
            if (_device != null)
            {
                this.DataContext = _device;
                this.Clear();

                if (mesh == null)
                    mesh = _device.BaseMesh;

                //
                // Поинты
                //

                for (int i = 0; i < mesh.GetLength(0); i++)
                    for (int j = 0; j < mesh.GetLength(1); j++)
                    {
                        mesh[i, j].M = this._size;

                        CadDot dot = new CadDot(
                             mesh[i, j],
                            this.ActualWidth * 0.02,
                            //calibration flag
                            true, true, false);

                        dot.IsFix = false; // !mesh.OnlyEdge;
                        dot.StrokeThickness = 0;
                        dot.Uid = i.ToString() + ":" + j.ToString();
                        dot.ToolTip = "Позиция: " + i + ":" + j + "\nX: " + mesh[i, j].X + "\n" + "Y: " + mesh[i, j].Y;
                        dot.DataContext = mesh;
                        dot.OnBaseMesh = !mesh.Affine;
                        dot.Render = false;
                        this.Add(dot);
                    }

            }
            SelectedObject?.Invoke(null, false);
        }

        public void Clear()
        {
            for (int i = 0; i < this.Children.Count; i ++)
            {
                if (this.Children[i] is CadObject cadObject)
                {
                    RemoveChildren(cadObject);
                    i--;
                }
            }
        }

        public void RemoveChildren(CadObject cadObject)
        {
            cadObject.Selected -= CadObject_Selected;
            cadObject.OnObject -= CadObject_OnObject1;
            cadObject.Updated -= CadObject_Updated;
            cadObject.Remove();
            this.Children.Remove(cadObject);
        }

        public void Add(Shape obj)
        {
            if (this.Children.Equals(obj) == false)
            {
                if (obj is CadObject cadObject)
                {
                    cadObject.Selected += CadObject_Selected;
                    cadObject.OnObject += CadObject_OnObject1;
                    cadObject.Updated += CadObject_Updated;
                }
                this.Children.Add(obj);
            }

        }

        private void CadObject_Updated(object sender, string e)
        {
            UpdateProjection(false);
        }

        private void CadObject_OnObject1(object sender, bool e)
        {
            this._nofreecursor = e;
        }

        private void CadObject_Selected(object sender, bool e)
        {
            if (Keyboard.Modifiers != ModifierKeys.Shift)
            {
                this.ClearSelectedObject((CadObject)sender);
            }
            SelectedObject?.Invoke(sender, e);
        }


    }
}
