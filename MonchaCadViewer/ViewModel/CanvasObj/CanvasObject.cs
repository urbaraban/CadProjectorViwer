﻿using System;
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

namespace CadProjectorViewer.CanvasObj
{
    public class CanvasObject : FrameworkElement, INotifyPropertyChanged
    {
        public ContextMenu ObjectContextMenu { get; set; }

        public virtual GetThinkessDelegate GetThinkess { get; set; }
        public delegate double GetThinkessDelegate();

        public virtual ChangeSizeDelegate SizeChange { get; set; }
        public delegate void ChangeSizeDelegate();

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
            this.InvalidateVisual();
            OnPropertyChanged(e.PropertyName);
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


        public double StrokeThinkess
        {
            get => strokethinkess <= 0 ? (this.GetThinkess?.Invoke() ?? 1) * AppSt.Default.default_thinkess_percent : strokethinkess;
            set
            {
                strokethinkess = value;
                OnPropertyChanged("StrokeThinkess");
            }
        }
        private double strokethinkess = 0;

        public virtual Pen myPen => GetPen(StrokeThinkess, this.IsMouseOver, this.CadObject.IsSelected, this.CadObject.IsRender, this.CadObject.IsBlank, this.CadObject.ProjectionSetting.GetBrush);

        public static Pen GetPen(
            double StrThink,
            bool MouseOver, 
            bool Selected,
            bool Render,
            bool Blank,
            SolidColorBrush DefBrush
            )
        {
            double thinkess = Math.Max(0.1, StrThink);

            if (MouseOver == true)
            {
                return new Pen(Brushes.Orange, thinkess * 1.5);
            }
            else if (Selected == true)
            {
                return new Pen(Brushes.Black, thinkess);
            }
            else if (Render == false)
            {
                return new Pen(Brushes.DarkGray, thinkess);
            }
            else if (Blank == true)
            {
                return new Pen(Brushes.LightBlue, thinkess);
            }
            else return new Pen(DefBrush, thinkess);

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
            adornerLayer = AdornerLayer.GetAdornerLayer(this);
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


        public CanvasObject(UidObject uidObject, bool ActiveObject)
        {
            this.CadObject = uidObject;
            this.DataContext = uidObject;
            this.ContextMenu = new System.Windows.Controls.ContextMenu();
            ContextMenuLib.CadObjMenu(this.ContextMenu);
            if (uidObject is CadGroup group)
            {
                ContextMenuLib.CadGroupMenu(this.ContextMenu);
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
                    this.CadObject.Mirror = !this.CadObject.Mirror;
                    break;

                case "obj_Fix":
                    this.CadObject.IsFix = !this.CadObject.IsFix;
                    break;

                case "common_Remove":
                    this.CadObject.Remove();
                    break;

                case "obj_Render":
                    this.CadObject.IsRender = !this.CadObject.IsRender;
                    break;

                case "group_Open":
                    {
                        if (this.CadObject is CadGroup group)
                        {
                            group.Ungroup();
                        }
                    }
                    break;
            }
        }

        protected override void OnRender(DrawingContext drawingContext)
        {
           /* if (this.CadObject.IsSelected == true)
            {
                Rect Bounds = this.Bounds;
                //Left
                DrawSize(drawingContext,
                    new Point(0, Bounds.Y + Bounds.Height / 2),
                    new Point(Bounds.X, Bounds.Y + Bounds.Height / 2));
                //Right
                DrawSize(drawingContext,
                    new Point(ProjectorHub.Size.X, Bounds.Y + Bounds.Height / 2),
                    new Point(Bounds.X + Bounds.Width, Bounds.Y + Bounds.Height / 2));
                //Top
                DrawSize(drawingContext,
                    new Point(Bounds.X + Bounds.Width / 2, 0),
                    new Point(Bounds.X + Bounds.Width / 2, Bounds.Y));
                //Down
                DrawSize(drawingContext,
                    new Point(Bounds.X + Bounds.Width / 2, ProjectorHub.Size.Y),
                    new Point(Bounds.X + Bounds.Width / 2, Bounds.Y + Bounds.Height));
            }*/
        }

        protected void DrawSize(DrawingContext drawingContext, Point point1, Point point2)
        {
            double thinkess = CadObject.Thinkess() / 3d / Math.Abs(this.CadObject.Scale.ScaleX * Math.Max(this.CadObject.ScaleX, this.CadObject.ScaleY));
            thinkess = thinkess <= 0 ? 1 : thinkess;

            //drawingContext.DrawLine(new Pen(Brushes.DarkGray, thinkess), point1, point2);

            Vector vector = point1 - point2;

            drawingContext.DrawText(
                new FormattedText(Math.Round(vector.Length, 1).ToString(),
                new System.Globalization.CultureInfo("ru-RU"), 
                FlowDirection.LeftToRight,
                    new Typeface("Segoe UI"), 
                    (int)thinkess * 3,
                    Brushes.Gray), 
                new Point((point1.X + point2.X)/2, (point1.Y + point2.Y) / 2));

        }
    }
}