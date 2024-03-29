﻿using CadProjectorSDK.CadObjects;
using CadProjectorSDK.Device;
using CadProjectorSDK.Device.Mesh;
using CadProjectorViewer.ViewModel;
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
        public ProjectorMesh Mesh => (ProjectorMesh)this.CadObject;

        public CanvasMesh(ProjectorMesh mesh) : base(mesh, true)
        {
            mesh.ChangedMesh += Mesh_ChangedMesh;
            this.CadObject.PropertyChanged += CadObject_PropertyChanged;
        }

        private void CadObject_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            OnPropertyChanged("X");
            OnPropertyChanged("Y");
        }

        private void Mesh_ChangedMesh(object sender, bool e)
        {
            this.Dispatcher.Invoke(() =>
            {
                this.InvalidateVisual();
            });
        }

        protected override void OnInitialized(EventArgs e)
        {
            base.OnInitialized(e);
            MeshAdorner lineAdorner = new MeshAdorner(this);
            adornerLayer.Add(lineAdorner);
        }


        protected override async void OnRender(DrawingContext drawingContext)
        {
            Pen pen = this.GetPen(true);
            RenderDeviceModel deviceModel = this.GetViewModel?.Invoke();

            double propwidth = deviceModel.Size.MWidth;
            double propheight = deviceModel.Size.MHeight;

            for (int i = 0; i < Mesh.Points.GetLength(0); i += 1)
            {
                for (int j = 0; j < Mesh.Points.GetLength(1); j += 1)
                {
                    Point ProportionPoint1 = deviceModel.GetProportion(Mesh.Points[i, j].MX, Mesh.Points[i, j].MY);
                    Point RenderPoint1 = deviceModel.GetPoint(ProportionPoint1.X, ProportionPoint1.Y);

                    if (i - 1 > -1)
                    {
                        Point ProportionPoint2 = deviceModel.GetProportion(Mesh.Points[i - 1, j].MX, Mesh.Points[i - 1, j].MY);
                        Point RenderPoint2 = deviceModel.GetPoint(ProportionPoint2.X, ProportionPoint2.Y);

                        drawingContext.DrawLine(pen, RenderPoint1, RenderPoint2);
                    }
                        
                    if (j - 1 > -1)
                    {
                        Point ProportionPoint2 = deviceModel.GetProportion(Mesh.Points[i, j - 1].MX, Mesh.Points[i, j - 1].MY);
                        Point RenderPoint2 = deviceModel.GetPoint(ProportionPoint2.X, ProportionPoint2.Y);

                        drawingContext.DrawLine(pen, RenderPoint1, RenderPoint2);
                    } 
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
            this.IsClipEnabled = true;
            this.Mesh = mesh;
            _Visuals = new VisualCollection(this);
            _Anchors = new List<CanvasAnchor>();

            for (int i = 0; i < mesh.Mesh.Points.GetLength(0); i += 1)
            {
                for (int j = 0; j < mesh.Mesh.Points.GetLength(1); j += 1)
                {
                    _Anchors.Add(new CanvasAnchor(mesh.Mesh[i, j])
                    {
                        GetViewModel = mesh.GetViewModel,
                        GetFrameTransform = mesh.GetFrameTransform
                    });
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
            this.Dispatcher.Invoke(() => this.InvalidateVisual());
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
