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

        public override Rect Bounds => GCObject.Bounds;

        public override string NameID => GCObject.Name;

        public override  Geometry GetGeometry => this.GCObject.GetGeometry(this.TransformGroup, this.ProjectionSetting.PointStep.MX, this.ProjectionSetting.RadiusEdge);
    
        public CadGeometry(IGCObject gCObject, bool ActiveObject) : base(ActiveObject)
        {
            this.GCObject = gCObject;
            this.Cursor = Cursors.Hand;
            ContextMenuLib.ViewContourMenu(this.ContextMenu);
        }

        public CadGeometry(LObjectList ObjectsList, bool ActiveObject) : base(ActiveObject)
        {
            GCCollection gCObjects = new GCCollection(ObjectsList.DisplayName);
            foreach (LObject obj in ObjectsList)
            {
                PointsElement Points = new PointsElement() { IsClosed = obj.Closed } ;
                foreach (LPoint3D point in obj)
                {
                    Points.Add(new GCPoint3D(point.X, point.Y, point.Z));
                }
                gCObjects.Add(Points);
            }

            this.GCObject = gCObjects;
            this.Cursor = Cursors.Hand;
            ContextMenuLib.ViewContourMenu(this.ContextMenu);
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

