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
using MonchaSDK.Setting;
using System.Threading;
using MonchaCadViewer.Interface;
using AppSt = MonchaCadViewer.Properties.Settings;
using MonchaSDK.Device;
using MonchaSDK.Object;
using ToGeometryConverter.Object;
using ToGeometryConverter;

namespace MonchaCadViewer.CanvasObj
{
    public abstract class CadObject : FrameworkElement, INotifyPropertyChanged, TransformObject, LSettingObject
    {
        private LObjectList _renderpoint;

        public virtual Rect Bounds => this.GetGeometry.Bounds;

        public virtual Geometry GetGeometry { get; set; }

        public virtual Pen myPen { 
            get
            {
                double thinkess = MonchaHub.GetThinkess / 3d;
                thinkess = thinkess <= 0 ? 1 : thinkess;

                if (this.IsMouseOver == true)
                {
                    return new Pen(Brushes.Orange, thinkess * 1.5);
                }
                else if (this.IsSelected == true)
                {
                    return new Pen(Brushes.Black, thinkess);
                }
                else if (this.Render == false)
                {
                    return new Pen(Brushes.DarkGray, thinkess);
                }
                else if (this.IsFix == true)
                {
                    return new Pen(Brushes.LightBlue, thinkess);
                }
                else
                {
                    return new Pen(new SolidColorBrush(
                        Color.FromArgb(255,
                        (ProjectionSetting.RedOn == true ? ProjectionSetting.Red : (byte)0),
                        (ProjectionSetting.GreenOn == true ? ProjectionSetting.Green : (byte)0),
                        (ProjectionSetting.BlueOn == true ? ProjectionSetting.Blue : (byte)0))),
                        thinkess);
                }

                return null;
            }
        }
        public virtual Brush myBack => Brushes.Transparent;

        public virtual string NameID
        {
            get => name;
            set
            {
                name = value;
                this.ToolTip = value;
                OnPropertyChanged("Name");
            }
        }
        private string name = string.Empty;

        public MeshType MeshType { get; set; } = MeshType.SELECT;

        public AdornerLayer adornerLayer { get; set; }

        #region Property
        public event PropertyChangedEventHandler PropertyChanged;

