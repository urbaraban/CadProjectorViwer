using CadProjectorSDK.CadObjects;
using CadProjectorSDK.CadObjects.Abstract;
using CadProjectorSDK.CadObjects.LObjects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ToGeometryConverter.Object;
using ToGeometryConverter.Object.Elements;

namespace CadProjectorViewer.StaticTools
{
    public static class GCToCad
    {
        public static UidObject GetUid(IGCObject gCObject)
        {
            if (gCObject is GeometryElement geometryElement)
            {
                return new CadGeometry(geometryElement.MyGeometry, true);
            }
            else if (gCObject is TextElement textElement) 
            {
                return new CadText(
                    textElement.Text,
                    new CadPoint3D(textElement.Point.X, textElement.Point.Y, textElement.Point.Z),
                    textElement.Size
                    );
            }
            else if (gCObject is PointsElement point3Ds)
            {
                CadPointsObject cadPoints = new CadPointsObject();
                foreach (GCPoint3D point in point3Ds)
                {
                    cadPoints.Add(new CadPoint3D(point.X, point.Y, point.Z));
                }
                return cadPoints;
            }

            return null;
        }
    }
}
