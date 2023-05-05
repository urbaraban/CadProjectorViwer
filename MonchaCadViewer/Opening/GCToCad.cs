using CadProjectorSDK.CadObjects;
using CadProjectorSDK.CadObjects.Abstract;
using System.Threading.Tasks;
using System.Windows.Media;
using ToGeometryConverter.Object;
using ToGeometryConverter.Object.Elements;

namespace CadProjectorViewer.Opening
{
    public static class GCToCad
    {
        public async static Task<UidObject> GetUid(IGCObject gCObject)
        {
            if (gCObject is GeometryElement geometryElement)
            {
                return new CadGeometry(geometryElement.MyGeometry);
            }
            else if (gCObject is TextElement textElement)
            {
                return new CadText(
                    textElement.Text,
                    new CadAnchor(textElement.Point.X, textElement.Point.Y, textElement.Point.Z),
                    textElement.Size);
            }
            else if (gCObject is PointsElement point3Ds)
            {
                LineGeometry lineGeometry = new LineGeometry(
                    new System.Windows.Point(point3Ds[0].X, point3Ds[0].Y),
                    new System.Windows.Point(point3Ds[1].X, point3Ds[1].Y));
                return new CadGeometry(lineGeometry);
            }
            else if (gCObject is GCCollection collection)
            {
                if (collection.Count > 0)
                    return await GetGroup(collection);
            }

            return null;
        }

        public async static Task<CadGroup> GetGroup(GCCollection gCObjects)
        {
            CadGroup group = new CadGroup() { NameID = gCObjects.Name };
            foreach (IGCObject obj in gCObjects)
            {
                UidObject uidObject = await GCToCad.GetUid(obj);
                if (uidObject != null)
                {
                    group.Add(uidObject);
                }
            }
            return group.Count > 0 ? group : null;
        }
    }
}
