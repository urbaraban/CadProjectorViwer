using CadProjectorSDK;
using CadProjectorSDK.CadObjects;
using CadProjectorSDK.CadObjects.LObjects;
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

namespace CadProjectorViewer.CanvasObj
{
    public class CadGeometry : CanvasObject
    {
        public IGCObject GCObject { get; set; }

        public override Rect Bounds => GCObject.Bounds;

        public override string NameID => GCObject.Name;

        public override  Geometry GetGeometry => this.GCObject.GetGeometry(this.TransformGroup, this.ProjectionSetting.PointStep.MX, this.ProjectionSetting.RadiusEdge);
    
        public CadGeometry(IGCObject gCObject, bool ActiveObject) : base(ActiveObject)
        {
            this.GCObject = gCObject;
            this.Cursor = Cursors.Hand;
            ContextMenuLib.ViewContourMenu(this.ContextMenu);
        }


        public PointsObjectList GetTransformPoints()
        {
            PointsObjectList lObjectList = new PointsObjectList();
            Transform3DGroup transform3DGroup = this.TransformGroup;


            List<PointsElement> Points = this.Dispatcher.Invoke<List<PointsElement>>(() => { 
            return this.GCObject.GetPointCollection(transform3DGroup, this.ProjectionSetting.PointStep.MX, this.ProjectionSetting.RadiusEdge);
            });

            foreach (PointsElement points in Points)
            {
                lObjectList.Add(new PointsObject(points.GetPoints3D)
                {
                    ProjectionSetting = this.ProjectionSetting,
                    IsClosed = points.IsClosed
                });
            }

            return lObjectList;
        }

    }
}

