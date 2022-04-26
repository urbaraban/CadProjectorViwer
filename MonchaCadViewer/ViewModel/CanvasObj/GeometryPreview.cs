using CadProjectorSDK.CadObjects;
using CadProjectorSDK.CadObjects.Abstract;
using CadProjectorSDK.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Media;

namespace CadProjectorViewer.CanvasObj
{
    public class GeometryPreview : CanvasObject
    {
        public IGeometryObject Geometry => (IGeometryObject)this.CadObject;

        public GeometryPreview(UidObject Object) : base(Object, true)
        {


        }

        protected override void OnRender(DrawingContext drawingContext)
        {
            base.OnRender(drawingContext);
            Drawing(this.CadObject, this.IsMouseOver, true, StrokeThinkess, drawingContext);
        }

        private static void Drawing(UidObject uidObject, bool MouseOver, bool ParentRender, double StrThink,  DrawingContext drawingContext)
        {
            if (uidObject is CadGroup group)
            {
                foreach(UidObject uid in group)
                {
                    Drawing(uid, MouseOver, ParentRender && uid.IsRender, StrThink, drawingContext);
                }
            }
            else if (uidObject is IGeometryObject geometryObject)
            {
                Geometry geometry = geometryObject.GetGeometry();
                Pen pen = GetPen(
                    StrThink, 
                    MouseOver, 
                    uidObject.IsSelected,
                    ParentRender,
                    uidObject.IsBlank,
                    uidObject.ProjectionSetting.GetBrush);

                drawingContext.DrawGeometry(GetBrush(uidObject), pen, geometry);
            }
        }
    }
}
