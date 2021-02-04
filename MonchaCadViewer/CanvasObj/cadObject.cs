using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Documents;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Media.Media3D;
using MonchaSDK;
using System.Collections.Generic;
using ToGeometryConverter.Object;
using MonchaSDK.Setting;
using System.Threading;
using MonchaCadViewer.Interface;
using MonchaCadViewer.CanvasObj.DimObj;
using AppSt = MonchaCadViewer.Properties.Settings;

namespace MonchaCadViewer.CanvasObj
{
    public abstract class CadObject : FrameworkElement, INotifyPropertyChanged, TransformObject
    {
        public Geometry myGeometry { get; set; }
        public virtual Pen myPen { 
            get
            {
                if (this.IsMouseOver == true)
                {
                    return new Pen(Brushes.Orange, MonchaHub.GetThinkess);
                }
                else if (this._isselected == true)
                {
                    return new Pen(Brushes.Black, MonchaHub.GetThinkess);
                }
                else if (this._render == false)
                {
                    return new Pen(Brushes.DarkGray, MonchaHub.GetThinkess);
                }
                else if (this._isfix == true)
                {
                    return new Pen(Brushes.LightBlue, MonchaHub.GetThinkess);
                }
                else
                {
                    return new Pen(new SolidColorBrush(
                        Color.FromArgb(255,
                        (ProjectionSetting.RedOn == true ? ProjectionSetting.Red : (byte)0),
                        (ProjectionSetting.GreenOn == true ? ProjectionSetting.Green : (byte)0),
                        (ProjectionSetting.BlueOn == true ? ProjectionSetting.Blue : (byte)0))), 
                        MonchaHub.GetThinkess);
                }

                return null;
            }
        }
        public virtual Brush myBack => Brushes.Transparent;

        public string Name { get; set; } = string.Empty;

        public AdornerLayer adornerLayer { get; set; }

        #region Property
        public event PropertyChangedEventHandler PropertyChanged;

