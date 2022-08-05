using CadProjectorSDK.CadObjects;
using System;
using System.Collections.Generic;
using System.Globalization;
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

        protected override void OnInitialized(EventArgs e)
        {
            base.OnInitialized(e);
            LineAdorner lineAdorner = new LineAdorner(this);
            Binding binding = new Binding()
            {
                Source = this.CadObject,
                Path = new PropertyPath("IsInit"),
                Converter = new InitVisible()
            };
            lineAdorner.SetBinding(Adorner.VisibilityProperty, binding);
            adornerLayer.Add(lineAdorner);
        }

        protected override void OnRender(DrawingContext drawingContext)
        {
            drawingContext.DrawLine(GetPen(), 
                new Point(this.Line.P1.MX, this.Line.P1.MY), 
                new Point(this.Line.P2.MX, this.Line.P2.MY));
        }
    }

    public class LineAdorner : Adorner
    {
        private VisualCollection _Visuals;

        private List<CanvasAnchor> _Anchors;

        private CanvasLine canvasLine;

        public LineAdorner(CanvasLine line) : base(line)
        {
            this.IsClipEnabled = true;
            this.canvasLine = line;

            _Visuals = new VisualCollection(this);
            _Anchors = new List<CanvasAnchor>();

            CanvasAnchor A1 = new CanvasAnchor(line.Line.P1) { GetCanvas = line.GetCanvas };
            line.SizeChange += A1.ParentChangeSize;
            _Anchors.Add(A1);

            CanvasAnchor A2 = new CanvasAnchor(line.Line.P2) { GetCanvas = line.GetCanvas };
            line.SizeChange += A2.ParentChangeSize;
            _Anchors.Add(A2);
            
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
