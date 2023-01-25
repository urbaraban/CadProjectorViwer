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
using CadProjectorSDK.CadObjects.Abstract;
using CadProjectorSDK.Interfaces;
using CadProjectorSDK.Device.Mesh;
using CadProjectorSDK.Scenes.Commands;
using CadProjectorSDK.Render;
using static System.Windows.Forms.LinkLabel;
using System.Drawing;
using Point = System.Windows.Point;
using Pen = System.Windows.Media.Pen;
using Brush = System.Windows.Media.Brush;
using Brushes = System.Windows.Media.Brushes;
using System.Threading.Tasks;
using CadProjectorViewer.ViewModel;
using Microsoft.Xaml.Behaviors.Core;
using Cursors = System.Windows.Input.Cursors;
using CadProjectorViewer.Dialogs;
using CadProjectorSDK.CadObjects.Interfaces;
using CadProjectorSDK.Tools;
using System.Linq;
using Size = System.Windows.Size;
using OpenCvSharp;
using Rect = System.Windows.Rect;

namespace CadProjectorViewer.CanvasObj
{
    public class CanvasObject : FrameworkElement, INotifyPropertyChanged
    {
        public virtual GetScaleDelegate GetFrameTransform { get; set; }
        public delegate ScaleTransform GetScaleDelegate();

        public virtual ChangeSizeDelegate SizeChange { get; set; }
        public delegate void ChangeSizeDelegate();

        public GetViewDelegate GetViewModel { get; set; }
        public delegate RenderDeviceModel GetViewDelegate();

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
        private UidObject cadobject;

        private void Cadobject_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            this.Dispatcher.Invoke(() =>
            {
                this.InvalidateVisual();
                OnPropertyChanged(e.PropertyName);
            });
        }

        public void ParentChangeSize()
        {
            this.InvalidateVisual();
            SizeChange?.Invoke();
        }

        public AdornerLayer adornerLayer 
        {
            get => _alayer;
            private set
            {
                _alayer = value;
            } 
        }
        private AdornerLayer _alayer;

        public virtual event EventHandler<bool> OnObject;
        public virtual event EventHandler<CanvasObject> Opening;

        public virtual Rect Bounds => this.CadObject.Bounds;

        public bool ActiveObject { get; private set; }

        public virtual Pen GetPen(bool parentRender = true)
        {
            double thinkess = GetViewModel?.Invoke().Thinkess ?? 1;

            return GetPen(
                thinkess,
                this.IsMouseOver,
                this.IsSelected,
                parentRender,
                this.IsBlank,
                GetViewModel?.Invoke().RenderingDisplay.ProjectionSetting.GetBrush);
        }

        public virtual Pen GetPen(
            double StrThink,
            bool MouseOver, 
            bool Selected,
            bool Render,
            bool Blank,
            SolidColorBrush DefBrush)
        {
            if (MouseOver == true)
            {
                return new Pen(Brushes.Orange, StrThink * 1.5);
            }
            else if (Selected == true)
            {
                return new Pen(Brushes.Black, StrThink);
            }
            else if (Render == false)
            {
                return new Pen(Brushes.DarkGray, StrThink);
            }
            else if (Blank == true)
            {
                return new Pen(Brushes.LightBlue, StrThink);
            }
            else return new Pen(DefBrush, StrThink);

            return null;
        }

        public static Brush GetBrush(UidObject uidObject) => Brushes.Transparent;

        public virtual Brush myBack => GetBrush(this.CadObject);

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

        #region OnPropertyChanged
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
        public virtual double X 
        { 
            get => this.CadObject.MX;
            set
            {
                this.CadObject.MX = value;
                OnPropertyChanged("X");
            }
        }
        public virtual double Y 
        { 
            get => this.CadObject.MY;
            set
            {
                this.CadObject.MY = value;
                OnPropertyChanged("Y");
            }
        }
        public virtual double Z 
        {
            get => this.CadObject.MZ;
            set
            {
                this.CadObject.MZ = value;
                OnPropertyChanged("Z");
            }
        }
        #endregion


        #region Variable

        public bool ShowName { get; set; } = true;

        public bool WasMove { get; set; } = false;

        public bool Editing { get; set; } = true;

