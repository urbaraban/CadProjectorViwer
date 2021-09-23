using CadProjectorSDK;
using CadProjectorSDK.CadObjects;
using CadProjectorSDK.CadObjects.LObjects;
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
using AppSt = CadProjectorViewer.Properties.Settings;

namespace CadProjectorViewer.CanvasObj
{
    public class CadObjectsGroup : CanvasObject, IList<CanvasObject>
    {
        private bool Opened = false;

        private void ParseColletion(GCCollection objects)
        {
            List<CanvasObject> geometries = new List<CanvasObject>();

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
                    new ToGeometryConverter.Object.Elements.TextElement(objects.Name, ProjectorHub.GetThinkess,
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
                foreach(CanvasObject cadObject in Children)
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
                foreach (CanvasObject element in this.Children)
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

        public CadObjectsGroup() : base(true)
        {

        }

        public CadObjectsGroup(GCCollection gCCollection) : base(true)
        {
            this.gCCollection = gCCollection;
            this.UpdateTransform(true);
            ParseColletion(gCCollection);
            this.NameID = gCCollection.Name;
        }

        public CadObjectsGroup(PointsObjectList ObjectsList, bool ActiveObject) : base(ActiveObject)
        {
            foreach (PointsObject obj in ObjectsList)
            {
                PointsElement Points = new PointsElement() { IsClosed = obj.IsClosed };
                foreach (CadPoint3D point in obj)
                {
                    Points.Add(new GCPoint3D(point.X, point.Y, point.Z));
                }
                this.Add(new CadGeometry(Points, true));
            }


            this.Cursor = Cursors.Hand;
            ContextMenuLib.ViewContourMenu(this.ContextMenu);
        }


        #region IList<CadObject>
        public CanvasObject this[int index] { get => ((IList<CanvasObject>)Children)[index]; set => ((IList<CanvasObject>)Children)[index] = value; }

        public int Count => ((ICollection<CanvasObject>)Children).Count;

        public bool IsReadOnly => ((ICollection<CanvasObject>)Children).IsReadOnly;

        public void AddRange(IList<CanvasObject> Children)
        {
            foreach (CanvasObject cadObject in Children) this.Add(cadObject);
        }

        public void Add(CanvasObject item)
        {
            item.TransformGroup = this.TransformGroup;
            item.PropertyChanged += Item_PropertyChanged;
            ((ICollection<CanvasObject>)Children).Add(item);
        }

        private void Item_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            this.InvalidateVisual();
            OnPropertyChanged();
        }

        public void Clear()
        {
            ((ICollection<CanvasObject>)Children).Clear();
        }

        public bool Contains(CanvasObject item)
        {
            return ((ICollection<CanvasObject>)Children).Contains(item);
        }

        public void CopyTo(CanvasObject[] array, int arrayIndex)
        {
            ((ICollection<CanvasObject>)Children).CopyTo(array, arrayIndex);
        }

        public IEnumerator<CanvasObject> GetEnumerator()
        {
            return ((IEnumerable<CanvasObject>)Children).GetEnumerator();
        }

        public int IndexOf(CanvasObject item)
        {
            return ((IList<CanvasObject>)Children).IndexOf(item);
        }

        public void Insert(int index, CanvasObject item)
        {
            ((IList<CanvasObject>)Children).Insert(index, item);
        }

        public bool Remove(CanvasObject item)
        {
            return ((ICollection<CanvasObject>)Children).Remove(item);
        }

        public void RemoveAt(int index)
        {
            ((IList<CanvasObject>)Children).RemoveAt(index);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable)Children).GetEnumerator();
        }
        #endregion
    }
}
