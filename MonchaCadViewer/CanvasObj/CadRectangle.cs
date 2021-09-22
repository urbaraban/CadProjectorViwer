using CadProjectorViewer.CanvasObj;
using CadProjectorViewer.Interface;
using CadProjectorViewer.Panels.ObjectPanel;
using CadProjectorSDK;
using CadProjectorSDK.CadObjects;
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

namespace CadProjectorViewer.CanvasObj
{
    public class CadRectangle : CadObject, IDrawingObject
    {
        #region Property
        public event PropertyChangedEventHandler PropertyChanged;

        public void OnPropertyChanged([CallerMemberName] string prop = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop));
        }
        #endregion

        public override event EventHandler<bool> Selected;
        public override event EventHandler<string> Updated;
        public override event EventHandler<CadObject> Removed;

        public CadSize3D LRect
        {
            get => _lrect;
            set
            {
                if (_lrect != null) { }
                _lrect = value;
                _lrect.PropertyChanged += _lrect_PropertyChanged;
            }
        }

        private CadSize3D _lrect;

        public SolidColorBrush BackColorBrush;

        private void _lrect_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            this.InvalidateVisual();
            Updated?.Invoke(this, "Rect");
        }



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

        public CadRectangle(CadPoint3D P1, CadPoint3D P2, string Label, bool MouseSet) : base(true)
        {
            this.NameID = Label;
            this.LRect = new CadSize3D(P1, P2);
            LoadSetting();
        }

        public CadRectangle(CadSize3D lRect, string Label) : base(true)
        {
            this.NameID = Label;
            LRect = lRect;
            LoadSetting();
        }


        private void LoadSetting()
        {
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
                    (int)ProjectorHub.GetThinkess * 3,
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
            RectangelAdorner rectangelAdorner = new RectangelAdorner(this);
            adornerLayer.Add(rectangelAdorner);
            rectangelAdorner.SelectAnchor += RectangelAdorner_SelectAnchor;
            IsInit = true;
        }

        private void RectangelAdorner_SelectAnchor(object sender, CadAnchor e)
        {
            Selected?.Invoke(e, e.IsSelected);
        }

        public void SetTwoPoint(Point point)
        {
            this.LRect.P2.MX = point.X;
            this.LRect.P2.MY = point.Y;
        }
    }

    public class RectangelAdorner : Adorner
    {
        public event EventHandler<CadAnchor> SelectAnchor;

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

            AddAnchor(new CadAnchor(adornedElement.LRect.P1));
            AddAnchor(new CadAnchor(adornedElement.LRect.P2, adornedElement.LRect.P1));
            AddAnchor(new CadAnchor(adornedElement.LRect.P1, adornedElement.LRect.P2));
            AddAnchor(new CadAnchor(adornedElement.LRect.P2));

            foreach (CadAnchor anchor in _Anchors)
            {
                _Visuals.Add(anchor);
            }
        }

        private void AddAnchor(CadAnchor anchor)
        {
            anchor.Selected += Anchor_Selected;
            _Anchors.Add(anchor);
        }

        private void Anchor_Selected(object sender, bool e)
        {
            SelectAnchor?.Invoke(this, (CadAnchor)sender);
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

