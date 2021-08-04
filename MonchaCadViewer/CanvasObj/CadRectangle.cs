﻿using MonchaCadViewer.CanvasObj;
using MonchaCadViewer.Interface;
using MonchaCadViewer.Panels.ObjectPanel;
using MonchaSDK;
using MonchaSDK.Object;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using System.Windows.Shapes;

namespace MonchaCadViewer.CanvasObj
{
    public class CadRectangle : CadObject, INotifyPropertyChanged, IDrawingObject
    {
        #region Property
        public event PropertyChangedEventHandler PropertyChanged;

        public void OnPropertyChanged([CallerMemberName] string prop = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop));
        }
        #endregion

        public override event EventHandler<string> Updated;
        public override event EventHandler<CadObject> Removed;

        public LSize3D LRect
        {
            get => _lrect;
            set
            {
                if (_lrect != null) { }
                _lrect = value;
                _lrect.PropertyChanged += _lrect_PropertyChanged;
            }
        }

        public SolidColorBrush BackColorBrush;

        private void _lrect_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            this.InvalidateVisual();
            Updated?.Invoke(this, "Rect");
        }

        private LSize3D _lrect;


        public override double X
        {
            get => Math.Min(LRect.P1.MX, LRect.P2.MX);
            set
            {
                if (this.IsFix == false)
                {
                    double delta = value - Math.Min(LRect.P1.MX, LRect.P2.MX);
                    LRect.P1.MX += delta;
                    LRect.P2.MX += delta;
                    Updated?.Invoke(this, "X");
                    OnPropertyChanged("X");
                }
            }
        }
        public override double Y
        {
            get => Math.Min(LRect.P1.MY, LRect.P2.MY);
            set
            {
                if (this.IsFix == false)
                {
                    double delta = value - Math.Min(LRect.P1.MY, LRect.P2.MY);
                    LRect.P1.MY += delta;
                    LRect.P2.MY += delta;
                    Updated?.Invoke(this, "Y");
                    OnPropertyChanged("Y");
                }
            }
        }

        public bool IsInit { get; private set; } = false;

        public override Rect Bounds => new Rect(LRect.P1.GetMPoint, LRect.P2.GetMPoint);

        public CadRectangle(LPoint3D P1, LPoint3D P2, string Label, bool MouseSet)
        {
            this.NameID = Label;
            this.LRect = new LSize3D(P1, P2);
            LoadSetting(MouseSet);
        }

        public CadRectangle(LSize3D lRect, string Label, bool MouseSet)
        {
            this.NameID = Label;
            LRect = lRect;
            LoadSetting(MouseSet);
        }


        private void LoadSetting(bool MouseSet)
        {
            this.Render = false;
            UpdateTransform(false);

            ContextMenuLib.CadRectMenu(this.ContextMenu);
        }

        protected override void OnContextMenuClosing(ContextMenuEventArgs e)
        {
            base.OnContextMenuClosing(e);
            if (this.ContextMenu.DataContext is MenuItem menuItem)
            {
                switch (menuItem.Tag)
                {
                    case "common_Setting":
                        CadRectangleSizePanel cadRectangleSizePanel = new CadRectangleSizePanel(this);
                        cadRectangleSizePanel.Show();
                        break;

                }
            }
        }


        protected override void OnRender(DrawingContext drawingContext)
        {
            drawingContext.DrawText(
                new FormattedText(this.NameID.ToString(),
                new System.Globalization.CultureInfo("ru-RU"),
                FlowDirection.LeftToRight,
                    new Typeface("Segoe UI"),
                    (int)MonchaHub.GetThinkess * 3,
                    Brushes.Gray),
                new Point(LRect.P1.MX, LRect.P1.MY));

            drawingContext.DrawRectangle(BackColorBrush, myPen, new Rect(X, Y, Math.Abs(LRect.P1.MX - LRect.P2.MX), Math.Abs(LRect.P1.MY - LRect.P2.MY)));
        }

        public override void Remove()
        {
            Removed?.Invoke(this, this);
        }

        public void Init()
        {
            AdornerLayer adornerLayer = AdornerLayer.GetAdornerLayer(this);
            adornerLayer.Add(new RectangelAdorner(this));
            IsInit = true;
        }

        public void SetTwoPoint(Point point)
        {
            this.LRect.P2.MX = point.X;
            this.LRect.P2.MY = point.Y;
        }
    }

    public class RectangelAdorner : Adorner
    {
        private VisualCollection _Visuals;

        private List<CadAnchor> _Anchors;

        private CadRectangle rectangle;
        // Be sure to call the base class constructor.
        public RectangelAdorner(CadRectangle adornedElement): base(adornedElement)
        {
            _Visuals = new VisualCollection(this);
            _Anchors = new List<CadAnchor>();

            this.rectangle = adornedElement;
            this.rectangle.PropertyChanged += Rectangle_PropertyChanged;


            Rect rect = new Rect(0, 0, this.rectangle.Bounds.Width, this.rectangle.Bounds.Height);

            _Anchors.Add(new CadAnchor(adornedElement.LRect.P1));
            _Anchors.Add(new CadAnchor(adornedElement.LRect.P2, adornedElement.LRect.P1));
            _Anchors.Add(new CadAnchor(adornedElement.LRect.P1, adornedElement.LRect.P2));
            _Anchors.Add(new CadAnchor(adornedElement.LRect.P2));

            foreach (CadAnchor anchor in _Anchors)
            {
                _Visuals.Add(anchor);
            }
        }

        private void Rectangle_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            this.InvalidateVisual();
        }

        protected override Size ArrangeOverride(Size finalSize)
        {
            foreach (CadAnchor anchor in _Anchors)
            {
                anchor.Arrange(new Rect(finalSize));
            }
            return this.rectangle.Bounds.Size;
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

