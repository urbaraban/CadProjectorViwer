using CadProjectorSDK.CadObjects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Media;

namespace CadProjectorViewer.CanvasObj
{
    public class CanvasLine : CanvasObject
    {
        public CadLine Line => (CadLine)base.CadObject;

        public CanvasLine(CadLine line) : base(line, true)
        {

        }


        public bool IsInit { get; private set; } = false;

        protected override void OnInitialized(EventArgs e)
        {
            base.OnInitialized(e);
            LineAdorner lineAdorner = new LineAdorner(this);
            Binding binding = new Binding()
            {
                Source = this.CadObject,
                Path = new PropertyPath("IsInit"),
            };
            lineAdorner.SetBinding(Adorner.VisibilityProperty, binding);
            adornerLayer.Add(lineAdorner);
            IsInit = true;
        }

        protected override void OnRender(DrawingContext drawingContext)
        {
            drawingContext.DrawLine(myPen, Line.P1.GetMPoint, Line.P2.GetMPoint);
        }
    }

    public class LineAdorner : Adorner
    {
        private VisualCollection _Visuals;

        private List<CanvasAnchor> _Anchors;

        private CanvasLine canvasLine;

        public LineAdorner(CanvasLine line) : base(line)
        {
            this.canvasLine = line;

            _Visuals = new VisualCollection(this);
            _Anchors = new List<CanvasAnchor>();

            _Anchors.Add(new CanvasAnchor(new CadAnchor(line.Line.P1)));
            _Anchors.Add(new CanvasAnchor(new CadAnchor(line.Line.P2)));

            line.Line.PropertyChanged += Line_PropertyChanged;

            foreach (CanvasAnchor anchor in _Anchors)
            {
                _Visuals.Add(anchor);
            }
        }

        private void Line_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            this.InvalidateVisual();
        }

        protected override Size ArrangeOverride(Size finalSize)
        {
            foreach (CanvasAnchor anchor in _Anchors)
            {
                anchor.Arrange(new Rect(finalSize));
            }
            return this.canvasLine.Bounds.Size;
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
