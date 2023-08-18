using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Documents;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Collections.Generic;
using AppSt = CadProjectorViewer.Properties.Settings;
using CadProjectorSDK.CadObjects;
using CadProjectorSDK.CadObjects.Abstract;
using CadProjectorSDK.Scenes.Commands;
using CadProjectorSDK.Render;
using Point = System.Windows.Point;
using Pen = System.Windows.Media.Pen;
using Brush = System.Windows.Media.Brush;
using Brushes = System.Windows.Media.Brushes;
using CadProjectorViewer.ViewModel;
using Microsoft.Xaml.Behaviors.Core;
using Cursors = System.Windows.Input.Cursors;
using CadProjectorViewer.Dialogs;
using CadProjectorSDK.CadObjects.Interfaces;
using System.Linq;
using Size = System.Windows.Size;
using Rect = System.Windows.Rect;
using System.Windows.Data;
using System.Globalization;
using static CadProjectorViewer.CanvasObj.CanvasObject;
using CadProjectorSDK.Scenes.Actions;

namespace CadProjectorViewer.CanvasObj
{
    internal class CanvasObject : FrameworkElement, INotifyPropertyChanged
    {
        public event EventHandler UpdateAnchorPoints;
        public virtual event EventHandler<bool> OnObject;
        public virtual event EventHandler<CanvasObject> Opening;

        public virtual GetScaleDelegate GetFrameTransform { get; set; }
        public delegate ScaleTransform GetScaleDelegate();

        public virtual ChangeSizeDelegate SizeChange { get; set; }
        public delegate void ChangeSizeDelegate();

        internal GetViewDelegate GetViewModel { get; set; }
        internal delegate RenderDeviceModel GetViewDelegate();

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

        public AdornerLayer adornerLayer 
        {
            get => _alayer;
            protected set
            {
                _alayer = value;
            } 
        }
        private AdornerLayer _alayer;

        public virtual Rect Bounds => this.CadObject.Bounds;

        public bool ActiveObject { get; private set; }

        public virtual string NameID
        {
            get
            {
                string ret = this.CadObject.NameID;
                if (string.IsNullOrEmpty(ret) == true)
                {
                    ret = this.CadObject.ToString().Split('.').Last();
                }
                return ret;
            }
            set
            {
                name = value;
                this.ToolTip = value;
                OnPropertyChanged("Name");
            }
        }
        private string name = string.Empty;