        public void OnPropertyChanged([CallerMemberName] string prop = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop));
        }
        #endregion

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

        #region TranformObject
        public TransformGroup TransformGroup 
        {
            get => transform;
            set
            {
                transform = value;
                this.Scale = (ScaleTransform)value.Children[0];
                this.Rotate = (RotateTransform)value.Children[1];
                this.Translate = (TranslateTransform)value.Children[2];
            }
        }
        private TransformGroup transform = new TransformGroup();
        public RotateTransform Rotate { get; set; } = new RotateTransform();
        public TranslateTransform Translate { get; set; } = new TranslateTransform();
        public ScaleTransform Scale { get; set; } = new ScaleTransform();

        public bool Mirror
        {
            get => _mirror;
            set
            {
                _mirror = value;
                this.ScaleX = this.ScaleX;
                OnPropertyChanged("Mirror");
            }
        }
        private bool _mirror = false;

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
                        Updated?.Invoke(this, "X");
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
                        Updated?.Invoke(this, "Y");

                        OnPropertyChanged("Y");

                    }
                }
            }
        }
        public virtual double Angle
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
        public virtual double ScaleX
        {
            get => this.Scale.ScaleX;
            set
            {
                this.Scale.ScaleX = (this.Mirror == true ? -1 * Math.Abs(value) : Math.Abs(value));
                if (this.Render == true)
                {
                    Updated?.Invoke(this, "ScaleX");
                }
                OnPropertyChanged("ScaleX");
            }
        }
        public virtual double ScaleY
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
        public virtual double CenterX
        {
            get => this.Scale.CenterX;
            set
            {
                this.Scale.CenterX = value;
                OnPropertyChanged("CenterX");
            }
        }
        public virtual double CenterY
        {
            get => this.Scale.CenterY;
            set
            {
                this.Scale.CenterY = value;
                OnPropertyChanged("CenterY");
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
        private bool _isfix = false;
        #endregion

        public bool IsSelected
        {
            get => this._isselected;
            set
            {
                if (this._isselected != value)
                {
                    this._isselected = value;
                    Selected?.Invoke(this, this._isselected);
                    OnPropertyChanged("IsSelected");
                }
            }
        }
        private bool _isselected = false;

        //Event
        //public event EventHandler TranslateChanged;
        public virtual event EventHandler<bool> Fixed;
        public virtual event EventHandler<bool> Selected;
        public virtual event EventHandler<bool> OnObject;
        public virtual event EventHandler<string> Updated;
        public virtual event EventHandler<CadObject> Removed;
        public virtual event EventHandler<CadObject> Opening;

        #region Variable

        public virtual Rect Bounds => myGeometry.Bounds;

        //protected override Geometry DefiningGeometry => GmtrObj;

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
        private bool _render = true;

        public bool WasMove { get; set; } = false;

        public bool Editing { get; set; } = false;

        public bool OnBaseMesh { get; set; } = false;

        public virtual Adorner ObjAdorner { get; set; }

        #endregion

        public CadObject(bool mouseevent, bool move)
        {

            if (this.ContextMenu == null) this.ContextMenu = new System.Windows.Controls.ContextMenu();

            this.Loaded += CadObject_Loaded;
        }

        private void CadObject_Loaded(object sender, RoutedEventArgs e)
        {
            if (true)
            {
                this.MouseLeave += CadObject_MouseLeave;
                this.MouseLeftButtonUp += CadObject_MouseLeftButtonUp;
                this.MouseMove += CadObject_MouseMove;
                this.MouseEnter += CadObject_MouseEnter;
                this.MouseLeftButtonDown += CadObject_MouseLeftButtonDown;
                this.PropertyChanged += CadObject_PropertyChanged;
                this.ProjectionSetting.PropertyChanged += CadObject_PropertyChanged;

                ContextMenuLib.CadObjMenu(this.ContextMenu);
                this.ContextMenuClosing += CadObject_ContextMenuClosing;
            }

            this.InvalidateVisual();
            this.adornerLayer = AdornerLayer.GetAdornerLayer(this);
        }

        private void CadObject_ContextMenuClosing(object sender, ContextMenuEventArgs e)
        {
            if (this.ContextMenu.DataContext is MenuItem menuItem)
            {
                DoItContextMenu(menuItem);
            }
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
            OnPropertyChanged();
        }


        public void UpdateTransform(TransformGroup transformGroup)
        {
            if (transformGroup == null)
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
            }
            else
            {
                this.TransformGroup = transformGroup;
            }

            this.X = -(this.Bounds.X + this.Bounds.Width / 2) + MonchaHub.Size.MX / 2;
            this.Y = -(this.Bounds.Y + this.Bounds.Height / 2) + MonchaHub.Size.MY / 2;
            this._mirror = AppSt.Default.default_mirror;
            Scale.ScaleX = AppSt.Default.default_scale_x / 100 * (AppSt.Default.default_mirror == true ? -1 : 1);
            if (this.ScaleX < 0) this.Mirror = true;
            Scale.ScaleY = AppSt.Default.default_scale_y / 100;
            Scale.CenterX = this.Bounds.X + this.Bounds.Width / 2;
            Scale.CenterY = this.Bounds.Y + this.Bounds.Height / 2;
            Rotate.CenterX = Scale.CenterX;
            Rotate.CenterY = Scale.CenterY;
            Rotate.Angle = AppSt.Default.default_angle;
            this.InvalidateVisual();
        }

        public virtual void DoItContextMenu(MenuItem menuItem)
        {
            switch (menuItem.Header)
            {
                case "Mirror":
                    this.Mirror = !this.Mirror;
                    break;

                case "Fix":
                    this.IsFix = !this.IsFix;
                    break;

                case "Remove":
                    this.Remove();
                    break;

                case "Render":
                    this.Render = !this.Render;
                    break;
            }
        }

        private void CadObject_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            this.InvalidateVisual();
        }


        private void CadObject_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            Canvas canvas = this.Parent as Canvas;
            this.MousePos = e.GetPosition(canvas);
            this.BasePos = new Point(this.X, this.Y);
        }

        private void CadObject_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
        {
            this.WasMove = false;
            OnObject?.Invoke(this, this.IsMouseOver);
            OnPropertyChanged();
        }

        private void CadObject_MouseLeftButtonUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (this.WasMove == false)
            {
                this.IsSelected = !this._isselected;
            }
            else
            {
                this.WasMove = false;
                this.Editing = false;
                this.ReleaseMouseCapture();
            }
        }

        public void Remove()
        {
            Removed?.Invoke(this, this);
        }

        private void CadObject_MouseMove(object sender, MouseEventArgs e)
        {
            if (this.IsFix == false)
            {
                CadCanvas canvas = this.Parent as CadCanvas;

                if (e.LeftButton == MouseButtonState.Pressed)
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
            OnPropertyChanged();
        }

        protected override void OnRender(DrawingContext drawingContext)
        {
            drawingContext.PushTransform(new TransformGroup()
            {
                Children = new TransformCollection()
                {
                    this.Scale,
                    this.Rotate,
                    this.Translate
                }
            });
            drawingContext.DrawGeometry(myBack, myPen, myGeometry);
            //drawingContext.DrawRectangle(myBack, myPen, myGeometry.Bounds);
        }

    }
}
