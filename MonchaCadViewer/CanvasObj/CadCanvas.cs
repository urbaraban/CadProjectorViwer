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
using MonchaCadViewer.Interface;
using System.Runtime.CompilerServices;
using System.Windows.Documents;

namespace MonchaCadViewer.CanvasObj
{
    public class CadCanvas : Canvas, TransformObject, INotifyPropertyChanged
    {
        #region INotifyPropertyChanged
        public event PropertyChangedEventHandler PropertyChanged;
        public void OnPropertyChanged([CallerMemberName] string prop = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop));
        }
        #endregion

        public event EventHandler<CadObject> SelectedObject;
        public event EventHandler UpdateProjection;
        //public event EventHandler<string> ErrorMessageEvent;

        private object MouseOnObject = null;

        private bool _wasmove = false;
        private LPoint3D _size;
        private List<CadDot> anchors = new List<CadDot>();
        private int _status = 0;
        private bool _maincanvas;
        private bool _nofreecursor = true;
        private Point StartMovePoint;
        private Point StartMousePoint;

        private Rectangle _selectedRectangle = new Rectangle();

        public bool HorizontalMesh { get; set; } = false;

        public Point LastMouseDownPosition = new Point();

        public int Status
        {
            get => _status;
            set => _status = value;
        }



        public CadCanvas()
        {
            this._size = MonchaHub.Size;
            this._maincanvas = true;
            LoadSetting();
        }

        public CadCanvas(LPoint3D Size, bool MainCanvas)
        {
            this._size = Size;
            this._maincanvas = MainCanvas;
            LoadSetting();
        }

        private void LoadSetting()
        {
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
                this.ContextMenu = new ContextMenu();
                ContextMenuLib.CanvasMenu(this.ContextMenu);

                this.MouseLeftButtonDown += Canvas_MouseLeftDown;
                this.MouseMove += CadCanvas_MouseMove;
                this.MouseUp += CadCanvas_MouseUp;
                this.MouseLeftButtonDown += CadCanvas_MouseLeftButtonDown;
                this.MouseMove += CadCanvas_MouseMove1;
                this.MouseWheel += CadCanvas_MouseWheel;
                this.KeyDown += CadCanvas_KeyDown;
                this.KeyUp += CadCanvas_KeyUp;
                this.MouseLeave += CadCanvas_MouseLeave;
            }

            ResetTransform();
        }

        private void CadCanvas_MouseLeave(object sender, MouseEventArgs e)
        {
            this.Cursor = Cursors.Arrow;
        }

        private void CadCanvas_KeyUp(object sender, KeyEventArgs e)
        {
            this.Cursor = Cursors.Arrow;
        }

        private void CadCanvas_KeyDown(object sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.LeftCtrl: 
                case Key.RightCtrl:
                    this.Cursor = Cursors.SizeAll;
                    break;
            }
        }

        private void CadCanvas_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (Keyboard.Modifiers == ModifierKeys.Control)
            {
                Point centre = e.GetPosition(this);
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

        private void CadCanvas_MouseMove1(object sender, MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed && Keyboard.Modifiers == ModifierKeys.Control)
            {
                if (this.IsFix == false)
                {
                        this.WasMove = true;

                        Point tPoint = e.GetPosition(this.Parent as System.Windows.IInputElement);

                        this.X = this.StartMovePoint.X + (tPoint.X - this.StartMousePoint.X);
                        this.Y = this.StartMovePoint.Y + (tPoint.Y - this.StartMousePoint.Y);

                        this.CaptureMouse();
                        
                }
            }
            else if (Keyboard.Modifiers == ModifierKeys.Control)
            {
                this.Cursor = Cursors.SizeAll;
            }
            else
            {
                this.Cursor = Cursors.Arrow;
            }
        }

        private void CadCanvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            this.StartMousePoint = e.GetPosition(this.Parent as System.Windows.IInputElement);
            this.StartMovePoint = new Point(this.Translate.X, this.Translate.Y);
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

        /// <summary>
        /// Draw object on canvas.
        /// </summary>
        /// <param name="obj">Object</param>
        /// <param name="maincanvas">property for main canvas attributes</param>
        /// <param name="add">Add contour for already view</param>
        /// <param name="mousemove">add mouse event</param>
        public void DrawContour(Shape obj, bool maincanvas, bool mousemove)
        {
            if (obj is CadObject cadObject)
            {
                cadObject.MouseMove += CadObject_MouseMove;
                cadObject.MouseLeave += CadObject_MouseLeave;
            }

            this.Add(obj);
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
            SelectedObject?.Invoke(this, null);
           
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
            SelectedObject?.Invoke(this, null);
            UpdateProjection?.Invoke(this, null);
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

        public void RemoveChildren(UIElement Object)
        {
            if (Object is CadObject cadObject)
            {
                cadObject.Selected -= CadObject_Selected;
                cadObject.OnObject -= CadObject_OnObject1;
                cadObject.Updated -= CadObject_Updated;
            }
            this.Children.Remove(Object);
        }

        /// <summary>
        /// Add object in Children Canvas
        /// </summary>
        /// <param name="obj"></param>
        public void Add(Shape obj)
        {
            if (this.Children.Equals(obj) == false)
            {
                if (obj is CadObject cadObject)
                {
                    cadObject.Selected += CadObject_Selected;
                    cadObject.OnObject += CadObject_OnObject1;
                    cadObject.Updated += CadObject_Updated;
                    cadObject.Removed += CadObject_Removed;
                }

                this.Children.Add(obj);
            }
        }

        private void CadObject_Removed(object sender, CadObject e)
        {
            RemoveChildren((UIElement)sender);
        }

        private void CadObject_Updated(object sender, string e)
        {
            UpdateProjection?.Invoke(this, null);
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
            if (e == true)
            SelectedObject?.Invoke(this, (CadObject)sender);
        }



        #region TransfromObject
        public TransformGroup Transform { get; set; }
        public ScaleTransform Scale { get; set; }
        public RotateTransform Rotate { get; set; }
        public TranslateTransform Translate { get; set; }

        public double X { get => this.Translate.X; 
            set 
            {
                this.Translate.X = value;
            }
        }
        public double Y { get => this.Translate.Y;
            set
            {
                this.Translate.Y = value;
            }
        }
        public bool IsFix { get; set; }
        public bool Mirror { get; set; } = false;

        public bool WasMove { get; set; } = false;

        public void UpdateTransform(TransformGroup transformGroup)
        {
            if (transformGroup != null)
            {
                this.RenderTransform = Transform;
                this.Scale = this.Transform.Children[0] != null ? (ScaleTransform)this.Transform.Children[0] : new ScaleTransform();
                this.Rotate = this.Transform.Children[1] != null ? (RotateTransform)this.Transform.Children[1] : new RotateTransform();
                this.Translate = this.Transform.Children[2] != null ? (TranslateTransform)this.Transform.Children[2] : new TranslateTransform();
            }
            else ResetTransform();


            if (this.Scale.ScaleX < 0) this.Mirror = true;
        }

        public void ResetTransform()
        {
            this.Transform = new TransformGroup()
            {
                Children = new TransformCollection()
                    {
                        new ScaleTransform(),
                        new RotateTransform(),
                        new TranslateTransform()
                    }
            };
            this.Scale = (ScaleTransform)this.Transform.Children[0];
            this.Rotate = (RotateTransform)this.Transform.Children[1];
            this.Translate = (TranslateTransform)this.Transform.Children[2];
            this.RenderTransform = this.Transform;
        }
        #endregion
    }
}
