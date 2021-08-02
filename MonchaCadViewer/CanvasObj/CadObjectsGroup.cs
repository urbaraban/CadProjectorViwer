using MonchaSDK;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using ToGeometryConverter.Object;
using ToGeometryConverter.Object.Elements;
using AppSt = MonchaCadViewer.Properties.Settings;

namespace MonchaCadViewer.CanvasObj
{
    public class CadObjectsGroup : CadObject, IList<CadObject>
    {
        public ObservableCollection<CadObject> cadObjects = new ObservableCollection<CadObject>();

        private bool Opened = false;

        private GCCollection elements;

        private void ParseColletion(GCCollection objects)
        {
            List<CadObject> geometries = new List<CadObject>();

            foreach (IGCObject gC in objects)
            {
                if (gC is GCCollection collection)
                {
                    this.AddRange(new CadObjectsGroup(collection));
                }
                else
                {
                    this.Add(new CadGeometry(gC, true)
                    {
                        NameID = gC.Name
                    });
                }
            }

            if (AppSt.Default.stg_show_name == true)
            {
                geometries.Add(new CadGeometry(
                    new ToGeometryConverter.Object.Elements.TextElement(objects.Name, MonchaHub.GetThinkess,
                    new Point3D(0, 0, 0)),
                    true));
            }
        }

        public override Geometry GetGeometry 
        {
            get
            {
                GeometryGroup geometryGroup = new GeometryGroup();
                foreach (CadObject element in this.cadObjects)
                {
                    if (element.Render == true)
                    {
                        geometryGroup.Children.Add(element.GetGeometry);
                    }
                }
                return geometryGroup;
            }
         }


        public override Rect Bounds => GetGeometry.Bounds;


        public CadObjectsGroup(GCCollection gCCollection)
        {
            this.UpdateTransform(null, true, gCCollection.Bounds);
            ParseColletion(gCCollection);
            this.NameID = gCCollection.Name;
        }


        #region IList<CadObject>
        public CadObject this[int index] { get => ((IList<CadObject>)cadObjects)[index]; set => ((IList<CadObject>)cadObjects)[index] = value; }

        public int Count => ((ICollection<CadObject>)cadObjects).Count;

        public bool IsReadOnly => ((ICollection<CadObject>)cadObjects).IsReadOnly;

        public void AddRange(IList<CadObject> cadObjects)
        {
            foreach (CadObject cadObject in cadObjects) this.Add(cadObject);
        }

        public void Add(CadObject item)
        {
            item.TransformGroup = this.TransformGroup;
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
