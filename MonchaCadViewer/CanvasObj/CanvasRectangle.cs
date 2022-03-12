using CadProjectorViewer.CanvasObj;
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
using System.Windows.Data;
using CadProjectorSDK.Interfaces;
using AppSt = CadProjectorViewer.Properties.Settings;

namespace CadProjectorViewer.CanvasObj
{
    public class CanvasRectangle : CanvasObject
    {
        #region Property
        public event PropertyChangedEventHandler PropertyChanged;

        public void OnPropertyChanged([CallerMemberName] string prop = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop));
        }
        #endregion

        public CadRect3D LRect => (CadRect3D)this.CadObject;


        public SolidColorBrush BackColorBrush;

        public CanvasRectangle(CadRect3D rectangle, string Label) : base(rectangle, true)
        {
            this.NameID = Label;
        }


        protected override void OnInitialized(EventArgs e)
        {
            base.OnInitialized(e);
            ContextMenuLib.CadRectMenu(this.ContextMenu);
            RectangelAdorner rectangelAdorner = new RectangelAdorner(this);
            Binding binding = new Binding()
            {
                Source = (IDrawingObject)this.CadObject,
                Path = new PropertyPath("IsInit"),
                Converter = new InitVisible()
            };
            rectangelAdorner.SetBinding(Adorner.VisibilityProperty, binding);

            adornerLayer.Add(rectangelAdorner);
        }


        protected override void OnContextMenuClosing(ContextMenuEventArgs e)
        {
            base.OnContextMenuClosing(e);
            if (this.ContextMenu.DataContext is MenuItem menuItem)
            {
                switch (menuItem.Tag)
                {
                    case "common_Setting":
                        CadRectangleSizePanel cadRectangleSizePanel = new CadRectangleSizePanel() { DataContext = this };
                        cadRectangleSizePanel.Show();
                        break;

                }
            }
        }

        protected override void OnRender(DrawingContext drawingContext)
        {
            double textsize = Math.Max((CadObject.GetThinkess?.Invoke() ?? 1) * AppSt.Default.default_thinkess_percent * 5, 1d);

            drawingContext.DrawText(
                new FormattedText(this.NameID.ToString(),
                new System.Globalization.CultureInfo("ru-RU"),
                FlowDirection.LeftToRight,
                    new Typeface("Segoe UI"),
                    textsize,
                    Brushes.Gray),
                new Point(X, Y));

            drawingContext.DrawRectangle(BackColorBrush, myPen, new Rect(LRect.X, LRect.Y, LRect.Width, LRect.Height));
        }


        private void RectangelAdorner_SelectAnchor(object sender, CanvasAnchor e)
        {
            //Selected?.Invoke(e, e.CadObject.IsSelected);
        }

    }

    public class RectangelAdorner : Adorner
    {
        private VisualCollection _Visuals;

        private List<CanvasAnchor> _Anchors;

        private CanvasRectangle rectangle;
        // Be sure to call the base class constructor.
        public RectangelAdorner(CanvasRectangle adornedElement): base(adornedElement)
        {
            _Visuals = new VisualCollection(this);
            _Anchors = new List<CanvasAnchor>();

            CadRect3D cadRect3D = (CadRect3D)adornedElement.CadObject;

            this.rectangle = adornedElement;
            adornedElement.LRect.P1.GetThinkess = cadRect3D.GetThinkess;
            adornedElement.LRect.P2.GetThinkess = cadRect3D.GetThinkess;
            AddAnchor(new CanvasAnchor(adornedElement.LRect.P1));
            AddAnchor(new CanvasAnchor((adornedElement.LRect.P2)));
            AddAnchor(new CanvasAnchor(new CadAnchor(adornedElement.LRect.P2.PointX, adornedElement.LRect.P1.PointY, adornedElement.LRect.P1.M)
            {
                GetThinkess = cadRect3D.GetThinkess
            }));
            AddAnchor(new CanvasAnchor(new CadAnchor(adornedElement.LRect.P1.PointX, adornedElement.LRect.P2.PointY, adornedElement.LRect.P1.M)
            {
                GetThinkess = cadRect3D.GetThinkess
            }));


            foreach (CanvasAnchor anchor in _Anchors)
            {
                _Visuals.Add(anchor);
            }
        }

        private void AddAnchor(CanvasAnchor anchor)
        {
            _Anchors.Add(anchor);
        }


        protected override Size ArrangeOverride(Size finalSize)
        {
            foreach (CanvasAnchor anchor in _Anchors)
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

