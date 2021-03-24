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
    public class CadObjectsGroup : CadObject, IList<CadObject>
    {
        private List<CadObject> cadObjects = new List<CadObject>();

        private bool Opened = false;

        public CadObjectsGroup(GCCollection gcCollection, string Name)
        {
            this.Name = Name;
            this.GCObject = gcCollection;

            this.UpdateTransform(null, true, gcCollection.Bounds);

            foreach (IGCObject gC in gcCollection)
            {
                this.cadObjects.Add(new CadGeometry(gC, true)
                {
                    TransformGroup = this.TransformGroup,
                    Name = this.Name,
                });
            }

            
        }

        public override Geometry GetGeometry
        {
            get
            {
                GeometryGroup geometryGroup = new GeometryGroup();
                foreach (CadObject cadObject in cadObjects)
                {
                    geometryGroup.Children.Add(cadObject.GetGeometry);
                }
                return geometryGroup;
            }
        }

        #region IList<CadObject>
        public CadObject this[int index] { get => ((IList<CadObject>)cadObjects)[index]; set => ((IList<CadObject>)cadObjects)[index] = value; }

        public int Count => ((ICollection<CadObject>)cadObjects).Count;

        public bool IsReadOnly => ((ICollection<CadObject>)cadObjects).IsReadOnly;

        public void Add(CadObject item)
        {
            ((ICollection<CadObject>)cadObjects).Add(item);
        }

        public void Clear()
        {
            ((ICollection<CadObject>)cadObjects).Clear();
        }

        public bool Contains(CadObject item)
        {
            return ((ICollection<CadObject>)cadObjects).Contains(item);
        }

        public void CopyTo(CadObject[] array, int arrayIndex)
        {
            ((ICollection<CadObject>)cadObjects).CopyTo(array, arrayIndex);
        }

        public IEnumerator<CadObject> GetEnumerator()
        {
            return ((IEnumerable<CadObject>)cadObjects).GetEnumerator();
        }

        public int IndexOf(CadObject item)
        {
            return ((IList<CadObject>)cadObjects).IndexOf(item);
        }

        public void Insert(int index, CadObject item)
        {
            ((IList<CadObject>)cadObjects).Insert(index, item);
        }

        public bool Remove(CadObject item)
        {
            return ((ICollection<CadObject>)cadObjects).Remove(item);
        }

        public void RemoveAt(int index)
        {
            ((IList<CadObject>)cadObjects).RemoveAt(index);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable)cadObjects).GetEnumerator();
        }
        #endregion
    }
}
