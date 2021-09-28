using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Documents;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using CadProjectorSDK;
using System.Collections.Generic;
using CadProjectorSDK.Setting;
using System.Threading;
using AppSt = CadProjectorViewer.Properties.Settings;
using CadProjectorSDK.Device;
using CadProjectorSDK.CadObjects;
using ToGeometryConverter.Object;
using ToGeometryConverter;
using System.Collections.ObjectModel;
using System.Windows.Media.Media3D;
using CadProjectorSDK.CadObjects.LObjects;
using CadProjectorSDK.CadObjects.Abstract;
using CadProjectorSDK.CadObjects.Interface;

namespace CadProjectorViewer.CanvasObj
{
    public class CanvasObject : FrameworkElement, INotifyPropertyChanged
    {
        public ObservableCollection<CanvasObject> Children { get; } = new ObservableCollection<CanvasObject>();

        public UidObject CadObject
        {
            get => cadobject;
            set
            {
                cadobject = value;
                if (cadobject != null)
                {
                    cadobject.PropertyChanged += Cadobject_PropertyChanged;
                }
                OnPropertyChanged("CadObject");
            }
        }

        private void Cadobject_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            this.InvalidateVisual();
        }

        private UidObject cadobject;

        //Event
        //public event EventHandler TranslateChanged;
        public virtual event EventHandler<bool> Fixed;
        public virtual event EventHandler<bool> Selected;
        public virtual event EventHandler<bool> OnObject;
        public virtual event EventHandler<string> Updated;
        public virtual event EventHandler<CanvasObject> Removed;
        public virtual event EventHandler<CanvasObject> Opening;

        public virtual Rect Bounds => this.GetGeometry.Bounds;

        public virtual Geometry GetGeometry => _Geometry;
        private Geometry _Geometry;

        public bool ActiveObject { get; private set; }

        public double StrokeThinkess
        {
            get => strokethinkess <= 0 ? ProjectorHub.GetThinkess / 3d : strokethinkess;
            set
            {
                strokethinkess = value;
                OnPropertyChanged("StrokeThinkess");
            }
        }
        private double strokethinkess = 0;

        public virtual Pen myPen { 
            get
            {
                double thinkess = StrokeThinkess;
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

        

        #region Property
        public event PropertyChangedEventHandler PropertyChanged;

        public void OnPropertyChanged([CallerMemberName] string prop = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop));
            this.Update();
        }
        #endregion


        public LProjectionSetting ProjectionSetting
        {
            get => cadobject.ProjectionSetting;
            set
            {
                if (value != cadobject.ProjectionSetting)
                {
                    cadobject.ProjectionSetting = value;
                    OnPropertyChanged("ProjectionSetting");
                }
            }
        }
        protected Point MousePos = new Point();
        protected Point BasePos = new Point();

        #region TranformObject
        public Transform3DGroup TransformGroup 
        {
            get => this.CadObject.TransformGroup;
            set
            {
                this.CadObject.TransformGroup = value;
                OnPropertyChanged("TransformGroup");
            }
        }

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

        public virtual double AngleY
        {
            get => this.CadObject.AngleY;
            set => this.CadObject.AngleY = value;
        }
        public RotateTransform3D RotateY { get; set; } = new RotateTransform3D();

        private AxisAngleRotation3D AxisAngleZ = new AxisAngleRotation3D(new Vector3D(0, 0, 1), 0);

