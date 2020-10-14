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
using MonchaSDK;
using System.Collections.Generic;
using ToGeometryConverter.Object;
using MonchaSDK.Setting;
using System.Threading;

namespace MonchaCadViewer.CanvasObj
{
    public abstract class CadObject : Shape, INotifyPropertyChanged
    {
        public Shape ObjectShape;

        public event PropertyChangedEventHandler PropertyChanged;

        public void OnPropertyChanged([CallerMemberName] string prop = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop));
        }

        public bool NoEvent = false;
        private bool _isfix = false;
        private bool _mirror = false;
        private bool _render = true;

        private bool _otherprojection = false;
        public bool OtherProjection
        {
            get => this._otherprojection;
            set
            {
                this._otherprojection = value;

                if (value == true)
                {
                    this.ProjectionSetting = (LProjectionSetting)MonchaHub.ProjectionSetting.Clone();
                }
                else
                {
                    this.ProjectionSetting = MonchaHub.ProjectionSetting;
                }
            }
        }

        public LProjectionSetting ProjectionSetting = MonchaHub.ProjectionSetting;

        public TransformGroup Transform = new TransformGroup();

        public RotateTransform Rotate = new RotateTransform();
        public TranslateTransform Translate = new TranslateTransform();
        public ScaleTransform Scale = new ScaleTransform();

        private bool _isselected = false;

        //Event
        //public event EventHandler TranslateChanged;
        public event EventHandler<bool> Fixed;
        public event EventHandler<bool> Selected;
        public event EventHandler<bool> OnObject;
        public event EventHandler<string> Updated;
        public event EventHandler<CadObject> Removed;
        public event EventHandler<CadObject> Opening;

        #region Variable
        public Rect Bounds
        {
            get
            {
                switch (this.ObjectShape.GetType().Name)
                {
                    case "Path":
                        Path path = (Path)this.ObjectShape;
                        return path.Data.Bounds;
                        break;
                    case "NurbsShape":
                        NurbsShape nurbsShape = (NurbsShape)this.ObjectShape;
                        return nurbsShape.RenderedGeometry.Bounds;
                        break;
                    case "CadContour":
                        CadContour cadContour = (CadContour)this.ObjectShape;
                        return cadContour.Data.Bounds;
                        break;
                }

                return new Rect();
            }
        }

        public bool Mirror
        {
            get => this._mirror;
            set
            {
                this._mirror = value;
                OnPropertyChanged("Mirror");
                this.CenterX = this.DefiningGeometry.Bounds.X - this.Translate.X + this.DefiningGeometry.Bounds.Width / 2;
                this.CenterY = this.DefiningGeometry.Bounds.Y - this.Translate.Y + this.DefiningGeometry.Bounds.Height / 2;
                if (this._mirror && this.ScaleX > 0)
                {
                    this.ScaleX = -this.ScaleX;
                }
                else if (this._mirror == false && this.ScaleX < 0)
                {
                    this.ScaleX = -this.ScaleX;
                }
            }
        }

        public bool IsSelected
        {
            get => this._isselected;
            set
            {
                if (this._isselected != value)
                {
                    this._isselected = value;
                    if (this.NoEvent == false)
                    {
                        Selected?.Invoke(this, this._isselected);
                    }
                    OnPropertyChanged("IsSelected");
                }
            }
        }

        protected Point MousePos = new Point();
        protected Point BasePos = new Point();

        public virtual double X
        {
            get => this.Translate.X;
            set
            {
                if (this.IsFix == false)
                {
                    if (this.Translate.X != value)
                    {
                        this.Translate.X = value;
                        if (this.Render == true)
                        {
                            Updated?.Invoke(this, "X");
                        }
                        OnPropertyChanged("X");
                    }
                }
            }
        }

        public virtual double Y
        {
            get => this.Translate.Y;
            set
            {
                if (this.IsFix == false)
                {
                    if (this.Translate.Y != value)
                    {
                        this.Translate.Y = value;
                        if (this.Render == true)
                        {
                            Updated?.Invoke(this, "Y");
                        }
                        OnPropertyChanged("Y");
                    }
                }
            }
        }

        public double Angle
        {
            get => this.Rotate.Angle;
            set
            {
                this.Rotate.Angle = value;
                OnPropertyChanged("Angle");
                if (this.Render == true)
                {
                    Updated?.Invoke(this, "Angle");
                }
            }
        }

        public double ScaleX
        {
            get => this.Scale.ScaleX;
            set
            {
                this.Scale.ScaleX = value;
                if (this.Render == true)
                {
                    Updated?.Invoke(this, "ScaleX");
                }
                OnPropertyChanged("ScaleX");
            }
        }

        public double ScaleY
        {
            get => this.Scale.ScaleY;
            set
            {
                this.Scale.ScaleY = value;
                if (this.Render == true)
                {
                    Updated?.Invoke(this, "ScaleY");
                }
                OnPropertyChanged("ScaleY");
            }
        }

        public double CenterX
        {
            get => this.Scale.CenterX;
            set
            {
                this.Scale.CenterX = value;
                OnPropertyChanged("CenterX");
            }
        }

        public double CenterY
        {
            get => this.Scale.CenterY;
            set
            {
                this.Scale.CenterY = value;
                OnPropertyChanged("CenterY");
            }
        }

        public Geometry Data
        {
            get => this.DefiningGeometry;
        }
        //protected override Geometry DefiningGeometry => GmtrObj;

        protected override Geometry DefiningGeometry
        {
            get
            {
                Geometry geometry = null;

                switch (this.ObjectShape.GetType().Name)
                {
                    case "CadObjectsGroup":
                        CadObjectsGroup objectGroup = (CadObjectsGroup)this.ObjectShape;
                        geometry = objectGroup.Data;
                        break;

                    case "Path":
                        Path path = (Path)this.ObjectShape;
                        geometry = path.Data;

                        break;
                    case "NurbsShape":
                        NurbsShape nurbsShape = (NurbsShape)this.ObjectShape;
                        geometry = nurbsShape.Data;
                        break;
                }

                geometry.Transform = this.Transform;
                return geometry;
            }
        }


        public bool Render
        {
            get => this._render;
            set
            {
                if (value != this.Render)
                {
                    this._render = value;
                    Updated?.Invoke(this, "Render");
                    OnPropertyChanged("Render");
                }
            }
        }

        public bool IsFix
        {
            get => this._isfix;
            set
            {
                this._isfix = value;
                OnPropertyChanged("IsFix");
                Fixed?.Invoke(this, value);
            }
        }

        public bool WasMove { get; set; } = false;

        public bool Editing { get; set; } = false;

        public bool OnBaseMesh { get; set; } = false;

        public bool MouseForce { get; set; } = false;

        public AdornerLayer adornerLayer { get; set; }

        public Adorner ObjAdorner { get; set; }
        #endregion

        public CadObject(bool mouseevent, bool move)
        {
            if (mouseevent || move)
            {
                this.MouseLeave += CadObject_MouseLeave;
                this.MouseLeftButtonUp += CadObject_MouseLeftButtonUp;
                this.MouseMove += CadObject_MouseMove;
                this.MouseEnter += CadObject_MouseEnter;
                this.MouseLeftButtonDown += CadObject_MouseLeftButtonDown;
                this.PropertyChanged += CadObject_PropertyChanged;

                if (this.ContextMenu == null) this.ContextMenu = new System.Windows.Controls.ContextMenu();
                this.ContextMenu.ContextMenuClosing += ContextMenu_Closing;
                ContextMenuLib.CadObjMenu(this.ContextMenu);
            }
            this.MouseForce = move;
        }

        public void Update()
        {
            if (this.Render == true)
            {
                this.Updated?.Invoke(this, string.Empty);
            }
        }

        private void CadObject_MouseEnter(object sender, MouseEventArgs e)
        {
            OnObject?.Invoke(this, this.IsMouseOver);
        }


        public void UpdateTransform(TransformGroup transformGroup)
        {
            if (transformGroup != null)
            {
                this.Transform = transformGroup;
                this.Scale = this.Transform.Children[0] != null ? (ScaleTransform)this.Transform.Children[0] : new ScaleTransform();
                this.Rotate = this.Transform.Children[1] != null ? (RotateTransform)this.Transform.Children[1] : new RotateTransform();
                this.Translate = this.Transform.Children[2] != null ? (TranslateTransform)this.Transform.Children[2] : new TranslateTransform();
            }
            else
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
            }
        }

        private void CadObject_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (this.IsMouseOver == true)
            {
                if (this.Fill != Brushes.Transparent && this.Fill != null) this.Fill = Brushes.Orange;
                if (this.Stroke != null) this.Stroke = Brushes.Orange;
            }
            else if (this._isselected == true)
            {
                if (this.Fill != Brushes.Transparent && this.Fill != null) this.Fill = Brushes.Red;
                if (this.Stroke != null) this.Stroke = Brushes.Red;
            }
            else if (this._render == false)
            {
                if (this.Fill != Brushes.Transparent && this.Fill != null) this.Fill = Brushes.LightGray;
                if (this.Stroke != null) this.Stroke = Brushes.LightGray;
            }
            else if (this._isfix == true)
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
            this.BasePos = new Point(this.Translate.X, this.Translate.Y);
        }

        private void CadObject_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
        {
            this.WasMove = false;
            OnObject?.Invoke(this, this.IsMouseOver);
        }

        private void CadObject_MouseLeftButtonUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (this.WasMove == false)
            {
                this.IsSelected = !this._isselected;
            }
            else
            {
                this.MouseForce = false;
                this.WasMove = false;
                this.Editing = false;
                this.ReleaseMouseCapture();
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
