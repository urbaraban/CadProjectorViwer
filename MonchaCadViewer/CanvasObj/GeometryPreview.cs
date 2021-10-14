using CadProjectorSDK.CadObjects.Abstract;
using CadProjectorSDK.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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

            drawingContext.DrawGeometry(myBack, myPen, Geometry.GetGeometry());
        }
    }
}
