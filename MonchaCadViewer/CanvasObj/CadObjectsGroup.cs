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
        private bool Opened = false;

        private void ParseColletion(GCCollection objects)
        {
            List<CadObject> geometries = new List<CadObject>();

            foreach (IGCObject gC in objects)
            {
                if (string.IsNullOrEmpty(gC.Name) == true) gC.Name = $"Object_{geometries.Count}";

                if (gC is GCCollection collection)
                {
                    this.Add(new CadObjectsGroup(collection));
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
                    new ToGeometryConverter.Object.Elements.TextElement(objects.Name, LaserHub.GetThinkess,
                    new Point3D(0, 0, 0)),
                    true));
            }
        }

        public override Transform3DGroup TransformGroup
        {
            get => base.TransformGroup;
            set
            {
                base.TransformGroup = value;
                foreach(CadObject cadObject in Children)
                {
                    cadObject.TransformGroup = value;
                }
            }
        }

        public override Geometry GetGeometry
        {
            get
            {
                GeometryGroup geometryGroup = new GeometryGroup();
                foreach (CadObject element in this.Children)
                {
                    if (element.Render == true)
                    {
                        geometryGroup.Children.Add(element.GetGeometry);
                    }
                }
                return geometryGroup;
            }
        }
        public override Rect Bounds => gCCollection.Bounds;

        private GCCollection gCCollection;

        public CadObjectsGroup()
        {

        }

        public CadObjectsGroup(GCCollection gCCollection)
        {
            this.gCCollection = gCCollection;
            this.UpdateTransform(true);
            ParseColletion(gCCollection);
            this.NameID = gCCollection.Name;
        }


        #region IList<CadObject>
        public CadObject this[int index] { get => ((IList<CadObject>)Children)[index]; set => ((IList<CadObject>)Children)[index] = value; }

        public int Count => ((ICollection<CadObject>)Children).Count;

        public bool IsReadOnly => ((ICollection<CadObject>)Children).IsReadOnly;

        public void AddRange(IList<CadObject> Children)
        {
            foreach (CadObject cadObject in Children) this.Add(cadObject);
        }

        public void Add(CadObject item)
        {
            item.TransformGroup = this.TransformGroup;
            item.PropertyChanged += Item_PropertyChanged;
            ((ICollection<CadObject>)Children).Add(item);
        }

        private void Item_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            this.InvalidateVisual();
            OnPropertyChanged();
        }

        public void Clear()
        {
            ((ICollection<CadObject>)Children).Clear();
        }

        public bool Contains(CadObject item)
        {
            return ((ICollection<CadObject>)Children).Contains(item);
        }

        public void CopyTo(CadObject[] array, int arrayIndex)
        {
            ((ICollection<CadObject>)Children).CopyTo(array, arrayIndex);
        }

        public IEnumerator<CadObject> GetEnumerator()
        {
            return ((IEnumerable<CadObject>)Children).GetEnumerator();
        }

        public int IndexOf(CadObject item)
        {
            return ((IList<CadObject>)Children).IndexOf(item);
        }

        public void Insert(int index, CadObject item)
        {
            ((IList<CadObject>)Children).Insert(index, item);
        }

        public bool Remove(CadObject item)
        {
            return ((ICollection<CadObject>)Children).Remove(item);
        }

        public void RemoveAt(int index)
        {
            ((IList<CadObject>)Children).RemoveAt(index);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable)Children).GetEnumerator();
        }
        #endregion
    }
}