        public bool IsSelected 
        { 
            get => this.CadObject.IsSelected;
            set
            {
                this.CadObject.IsSelected = value;
                OnPropertyChanged("IsSelect");
            }
        }
        public bool IsRender 
        {
            get => this.CadObject.IsRender;
            set
            {
                this.CadObject.IsRender = value;
                OnPropertyChanged("IsRender");
            }
        }
        public bool IsBlank
        {
            get => this.CadObject.IsBlank;
            set
            {
                this.CadObject.IsBlank = value;
                OnPropertyChanged("IsBlank");
            }
        }

        #endregion

        public CanvasObject(UidObject uidObject, bool ActiveObject)
        {
            this.CadObject = uidObject;
            this.DataContext = uidObject;
            this.ToolTip = this.Name;
            this.ContextMenu = new System.Windows.Controls.ContextMenu();

            ContextMenuLib.AddItem("obj_Mirror", MirrorCommand, this.ContextMenu);
            ContextMenuLib.AddItem("common_Remove", RemoveCommand, this.ContextMenu);
            ContextMenuLib.AddItem("obj_Render", RenderCommand, this.ContextMenu);
            ContextMenuLib.AddItem("common_MasksGrid", MasksCommand, this.ContextMenu);
            ContextMenuLib.AddItem("obj_AddProjectivePoint", AddProjectivePointCommand, this.ContextMenu);
            ContextMenuLib.AddItem("obj_RoundCentre", RoundCentreCommand, this.ContextMenu);

            if (uidObject is CadGroup group)
            {
                this.ContextMenu.Items.Add(new Separator());
                ContextMenuLib.AddItem("gr_Ungroup", UngroupCommand, this.ContextMenu);
            }

            //this.ProjectionSetting.PropertyChanged += ProjectionSetting_PropertyChanged;
            this.Uid = Guid.NewGuid().ToString();
            this.ActiveObject = ActiveObject;
            this.Cursor = Cursors.Hand;

            this.CadObject.RenderUpdated += CadObject_RenderUpdated;
        }

        private void CadObject_RenderUpdated(object sender, EventArgs e) => Update();

        protected override void OnMouseLeftButtonUp(System.Windows.Input.MouseButtonEventArgs e)
        {
            base.OnMouseLeftButtonUp(e);
            if (ActiveObject == true)
            {
                if (this.WasMove == false)
                {
                    this.CadObject.Select(!this.CadObject.IsSelected);
                }
                else
                {
                    Point tPoint = e.GetPosition(this);
                    this.CadObject.SendCommand(new MovingCommand(this.CadObject, this.CadObject.MX - this.BasePos.X, this.CadObject.MY - this.BasePos.Y) { Status = true });
                    this.WasMove = false;
                    this.Editing = false;
                    this.ReleaseMouseCapture();
                }
            }
        }

        protected override void OnInitialized(EventArgs e)
        {
            base.OnInitialized(e);
            if ((this is CanvasAnchor) == false)
            {
                adornerLayer = AdornerLayer.GetAdornerLayer(this);
                ProjectiveAdorner projective = new ProjectiveAdorner(this);
                adornerLayer.Add(projective);
            }
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            OnPropertyChanged("IsMouseOver");
            if (ActiveObject == true)
            {
                if (this.CadObject.IsFix == false)
                {
                    if (e.LeftButton == MouseButtonState.Pressed && Keyboard.Modifiers == ModifierKeys.Alt && this.Editing == false)
                    {
                        this.WasMove = true;
                        this.Editing = true;
                        this.CadObject.Cloning();
                        Point tPoint = e.GetPosition(this);
                        this.X = this.BasePos.X + (tPoint.X - this.MousePos.X);
                        this.Y = this.BasePos.Y + (tPoint.Y - this.MousePos.Y);
                    }
                    else if (e.LeftButton == MouseButtonState.Pressed)
                    {
                        this.WasMove = true;
                        this.Editing = true;

                        Point tPoint = e.GetPosition(this);

                        this.X = this.BasePos.X + (tPoint.X - this.MousePos.X);
                        this.Y = this.BasePos.Y + (tPoint.Y - this.MousePos.Y);

                        this.CaptureMouse();
                        this.Cursor = Cursors.SizeAll;
                    }
                    else if (e.LeftButton == MouseButtonState.Released)
                    {
                        this.Editing = false;
                    }
                }
            }
            else
                this.Cursor = Cursors.Hand;
        }

