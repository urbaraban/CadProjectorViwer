using CadProjectorSDK.CadObjects;
using CadProjectorSDK.Device;
using CadProjectorSDK.Device.Mesh;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;

namespace CadProjectorViewer.CanvasObj
{
    public class CanvasMesh : CanvasObject
    {
        public LDeviceMesh Mesh => (LDeviceMesh)this.CadObject;

        public CanvasMesh(LDeviceMesh mesh) : base(mesh, true)
        {
        }
        protected override void OnInitialized(EventArgs e)
        {
            base.OnInitialized(e);
            MeshAdorner lineAdorner = new MeshAdorner(this);
            adornerLayer.Add(lineAdorner);
        }


        protected override void OnRender(DrawingContext drawingContext)
        {
            for (int i = 0; i < Mesh.Points.GetLength(0); i += 1)
            {
                for (int j = 0; j < Mesh.Points.GetLength(1); j += 1)
                {
                    if (i - 1 > -1) drawingContext.DrawLine(myPen, Mesh.Points[i, j].GetMPoint, Mesh.Points[i - 1, j].GetMPoint);
                    if (j - 1 > -1) drawingContext.DrawLine(myPen, Mesh.Points[i, j].GetMPoint, Mesh.Points[i, j - 1].GetMPoint);
                }
            }
            
        }

    }
    public class MeshAdorner : Adorner
    {
        private VisualCollection _Visuals;

        private List<CanvasAnchor> _Anchors;

        private CanvasMesh Mesh;

        public MeshAdorner(CanvasMesh mesh) : base(mesh)
        {
            this.Mesh = mesh;

            _Visuals = new VisualCollection(this);
            _Anchors = new List<CanvasAnchor>();

            for (int i = 0; i < mesh.Mesh.Points.GetLength(0); i += 1)
            {
                for (int j = 0; j < mesh.Mesh.Points.GetLength(1); j += 1)
                {
                    _Anchors.Add(new CanvasAnchor(new CadAnchor(mesh.Mesh[i,j])));
                }
            }

            Mesh.PropertyChanged += Line_PropertyChanged;

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
            return this.Mesh.Bounds.Size;
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
