using MonchaCadViewer.CanvasObj.DimObj;
using MonchaSDK;
using MonchaSDK.Object;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using ToGeometryConverter.Object;
using ToGeometryConverter.Object.Elements;

namespace MonchaCadViewer.CanvasObj
{
    public class CadGeometry : CadObject
    {
        public IGCObject GCObject { get; set; }

        public override  Geometry GetGeometry => this.GCObject.GetGeometry(this.TransformGroup, this.ProjectionSetting.PointStep.MX, this.ProjectionSetting.RadiusEdge);
    
        private bool _maincanvas;
        private AdornerContourFrame adornerContour;

        public CadGeometry(IGCObject gCObject,  bool maincanvas) 
        {
            this.GCObject = gCObject;
            this._maincanvas = maincanvas;
            this.Loaded += CadContour_Loaded;
        }

        private void CadContour_Loaded(object sender, RoutedEventArgs e)
        {
            if (this._maincanvas)
            {
                ContextMenuLib.ViewContourMenu(this.ContextMenu);
                this.Cursor = Cursors.Hand;
            }
        }

        public LObjectList GetTransformPoints()
        {
            LObjectList lObjectList = new LObjectList();
            Transform3DGroup transform3DGroup = this.TransformGroup;


            List<PointsElement> Points = this.Dispatcher.Invoke<List<PointsElement>>(() => { 
            return this.GCObject.GetPointCollection(transform3DGroup, this.ProjectionSetting.PointStep.MX, this.ProjectionSetting.RadiusEdge);
            });

            foreach (PointsElement points in Points)
            {
                lObjectList.Add(new LObject(points.GetPoints3D)
                {
                    ProjectionSetting = this.ProjectionSetting,
                    Closed = points.IsClosed
                });
            }

            return lObjectList;
        }

    }
}