        protected override void OnMouseLeave(MouseEventArgs e)
        {
            base.OnMouseLeave(e);
            if (ActiveObject == true)
            {
                this.CadObject.IsMouseOver = false;
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
                this.CadObject.IsMouseOver = true;
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
                    if (Keyboard.Modifiers == ModifierKeys.Alt)
                    {
                        this.CadObject.AngleY += Math.Abs(e.Delta) / e.Delta * (Keyboard.Modifiers == ModifierKeys.Shift ? 1 : 5);
                    }
                    else if (Keyboard.Modifiers == (ModifierKeys.Alt | ModifierKeys.Shift))
                    {
                        this.CadObject.AngleX += Math.Abs(e.Delta) / e.Delta * (Keyboard.Modifiers == ModifierKeys.Shift ? 1 : 5);
                    }
                    else if (Keyboard.Modifiers == ModifierKeys.None ||
                        Keyboard.Modifiers == ModifierKeys.Shift)
                    {
                        this.CadObject.AngleZ += Math.Abs(e.Delta) / e.Delta * (Keyboard.Modifiers == ModifierKeys.Shift ? 1 : 5);
                    }
                }
            }
        }

        protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
        {
            if (ActiveObject == true)
            {
                this.MousePos = e.GetPosition(this);
                this.BasePos = new Point(this.X, this.Y);
            }
            base.OnMouseLeftButtonDown(e);
        }


        public ICommand RemoveCommand => new ActionCommand(() =>
        {
            this.CadObject.Remove();
        });

        public ICommand MirrorCommand => new ActionCommand(() =>
        {
            this.CadObject.Mirror = !this.CadObject.Mirror;
        });

        public ICommand RenderCommand => new ActionCommand(() =>
        {
            this.CadObject.IsRender = !this.CadObject.IsRender;
        });

        public ICommand UngroupCommand => new ActionCommand(() => 
        {
            if (this.CadObject is CadGroup group)
            {
                group.Ungroup();
            }
        });

        public ICommand AddProjectivePointCommand => new ActionCommand(() =>
        {
            Point tPoint = Mouse.GetPosition(this);
            CadAnchor cadAnchor = new CadAnchor(tPoint.X, tPoint.Y, 0);
            this.CadObject.AddProjectionPoint(cadAnchor);
        });

        public ICommand MasksCommand => new ActionCommand(() =>
        {
            Rect bounds = this.CadObject.Bounds;
            MakeMeshSplitDialog makeMeshSplitDialog = new MakeMeshSplitDialog(bounds, this.CadObject.GetScene?.Invoke());
            makeMeshSplitDialog.Show();
        });

        public ICommand RoundCentreCommand => new ActionCommand(() =>
        {
            Rect bounds = this.CadObject.Bounds;
            MakeMeshSplitDialog makeMeshSplitDialog = new MakeMeshSplitDialog(bounds, this.CadObject.GetScene?.Invoke());
            makeMeshSplitDialog.Show();
        });


        private void ProjectionSetting_PropertyChanged(object sender, PropertyChangedEventArgs e) => this.Update();

        private void Transform_Changed(object sender, EventArgs e) => this.Update();
        
        public async void Update()
        {
            this.Dispatcher.Invoke(() => this.InvalidateVisual());
        }

        protected override void OnRender(DrawingContext drawingContext)
        {
            if (this.GetViewModel?.Invoke() is RenderDeviceModel deviceModel) 
            {
                Drawing(CadObject, deviceModel, this.IsSelected, this.IsMouseOver, this.IsRender, drawingContext);
            }
        }

        internal void Drawing (
            UidObject uidObject, RenderDeviceModel deviceModel,
            bool IsSelected, bool MouseOver, bool ParentRender,
            DrawingContext drawingContext)
        {
            double thinkess = deviceModel.Thinkess;

            Pen pen = this.GetPen(ParentRender);

            Brush brush = GetBrush(uidObject);

            if (uidObject is CadGroup group)
            {
                IEnumerable<UidObject> VisualSorted = 
                    group.Children.OrderBy(e => e.IsRender == true || e.IsSelected == true);

                foreach (UidObject uid in VisualSorted)
                {
                    if (uid.IsRender == true || deviceModel.ShowHide == true)
                        Drawing(uid, deviceModel, IsSelected, MouseOver, ParentRender && uid.IsRender, drawingContext);
                }
            }
            else if (uidObject.GetRender(deviceModel.RenderingDisplay) is IEnumerable<IRenderedObject> linesCollection)
            {
                DrawingIRenderableObjects(linesCollection, drawingContext, deviceModel, brush, pen);
            }
        }

        private void DrawingIRenderableObjects(IEnumerable<IRenderedObject> objects, DrawingContext drawingContext, RenderDeviceModel renderDevice, Brush brush, Pen pen)
        {
            foreach(IRenderedObject obj in objects)
            {
                if (obj is VectorLinesCollection vectorLines)
                {
                    DrawVectorLines(vectorLines, drawingContext, renderDevice, brush, pen);
                }
            }
        }

        private void DrawVectorLines(VectorLinesCollection vectorLines, DrawingContext drawingContext, RenderDeviceModel renderDevice, Brush brush, Pen pen)
        {
            if (vectorLines.Count > 0)
            {
                StreamGeometry streamGeometry = new StreamGeometry();

                using (StreamGeometryContext ctx = streamGeometry.Open())
                {
                    Point point = renderDevice.GetPoint(vectorLines[0].P1.X, vectorLines[0].P1.Y);

                    ctx.BeginFigure(
                        point,
                        vectorLines.IsClosed,
                        vectorLines.IsClosed);

                    for (int i = 0; i < vectorLines.Count; i += 1)
                    {
                        Point point_second = renderDevice.GetPoint(vectorLines[i].P2.X, vectorLines[i].P2.Y);
                        ctx.LineTo(point_second, true, false);
                    }

                    ctx.Close();
                }
                drawingContext.DrawGeometry(brush, pen, streamGeometry);
            }
        }
    }

    public class ProjectiveAdorner : Adorner
    {
        private VisualCollection _Visuals;

        private List<CanvasAnchor> _Anchors { get; } = new List<CanvasAnchor>();

        private CanvasObject canvasObject;

        public ProjectiveAdorner(CanvasObject canvasObject) : base(canvasObject)
        {
            this.canvasObject = canvasObject;

            _Visuals = new VisualCollection(this);
            _Anchors = new List<CanvasAnchor>();

            if (this.canvasObject.CadObject.ProjectionPoint.Count > 0)
            {
                foreach ((Point2d, CadAnchor) point in this.canvasObject.CadObject.ProjectionPoint)
                {
                    AddAnchor(point.Item2);
                }
            }

            this.canvasObject.CadObject.ProjectionPoint.CollectionChanged += ProjectionPoint_CollectionChanged;

        }

        private void ProjectionPoint_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            if (e.NewItems != null)
            {
                foreach((Point2d, CadAnchor) anch in e.NewItems)
                {
                    AddAnchor(anch.Item2);
                }
            }
            if (e.OldItems != null)
            {
                foreach ((Point2d, CadAnchor) anch in e.NewItems)
                {
                    RemoveAnchor(anch.Item2);
                }
            }
        }

        private void AddAnchor(CadAnchor anchor)
        {
            CanvasAnchor CanvAnchor = new CanvasAnchor(anchor);
            CanvAnchor.GetViewModel = this.canvasObject.GetViewModel;
            this._Anchors.Add(CanvAnchor);
            anchor.PropertyChanged += Point_PropertyChanged;
            _Visuals.Add(CanvAnchor);
            this.InvalidateVisual();
        }

        private void RemoveAnchor(CadAnchor cadAnchor)
        {
            for (int i = 0; i < this._Anchors.Count; i += 1)
            {
                if (this._Anchors[i].CadObject.Uid == cadAnchor.Uid)
                {
                    this._Anchors[i].PropertyChanged -= Point_PropertyChanged;
                    _Visuals.Remove(_Anchors[i]);
                    this._Anchors.RemoveAt(i);
                }
            }
            this.InvalidateVisual();
        }

        private void Point_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            this.InvalidateVisual();
        }

        protected override Size ArrangeOverride(Size finalSize)
        {
            foreach (CanvasAnchor anchor in _Anchors)
            {
                anchor.Arrange(new Rect(finalSize));
            }
            return this.canvasObject.Bounds.Size;
        }

        // A common way to implement an adorner's rendering behavior is to override the OnRender
        // method, which is called by the layout system as part of a rendering pass.

        protected override int VisualChildrenCount { get { return _Visuals.Count; } }
        protected override Visual GetVisualChild(int index) { return _Visuals[index]; }
        protected override void OnRender(DrawingContext drawingContext)
        {
            base.OnRender(drawingContext);
        }
    }

}
