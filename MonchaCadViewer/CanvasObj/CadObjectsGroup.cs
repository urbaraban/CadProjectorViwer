using MonchaSDK;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using ToGeometryConverter.Object;
using AppSt = MonchaCadViewer.Properties.Settings;

namespace MonchaCadViewer.CanvasObj
{
    public class CadObjectsGroup : CadObject
    {
        public List<CadObject> cadObjects = new List<CadObject>();

        private bool Opened = false;

        public CadObjectsGroup(GeometryGroup geometry, string Name) : base(false, false)
        {
            this.Name = Name;
            this.myGeometry = geometry;
            this.UpdateTransform(null);

            foreach (Geometry tempgeometry in geometry.Children)
            {
                this.cadObjects.Add(new CadContour(tempgeometry, true, true));
                this.cadObjects.Last().TransformGroup = this.TransformGroup;
            }
        }
    }
}