        public virtual double AngleZ
        {
            get => this.CadObject.AngleZ;
            set => this.CadObject.AngleZ = value;
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



        public double X { get => this.CadObject.X; set => this.CadObject.X = value; }
        public double Y { get => this.CadObject.Y; set => this.CadObject.Y = value; }

        public double Z { get => this.CadObject.Z; set => this.CadObject.Z = value; }


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


        #region Variable
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

        public bool Render
        {
            get => this._render && (!AppSt.Default.stg_selectable_show || this.IsSelected);
            set
            {

                this._render = value;
                if (AppSt.Default.stg_selectable_show == true) this.IsSelected = value;
                Updated?.Invoke(this, "Render");
                OnPropertyChanged("Render");
            }
        }
        private bool _render = true;

        public bool ShowName { get; set; } = true;

        public bool WasMove { get; set; } = false;

        public bool Editing { get; set; } = false;

        #endregion


        protected override void OnMouseLeftButtonUp(System.Windows.Input.MouseButtonEventArgs e)
        {
            base.OnMouseLeftButtonUp(e);
            if (ActiveObject == true)
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
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            OnPropertyChanged("IsMouseOver");
            if (ActiveObject == true)
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

        }

        protected override void OnMouseLeave(MouseEventArgs e)
        {
            base.OnMouseLeave(e);
            if (ActiveObject == true)
            {
                this.WasMove = false;
                OnObject?.Invoke(this, this.IsMouseOver);
            }
            OnPropertyChanged("IsMouseOver");
        }

        protected override void OnMouseEnter(MouseEventArgs e)
        {
            base.OnMouseEnter(e);
            if (ActiveObject == true)
            {
                OnObject?.Invoke(this, this.IsMouseOver);
            }
        }

        protected override void OnMouseWheel(MouseWheelEventArgs e)
        {
            base.OnMouseWheel(e);
            if (ActiveObject == true)
            {
                if ((e.Delta != 0) && (Keyboard.Modifiers != ModifierKeys.Control))
                {
                    if (Keyboard.Modifiers == ModifierKeys.Alt) RotateAxis(this.CadObject.AngleY, "AngleY");
                    else if (Keyboard.Modifiers == (ModifierKeys.Alt | ModifierKeys.Shift)) RotateAxis(this.CadObject.AngleX, "AngleX");
                    else if (Keyboard.Modifiers == ModifierKeys.None ||
                        Keyboard.Modifiers == ModifierKeys.Shift) RotateAxis(this.CadObject.AngleZ, "AngleZ");
                }

                void RotateAxis(double axisAngleRotation3D, string OnPropertyString)
                {
                    axisAngleRotation3D += Math.Abs(e.Delta) / e.Delta * (Keyboard.Modifiers == ModifierKeys.Shift ? 1 : 5);
                    OnPropertyChanged(OnPropertyString);
                }
            }
        }

        protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
        {
            base.OnMouseLeftButtonDown(e);
            if (ActiveObject == true)
            {
                this.MousePos = e.GetPosition(this);
                this.BasePos = new Point(this.X, this.Y);
            }
        }

        protected override void OnContextMenuClosing(ContextMenuEventArgs e)
        {
            base.OnContextMenuClosing(e);
            if (ActiveObject == true)
            {
                if (this.ContextMenu.DataContext is MenuItem menuItem)
                {
                    DoItContextMenu(menuItem);
                }
            }
        }


        public CanvasObject(bool ActiveObject)
        {
            if (this.ContextMenu == null)
            {
                this.ContextMenu = new System.Windows.Controls.ContextMenu();
                ContextMenuLib.CadObjMenu(this.ContextMenu);
            }
            //this.ProjectionSetting.PropertyChanged += ProjectionSetting_PropertyChanged;
            this.Uid = Guid.NewGuid().ToString();
            this.ActiveObject = ActiveObject;
            this.Cursor = Cursors.Hand;
        }


        private void ProjectionSetting_PropertyChanged(object sender, PropertyChangedEventArgs e) => this.Update();

        private void Transform_Changed(object sender, EventArgs e) => this.Update();
        
        public void Update()
        {
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

        public virtual void Remove()
        {
            Removed?.Invoke(this, this);
        }


        protected override void OnRender(DrawingContext drawingContext)
        {
            if (this.CadObject is CadGroup objectsGroup)
            {
                foreach (UidObject cadObject in objectsGroup)
                {
                    drawingContext.DrawGeometry(this.myBack, this.myPen, GetGeometry);
                }
            }
            else
            {
                Geometry geometry = GetGeometry;
                if (geometry != null)
                {
                    drawingContext.DrawGeometry(myBack, myPen, geometry);
                }
            }

            if (this.IsSelected == true)
            {
                Geometry geometry = GetGeometry;
                //Left
                DrawSize(drawingContext,
                    new Point(0, geometry.Bounds.Y + geometry.Bounds.Height / 2),
                    new Point(geometry.Bounds.X, geometry.Bounds.Y + geometry.Bounds.Height / 2));
                //Right
                DrawSize(drawingContext,
                    new Point(ProjectorHub.Size.X, geometry.Bounds.Y + geometry.Bounds.Height / 2),
                    new Point(geometry.Bounds.X + geometry.Bounds.Width, geometry.Bounds.Y + geometry.Bounds.Height / 2));
                //Top
                DrawSize(drawingContext,
                    new Point(geometry.Bounds.X + geometry.Bounds.Width / 2, 0),
                    new Point(geometry.Bounds.X + geometry.Bounds.Width / 2, geometry.Bounds.Y));
                //Down
                DrawSize(drawingContext,
                    new Point(geometry.Bounds.X + geometry.Bounds.Width / 2, ProjectorHub.Size.Y),
                    new Point(geometry.Bounds.X + geometry.Bounds.Width / 2, geometry.Bounds.Y + geometry.Bounds.Height));
            }
        }

        protected void DrawSize(DrawingContext drawingContext, Point point1, Point point2)
        {
            double thinkess = ProjectorHub.GetThinkess / 3d / Math.Abs(this.Scale.ScaleX * Math.Max(this.ScaleX, this.ScaleY));
            thinkess = thinkess <= 0 ? 1 : thinkess;

            //drawingContext.DrawLine(new Pen(Brushes.DarkGray, thinkess), point1, point2);

            Vector vector = point1 - point2;

            drawingContext.DrawText(
                new FormattedText(Math.Round(vector.Length, 1).ToString(),
                new System.Globalization.CultureInfo("ru-RU"), 
                FlowDirection.LeftToRight,
                    new Typeface("Segoe UI"), 
                    (int)ProjectorHub.GetThinkess * 3,
                    Brushes.Gray), 
                new Point((point1.X + point2.X)/2, (point1.Y + point2.Y) / 2));

        }
    }
}