        public void OnPropertyChanged([CallerMemberName] string prop = "")
        {
            //Console.WriteLine(prop);
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop));
        }
        #endregion


        public LProjectionSetting ProjectionSetting
        {
            get => projectionSetting == null ? MonchaHub.ProjectionSetting : projectionSetting;
            set
            {
                projectionSetting = value;
            }
        }
        private LProjectionSetting projectionSetting;

        public bool OwnedSetting 
        {
            get => ownedsetting;
            set
            {
                ownedsetting = value;
                if (ownedsetting == true) projectionSetting = MonchaHub.ProjectionSetting.Clone();
                else projectionSetting = null;
            }
        }
        private bool ownedsetting = false;

        #region TranformObject
        public virtual Transform3DGroup TransformGroup 
        {
            get => transform;
            set
            {
                transform = value;
                this.Scale = (ScaleTransform3D)value.Children[0];
                this.RotateX = (RotateTransform3D)value.Children[1];
                this.AxisAngleX = (AxisAngleRotation3D)this.RotateX.Rotation;
                this.RotateY = (RotateTransform3D)value.Children[2];
                this.AxisAngleY = (AxisAngleRotation3D)this.RotateY.Rotation;
                this.RotateZ = (RotateTransform3D)value.Children[3];
                this.AxisAngleZ = (AxisAngleRotation3D)this.RotateZ.Rotation;
                this.Translate = (TranslateTransform3D)value.Children[4];
            }
        }

        private Transform3DGroup transform = new Transform3DGroup();

        private AxisAngleRotation3D AxisAngleX = new AxisAngleRotation3D(new Vector3D(1, 0, 0), 0);
        public virtual double AngleX
        {
            get => this.AxisAngleX.Angle % 360;
            set
            {
                if (this.IsFix == false)
                {
                    this.AxisAngleX.Angle = value % 360;
                    OnPropertyChanged("AngleX");
                }
            }
        }
        public RotateTransform3D RotateX { get; set; } = new RotateTransform3D();

        private AxisAngleRotation3D AxisAngleY = new AxisAngleRotation3D(new Vector3D(0, 1, 0), 0);
        public virtual double AngleY
        {
            get => this.AxisAngleY.Angle % 360;
            set
            {
                if (this.IsFix == false)
                {
                    this.AxisAngleY.Angle = value % 360;
                    OnPropertyChanged("AngleY");
                }
            }
        }
        public RotateTransform3D RotateY { get; set; } = new RotateTransform3D();

        private AxisAngleRotation3D AxisAngleZ = new AxisAngleRotation3D(new Vector3D(0, 0, 1), 0);

        public virtual double AngleZ
        {
            get => this.AxisAngleZ.Angle % 360;
            set
            {
                if (this.IsFix == false)
                {
                    this.AxisAngleZ.Angle = value % 360;
                    OnPropertyChanged("AngleZ");
                }
            }
        }
        public RotateTransform3D RotateZ { get; set; } = new RotateTransform3D();
        public TranslateTransform3D Translate { get; set; } = new TranslateTransform3D();

        public ScaleTransform3D Scale { get; set; } = new ScaleTransform3D();

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
            get => this.Translate.OffsetX;
            set
            {
                if (this.IsFix == false)
                {
                    this.Translate.OffsetX = value;
                    OnPropertyChanged("X");
                }
            }
        }
        public virtual double Y
        {
            get => this.Translate.OffsetY;
            set
            {
                if (this.IsFix == false)
                {
                    if (this.Translate.OffsetY != value)
                    {
                        this.Translate.OffsetY = value;
                        OnPropertyChanged("Y");

                    }
                }
            }
        }
        public virtual double Z
        {
            get => this.Translate.OffsetZ;
            set
            {
                if (this.IsFix == false)
                {
                    if (this.Translate.OffsetZ != value)
                    {
                        this.Translate.OffsetZ = value;
                        OnPropertyChanged("Z");

                    }
                }
            }
        }


        public virtual double ScaleX
        {
            get => this.Scale.ScaleX;
            set
            {
                this.Scale.ScaleX = (this.Mirror == true ? -1 * Math.Abs(value) : Math.Abs(value));
                OnPropertyChanged("ScaleX");
            }
        }
        public virtual double ScaleY
        {
            get => this.Scale.ScaleY;
            set
            {
                this.Scale.ScaleY = value;
                OnPropertyChanged("ScaleY");
            }
        }
        public virtual double ScaleZ
        {
            get => this.Scale.ScaleZ;
            set
            {
                this.Scale.ScaleZ = value;
                OnPropertyChanged("ScaleZ");
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
        public virtual double CenterZ
        {
            get => this.Scale.CenterZ;
            set
            {
                this.Scale.CenterZ = value;
                OnPropertyChanged("CenterZ");
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
                this._isselected = value;
                Selected?.Invoke(this, this._isselected);
                OnPropertyChanged("IsSelected");
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


        //protected override Geometry DefiningGeometry => GmtrObj;

        public bool Render
        {
            get => this._render && (!AppSt.Default.stg_selectable_show || this.IsSelected);
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

        public bool ShowName { get; set; } = true;

        public bool WasMove { get; set; } = false;

        public bool Editing { get; set; } = false;

        #endregion

        public CadObject()
        {
            if (this.ContextMenu == null)
            {
                this.ContextMenu = new System.Windows.Controls.ContextMenu();
                ContextMenuLib.CadObjMenu(this.ContextMenu);
            }
            this.Loaded += CadObject_Loaded;
        }

        private void CadObject_Loaded(object sender, RoutedEventArgs e)
        {
            this.ContextMenuClosing += CadObject_ContextMenuClosing;
            this.MouseLeave += CadObject_MouseLeave;
            this.MouseLeftButtonUp += CadObject_MouseLeftButtonUp;
            this.MouseMove += CadObject_MouseMove;
            this.MouseEnter += CadObject_MouseEnter;
            this.MouseWheel += CadContour_MouseWheel;
            this.MouseLeftButtonDown += CadObject_MouseLeftButtonDown;
            this.PropertyChanged += CadObject_PropertyChanged;
            this.ProjectionSetting.PropertyChanged += CadObject_PropertyChanged;

            transform.Changed += Transform_Changed;
            this.InvalidateVisual();
        }

        private void Transform_Changed(object sender, EventArgs e)
        {
            this.InvalidateVisual();
        }

        private void CadContour_MouseWheel(object sender, MouseWheelEventArgs e)
        {
             if ((e.Delta != 0) && (Keyboard.Modifiers != ModifierKeys.Control))
            {
                if (Keyboard.Modifiers == ModifierKeys.Alt) RotateAxis(AxisAngleY, "AngleY");
                else if (Keyboard.Modifiers == (ModifierKeys.Alt | ModifierKeys.Shift)) RotateAxis(AxisAngleX, "AngleX");
                else if (Keyboard.Modifiers == ModifierKeys.None ||
                    Keyboard.Modifiers == ModifierKeys.Shift) RotateAxis(AxisAngleZ, "AngleZ");
            }

            void RotateAxis(AxisAngleRotation3D axisAngleRotation3D, string OnPropertyString)
            {
                axisAngleRotation3D.Angle += Math.Abs(e.Delta) / e.Delta * (Keyboard.Modifiers == ModifierKeys.Shift ? 1 : 5);
                OnPropertyChanged(OnPropertyString);
            }
        }


        private void CadObject_ContextMenuClosing(object sender, ContextMenuEventArgs e)
        {
            if (this.ContextMenu.DataContext is MenuItem menuItem)
            {
                DoItContextMenu(menuItem);
            }
        }

        private void CadObject_MouseEnter(object sender, MouseEventArgs e)
        {
            OnObject?.Invoke(this, this.IsMouseOver);
            OnPropertyChanged();
        }


        public void UpdateTransform(Transform3DGroup transformGroup, bool resetPosition, Rect Bounds)
        {
            if (transformGroup == null)
            {
                this.TransformGroup = new Transform3DGroup()
                {
                    Children = new Transform3DCollection()
                    {
                        new ScaleTransform3D(),
                        new RotateTransform3D() { Rotation = AxisAngleX },
                        new RotateTransform3D() { Rotation = AxisAngleY },
                        new RotateTransform3D() { Rotation = AxisAngleZ },
                        new TranslateTransform3D()
                    }
                };
            }
            else
            {
                this.TransformGroup = transformGroup;
            }

            if (resetPosition == true)
            {
                //Tuple1 - vertical, Tuple2 - horizontal
                Tuple<string, string> position = new Tuple<string, string>(AppSt.Default.stg_default_position.Split('%')[0], AppSt.Default.stg_default_position.Split('%')[1]);

                if (position.Item2 == "Left") this.X = -Bounds.X + 30 * MonchaHub.Size.M.X;
                else if (position.Item2 == "Right") this.X = MonchaHub.Size.X - (Bounds.X + Bounds.Width) - 30 * MonchaHub.Size.M.X;
                else this.X = MonchaHub.Size.X / 2 - (Bounds.X + Bounds.Width / 2);

                if (position.Item1 == "Down") this.Y = MonchaHub.Size.Y - (Bounds.Y + Bounds.Height) - 30 * MonchaHub.Size.M.Y;
                else if (position.Item1 == "Top") this.Y = -Bounds.Y + 30 * MonchaHub.Size.M.X;
                else this.Y = MonchaHub.Size.Y / 2 - (Bounds.Y + Bounds.Height / 2);

                this._mirror = AppSt.Default.default_mirror;
                Scale.ScaleX = AppSt.Default.default_scale_x / 100 * (AppSt.Default.default_mirror == true ? -1 : 1);
                if (this.ScaleX < 0) this.Mirror = true;
                Scale.ScaleY = AppSt.Default.default_scale_y / 100;
                Scale.CenterX = Bounds.X + Bounds.Width / 2;
                Scale.CenterY = Bounds.Y + Bounds.Height / 2;
                RotateZ.CenterX = Scale.CenterX;
                RotateZ.CenterY = Scale.CenterY;
                AngleZ = 0;
                RotateX.CenterX = Scale.CenterX;
                RotateX.CenterY = Scale.CenterY;
                AngleX = 0;
                RotateY.CenterX = Scale.CenterX;
                RotateY.CenterY = Scale.CenterY;
                AngleY = 0;
                AngleZ = AppSt.Default.default_angle;
            }
            this.InvalidateVisual();
        }

        public virtual void DoItContextMenu(MenuItem menuItem)
        {
            switch (menuItem.Tag)
            {
                case "obj_Mirror":
                    this.Mirror = !this.Mirror;
                    break;

                case "obj_Fix":
                    this.IsFix = !this.IsFix;
                    break;

                case "common_Remove":
                    this.Remove();
                    break;

                case "obj_Render":
                    this.Render = !this.Render;
                    break;
            }
        }

        private void CadObject_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            this.InvalidateVisual();
            Updated?.Invoke(this, e.PropertyName);
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

        public virtual void Remove()
        {

            this.MouseLeave -= CadObject_MouseLeave;
            this.MouseLeftButtonUp -= CadObject_MouseLeftButtonUp;
            this.MouseMove -= CadObject_MouseMove;
            this.MouseEnter -= CadObject_MouseEnter;
            this.MouseWheel -= CadContour_MouseWheel;
            this.MouseLeftButtonDown -= CadObject_MouseLeftButtonDown;
            this.PropertyChanged -= CadObject_PropertyChanged;
            this.ProjectionSetting.PropertyChanged -= CadObject_PropertyChanged;

            Removed?.Invoke(this, this);
        }

        private void CadObject_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            this.MousePos = e.GetPosition(this);
            this.BasePos = new Point(this.X, this.Y);
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

                    Point tPoint = e.GetPosition(this);

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

        protected override void OnRender(DrawingContext drawingContext)
        {
            //Console.WriteLine("Render");
            Geometry geometry = GetGeometry;
            if (geometry != null)
            {
                /*if (AppSt.Default.stg_show_name == true)
                {
                    drawingContext.DrawText(new FormattedText($"{this.Name}", new System.Globalization.CultureInfo("ru-RU"), FlowDirection.LeftToRight,
                    new Typeface("Segoe UI"), (int)MonchaHub.GetThinkess * 3, Brushes.Gray), new Point(GetGeometry.Bounds.X + GetGeometry.Bounds.Width / 2, GetGeometry.Bounds.Y + GetGeometry.Bounds.Height / 2));
                }*/

                drawingContext.DrawGeometry(myBack, myPen, geometry);
                if (this.IsSelected == true) 
                {
                    //Left
                    DrawSize(drawingContext, 
                        new Point(0, geometry.Bounds.Y + geometry.Bounds.Height / 2), 
                        new Point(geometry.Bounds.X, geometry.Bounds.Y + geometry.Bounds.Height / 2));
                    //Right
                    DrawSize(drawingContext,
                        new Point(MonchaHub.Size.X, geometry.Bounds.Y + geometry.Bounds.Height / 2),
                        new Point(geometry.Bounds.X + geometry.Bounds.Width, geometry.Bounds.Y + geometry.Bounds.Height / 2));
                    //Top
                    DrawSize(drawingContext,
                        new Point(geometry.Bounds.X + geometry.Bounds.Width / 2, 0),
                        new Point(geometry.Bounds.X + geometry.Bounds.Width / 2, geometry.Bounds.Y));
                    //Down
                    DrawSize(drawingContext,
                        new Point(geometry.Bounds.X + geometry.Bounds.Width / 2, MonchaHub.Size.Y),
                        new Point(geometry.Bounds.X + geometry.Bounds.Width / 2, geometry.Bounds.Y + geometry.Bounds.Height));
                }
            }
        }

        protected void DrawSize(DrawingContext drawingContext, Point point1, Point point2)
        {
            double thinkess = MonchaHub.GetThinkess / 3d / Math.Abs(this.Scale.ScaleX * Math.Max(this.ScaleX, this.ScaleY));
            thinkess = thinkess <= 0 ? 1 : thinkess;

            //drawingContext.DrawLine(new Pen(Brushes.DarkGray, thinkess), point1, point2);

            Vector vector = point1 - point2;

            drawingContext.DrawText(
                new FormattedText(Math.Round(vector.Length, 1).ToString(),
                new System.Globalization.CultureInfo("ru-RU"), 
                FlowDirection.LeftToRight,
                    new Typeface("Segoe UI"), 
                    (int)MonchaHub.GetThinkess * 3,
                    Brushes.Gray), 
                new Point((point1.X + point2.X)/2, (point1.Y + point2.Y) / 2));

        }
    }
}
