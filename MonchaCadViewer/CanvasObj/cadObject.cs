using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Documents;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Media.Media3D;

namespace MonchaCadViewer.CanvasObj
{
    public abstract class CadObject : Shape, INotifyPropertyChanged
    {

        public event PropertyChangedEventHandler PropertyChanged;

        public void OnPropertyChanged([CallerMemberName] string prop = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop));
        }

        private bool _isfix = false;
        private bool _mirror = false;
        private bool _render = true;
        private RotateTransform _rotate = new RotateTransform();
        private TranslateTransform _translate = new TranslateTransform();
        private ScaleTransform _scale = new ScaleTransform();
        private bool _isselected = false;

        //Event
        public event EventHandler<CadObject> Selected;
        public event EventHandler<Rect> TranslateChanged;
        public event EventHandler<bool> Fixed;
        public event EventHandler<CadObject> Updated;
        public event EventHandler<CadObject> Removed;

        //Geometry
        public Geometry GmtrObj { get; set; }

        public Rect Bounds => GmtrObj.Bounds;

        public bool Mirror { 
            get => this._mirror;
            set
            {
                this._mirror = value;
                OnPropertyChanged("Mirror");
                this.CenterX = this.GmtrObj.Bounds.X - this._translate.X + this.GmtrObj.Bounds.Width / 2;
                this.CenterY = this.GmtrObj.Bounds.Y - this._translate.Y + this.GmtrObj.Bounds.Height / 2;
                if (this._mirror && this.ScaleX > 0)
                {
                    this.ScaleX = -this.ScaleX;
                }
                else if (this._mirror == false && this.ScaleX < 0)
                {
                    this.ScaleX = -this.ScaleX;
                }
                this.Updated?.Invoke(this, this);

            }
        }

        public bool IsSelected 
        {
            get => this._isselected;
            set
            {
                this._isselected = value;
                Selected?.Invoke(this, this);
                OnPropertyChanged("IsSelected");
            }
        }

        protected Point MousePos = new Point();
        protected Point BasePos = new Point();

        public TransformGroup Transform = new TransformGroup();
        public double X
        {
            get => this._translate.X;
            set
            {
                if (this.IsFix == false)
                {
                    this._translate.X = value;
                    OnPropertyChanged("X");
                    TranslateChanged?.Invoke(this, this.GmtrObj.Bounds);
                    if (this.Render == true)
                    {
                        Updated?.Invoke(this, this);
                    }
                }
            }
        }

        public double Y
        {
            get => this._translate.Y;
            set
            {
                if (this.IsFix == false)
                {
                    this._translate.Y = value;
                    OnPropertyChanged("Y");
                    TranslateChanged?.Invoke(this, this.GmtrObj.Bounds);
                    if (this.Render == true)
                    {
                        Updated?.Invoke(this, this);
                    }
                }
            }
        }

        public double Angle
        {
            get => this._rotate.Angle;
            set
            {
                this._rotate.Angle = value;
                OnPropertyChanged("Angle");
                if (this.Render == true)
                {
                    Updated?.Invoke(this, this);
                }
            }
        }

        public double ScaleX
        {
            get => this._scale.ScaleX;
            set
            {
                this._scale.ScaleX = value;
                OnPropertyChanged("ScaleX");
                if (this.Render == true)
                {
                    Updated?.Invoke(this, this);
                }
            }
        }

        public double ScaleY
        {
            get => this._scale.ScaleY;
            set
            {
                this._scale.ScaleY = value;
                OnPropertyChanged("ScaleY");
                if (this.Render == true)
                {
                    Updated?.Invoke(this, this);
                }
            }
        }

        public double CenterX
        {
            get => this._scale.CenterX;
            set
            {
                this._scale.CenterX = value;
                OnPropertyChanged("CenterX");
                if (this.Render == true)
                {
                    Updated?.Invoke(this, this);
                }
            }
        }

        public double CenterY
        {
            get => this._scale.CenterY;
            set
            {
                this._scale.CenterY = value;
                OnPropertyChanged("CenterY");
                if (this.Render == true)
                {
                    Updated?.Invoke(this, this);
                }
            }
        }

        protected override Geometry DefiningGeometry => GmtrObj;

        public bool Render
        {
            get => this._render;
            set
            {
                this._render = value;
                OnPropertyChanged("Render");
                this.Updated?.Invoke(this, this);
            }
        }
        
        public bool IsFix { get => _isfix; 
            set 
            {
                this._isfix = value;
                OnPropertyChanged("IsFix");
                if (Fixed != null)
                    Fixed(this, value);
                if (this.Render == true)
                {
                    Updated?.Invoke(this, this);
                }
            } 
        }

        public bool WasMove { get; set; } = false;

        public bool Editing { get; set; } = false;

        public bool OnBaseMesh { get; set; } = false;

        public bool MouseForce { get; set; } = false;

        public AdornerLayer adornerLayer { get; set; }

        public Adorner ObjAdorner { get; set; }


        public CadObject(bool mouseevent, bool move, Geometry Path)
        {
            this.GmtrObj = Path;

            this.PropertyChanged += CadObject_PropertyChanged;

            if (mouseevent || move)
            {
                this.MouseLeave += CadObject_MouseLeave;
                this.MouseLeftButtonUp += CadObject_MouseLeftButtonUp;
                this.MouseMove += CadObject_MouseMove;
                this.MouseLeftButtonDown += CadObject_MouseLeftButtonDown;
            }

            if (this.ContextMenu == null) this.ContextMenu = new System.Windows.Controls.ContextMenu();
            this.ContextMenu.ContextMenuClosing += ContextMenu_Closing;

            ContextMenuLib.CadObjMenu(this.ContextMenu);
            this.MouseForce = move;

            if (this.GmtrObj.Transform is TransformGroup transformGroup) {
                this.Transform = transformGroup;
                this._translate = (TranslateTransform)this.Transform.Children[2];
                this._rotate = (RotateTransform)this.Transform.Children[1];
                this._scale = (ScaleTransform)this.Transform.Children[0];
            }
            else
            {
                this.GmtrObj.Transform = new TransformGroup();
                this.Transform = (TransformGroup)this.GmtrObj.Transform;
                this._scale = new ScaleTransform();
                this.Transform.Children.Add(this._scale);
                this._rotate = new RotateTransform();
                this.Transform.Children.Add(this._rotate);
                this._translate = new TranslateTransform();
                this.Transform.Children.Add(this._translate);
            }
        }

        private void CadObject_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (this.Fill == null) this.Fill = Brushes.Gray;

            if (this.IsMouseOver)
            {
                if (this.Fill != Brushes.Transparent && this.Fill != null) this.Fill = Brushes.Orange;
                if (this.Stroke != null) this.Stroke = Brushes.Orange;
            }
            else if (this.IsSelected)
            {
                if (this.Fill != Brushes.Transparent && this.Fill != null) this.Fill = Brushes.Red;
                if (this.Stroke != null) this.Stroke = Brushes.Red;
            }
            else if (!this.Render)
            {
                if (this.Fill != Brushes.Transparent && this.Fill != null) this.Fill = Brushes.LightGray;
                if (this.Stroke != null) this.Stroke = Brushes.LightGray;
            }
            else if (this.IsFix)
            {
                if (this.Fill != Brushes.Transparent && this.Fill != null) this.Fill = Brushes.LightBlue;
                if (this.Stroke != null) this.Stroke = Brushes.LightBlue;
            }
            else
            {
                if (this.Fill != Brushes.Transparent && this.Fill != null) this.Fill = Brushes.Gray;
                if (this.Stroke != null) this.Stroke = Brushes.Blue;
            }
        }

        private void ContextMenu_Closing(object sender, RoutedEventArgs e)
        {

        }

        private void CadObject_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            Canvas canvas = this.Parent as Canvas;
            this.MousePos = e.GetPosition(canvas);
            this.BasePos = new Point(this._translate.X, this._translate.Y);
        }

        private void CadObject_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
        {
            OnPropertyChanged("MouseLeave");

            this.WasMove = false;
        }

        private void CadObject_MouseLeftButtonUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
           
                if (this.WasMove == false)
                {
                    this.IsSelected = !this.IsSelected;

                   /* if (this.IsSelected && !Keyboard.IsKeyDown(Key.LeftShift))
                        if (this.Parent is CadCanvas canvas)
                            canvas.UnselectAll(this);*/

                    if (this.ObjAdorner != null) 
                    {
                        if (this.IsSelected)
                        {
                            this.ObjAdorner.Visibility = Visibility.Visible;
                        }
                        else
                        {
                            this.ObjAdorner.Visibility = Visibility.Hidden;
                        }   
                    }
                }
                else
                {
                    this.MouseForce = false;
                    this.WasMove = false;
                    this.Editing = false;
                    this.ReleaseMouseCapture();

                }

            if (Selected != null)
            {
                if (this.IsSelected)
                    Selected(this, this);
                else
                    Selected(this, null);
            }
        }

        public bool Remove()
        {
            if (this.Parent is CadCanvas canvas)
            {
                canvas.Children.Remove(this);
                if (Removed != null)
                    Removed(this, this);
                return true;
            }

            return false;
        }

        private void CadObject_MouseMove(object sender, MouseEventArgs e)
        {
            OnPropertyChanged("MouseMove");

            if (this.IsFix == false)
            {
                CadCanvas canvas = this.Parent as CadCanvas;

                if ((e.LeftButton == MouseButtonState.Pressed && canvas.Status == 0) || MouseForce)
                {
                    this.WasMove = true;
                    this.Editing = true;

                    Point tPoint = e.GetPosition(canvas);

                    this.X = this.BasePos.X + (tPoint.X - this.MousePos.X);
                    this.Y = this.BasePos.Y + (tPoint.Y - this.MousePos.Y);

                    this.CaptureMouse();
                    this.Cursor = Cursors.SizeAll;
                }
                else
                {
                    this.Cursor = Cursors.Hand;
                }
            }
        }
    }
}