        protected Point MousePos = new Point();
        protected Point BasePos = new Point();

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
            if (uidObject is IAnchoredObject anchoredObject && anchoredObject.CanAddPoint == true)
            {
                ContextMenuLib.AddItem("obj_AddProjectivePoint", AddProjectivePointCommand, this.ContextMenu);
                ContextMenuLib.AddItem("obj_RectProjectivePoint", RectProjectivePointCommand, this.ContextMenu);
                ContextMenuLib.AddItem("obj_RoundCentre", RoundCentreCommand, this.ContextMenu);
            }


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
            if (this.CadObject is IAnchoredObject anchoredObject)
            {
                adornerLayer = AdornerLayer.GetAdornerLayer(this);
                if (adornerLayer != null)
                {
                    AnchorAdorner anchorAdorner = new AnchorAdorner(anchoredObject, this);
                    if (anchoredObject.AlwaysShowAnchor == false)
                    {
                        Binding binding = new Binding()
                        {
                            Source = this.CadObject,
                            Path = new PropertyPath("IsInit"),
                            Converter = new InitVisible()
                        };
                        anchorAdorner.SetBinding(Adorner.VisibilityProperty, binding);
                    }
                    adornerLayer.Add(anchorAdorner);
                }
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

        public ICommand RectProjectivePointCommand => new ActionCommand(() =>
        {
            if (this.CadObject.ProjectionMat == null)
            {
                Point TL = this.Bounds.TopLeft;
                Point TR = this.Bounds.TopRight;
                Point BL = this.Bounds.BottomLeft;
                Point BR = this.Bounds.BottomRight;
                CadAnchor[] anchors = new CadAnchor[]
                {
                    new CadAnchor(TL.X, TL.Y, 0),
                    new CadAnchor(TR.X, TR.Y, 0),
                    new CadAnchor(BL.X, BL.Y, 0),
                    new CadAnchor(BR.X, BR.Y, 0)
                };
                this.CadObject.AddProjectionPoints(anchors);
            } 
            else
            {
                this.CadObject.ClearProjectionPoint();
            }
            UpdateAnchorPoints?.Invoke(this, null);
        });

        public ICommand AddProjectivePointCommand => new ActionCommand(() =>
        {
            if (this.CadObject.ProjectionMat == null)
            {
                this.CadObject.SendCommand(new AddProjectPointAction(this.CadObject));
            }
            else
            {
                this.CadObject.ClearProjectionPoint();
            }
            UpdateAnchorPoints?.Invoke(this, null);
        });

        public ICommand MasksCommand => new ActionCommand(() =>
        {
            Rect bounds = this.CadObject.Bounds;
            MakeMeshSplitDialog makeMeshSplitDialog = new MakeMeshSplitDialog(bounds, this.CadObject.GetScene?.Invoke());
            makeMeshSplitDialog.Show();
        });

        public ICommand RoundCentreCommand => new ActionCommand(() =>
        {

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

        internal void Drawing (UidObject uidObject, RenderDeviceModel deviceModel,
            bool IsSelected, bool MouseOver, bool ParentRender, DrawingContext drawingContext)
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
                if (obj is LinesCollection vectorLines)
                {
                    DrawVectorLines(vectorLines, drawingContext, renderDevice, brush, pen);
                }
            }
        }

        private void DrawVectorLines(LinesCollection vectorLines, DrawingContext drawingContext, RenderDeviceModel renderDevice, Brush brush, Pen pen)
        {
            if (vectorLines.Count > 0)
            {
                StreamGeometry streamGeometry = new StreamGeometry();

                using (StreamGeometryContext ctx = streamGeometry.Open())
                {
                    Point point = renderDevice.GetPoint(vectorLines[0].P1.X, vectorLines[0].P1.Y);
                    ctx.BeginFigure(point, vectorLines.IsClosed, vectorLines.IsClosed);

                    for (int i = 0; i < vectorLines.Count; i += 1)
                    {
                        point = renderDevice.GetPoint(vectorLines[i].P1.X, vectorLines[i].P1.Y);
                        ctx.BeginFigure(point, vectorLines.IsClosed, vectorLines.IsClosed);

                        Point point_second = renderDevice.GetPoint(vectorLines[i].P2.X, vectorLines[i].P2.Y);
                        ctx.LineTo(point_second, true, true);
                    }

                    ctx.Close();
                }
                drawingContext.DrawGeometry(brush, pen, streamGeometry);
            }
        }

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

        public virtual Pen GetPen(double StrThink, bool MouseOver, bool Selected,
            bool Render, bool Blank, SolidColorBrush DefBrush)
        {
            if (Render == true || AppSt.Default.show_hide_object == true)
            {
                if (MouseOver == true)
                {
                    return new Pen(Brushes.Orange, StrThink * 1.5);
                }
                else if (Selected == true && Render == true)
                {
                    return new Pen(Brushes.MediumPurple, StrThink);
                }
                else if (Render == false && Selected == true)
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
            }

            return null;
        }

        public static Brush GetBrush(UidObject uidObject) => Brushes.Transparent;

        public virtual Brush myBack => GetBrush(this.CadObject);


        #region OnPropertyChanged
        public event PropertyChangedEventHandler PropertyChanged;

        public void OnPropertyChanged([CallerMemberName] string prop = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop));
            this.Update();
        }
        #endregion
    }

    internal class AnchorAdorner : Adorner
    {
        private VisualCollection _Visuals;

        private GetViewDelegate GetViewModel { get; set; }


        private IAnchoredObject AnchoredObject;

        public AnchorAdorner(IAnchoredObject canvasObject, CanvasObject uIElement) : base(uIElement)
        {
            this.AnchoredObject = canvasObject;
            this.GetViewModel = uIElement.GetViewModel;

            _Visuals = new VisualCollection(this);

            if (this.AnchoredObject.Anchors.Count() > 0)
            {
                foreach (CadAnchor point in this.AnchoredObject.Anchors)
                {
                    AddAnchor(point);
                }
            }

            this.AnchoredObject.UpdateAnchorPoints += AnchoredObject_UpdatePoints;
        }

        private void AnchoredObject_UpdatePoints(object sender, EventArgs e) => UpdatePoint();

        private void UpdatePoint()
        {
            RemoveAnchors();
            foreach (CadAnchor anchor in this.AnchoredObject.Anchors)
            {
                AddAnchor(anchor);
            }
        }

        private void AddAnchor(CadAnchor anchor)
        {
            if (anchor != null)
            {
                CanvasAnchor CanvAnchor = new CanvasAnchor(anchor);
                CanvAnchor.GetViewModel = this.GetViewModel;
                anchor.PropertyChanged += Point_PropertyChanged;
                _Visuals.Add(CanvAnchor);
                this.InvalidateVisual();
            }
        }

        private void RemoveAnchors()
        {
            for (int i = _Visuals.Count - 1; i > -1; i -= 1)
            {
                if (_Visuals[i] is CanvasAnchor cadAnchor)
                {
                    cadAnchor.PropertyChanged -= Point_PropertyChanged;
                    _Visuals.RemoveAt(i);
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
            foreach (object obj in _Visuals)
            {
                if (obj is CanvasAnchor anchor)
                {
                    anchor.Arrange(new Rect(finalSize));
                }
            }
            return this.AnchoredObject.Bounds.Size;
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

    public class InitVisible : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool b && b == true) return Visibility.Visible;
            else return Visibility.Hidden;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return null;
        }
    }
}
