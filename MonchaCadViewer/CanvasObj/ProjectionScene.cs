using MonchaSDK.Object;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using ToGeometryConverter.Object;

namespace MonchaCadViewer.CanvasObj
{
    public class ProjectionScene : INotifyPropertyChanged
    {
        #region INotifyPropertyChanged
        public event PropertyChangedEventHandler PropertyChanged;
        public void OnPropertyChanged([CallerMemberName] string prop = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop));
        }
        #endregion

        public event EventHandler UpdateFrame;

        public string NameID
        {
            get => nameid;
            set
            {
                nameid = value;
                OnPropertyChanged("NameID");
            }
        }
        private string nameid = string.Empty;

        public CadObject LastSelectObject 
        {
            get => lastselectobject;
            set
            {
                lastselectobject = value;
                OnPropertyChanged("LastSelectObject");
                if (Keyboard.Modifiers != ModifierKeys.Shift)
                {
                    ClearSelectedObject(lastselectobject);
                }
            }
        }
        private CadObject lastselectobject;

        public ObservableCollection<CadObject> SelectedObject = new ObservableCollection<CadObject>();

        public ObservableCollection<CadObject> Objects { get; } = new ObservableCollection<CadObject>();
        public ObservableCollection<CadRectangle> Masks { get; } = new ObservableCollection<CadRectangle>();

        public ProjectionScene()
        {
            Objects.CollectionChanged += Objects_CollectionChanged;
            SelectedObject.CollectionChanged += SelectedObject_CollectionChanged;
        }


        public ProjectionScene(CadObject Obj)
        {
            Objects.Add(Obj);
        }

        private void Objects_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            if (e.NewItems != null)
            {
                foreach (CadObject Obj in e.NewItems)
                {
                    Obj.Selected += Obj_Selected;
                    Obj.PropertyChanged += Obj_PropertyChanged;
                }
            }
            if (e.OldItems != null)
            {
                foreach (CadObject Obj in e.OldItems)
                {
                    Obj.Selected -= Obj_Selected; 
                    Obj.PropertyChanged -= Obj_PropertyChanged;
                }
            }
        }

        private void Obj_PropertyChanged(object sender, PropertyChangedEventArgs e) => UpdateFrame?.Invoke(this, null);


        #region SelectionManager
        private void Obj_Selected(object sender, bool e)
        {
            if (sender is CadObject cadObject)
            {
                if (e == true)
                {
                    SelectedObject.Add(cadObject);
                }
                else
                {
                    if (SelectedObject.Contains(cadObject))
                    {
                        SelectedObject.Remove(cadObject);
                    }
                }
            }
        }

        private void SelectedObject_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            if (e.NewItems != null)
            {
                foreach (CadObject Obj in e.NewItems)
                {
                    LastSelectObject = Obj;
                }
            }
            if (e.OldItems != null)
            {
                foreach (CadObject Obj in e.OldItems)
                {
                    if (LastSelectObject == Obj && SelectedObject.Count > 0)
                    {
                        LastSelectObject = SelectedObject.Last();
                    }
                    else
                    {
                        LastSelectObject = null;
                    }
                }
            }
        }

        public void ClearSelectedObject(CadObject noclearobj)
        {
            foreach (CadObject obj in this.Objects)
            {
                if (obj != noclearobj)
                {
                    obj.IsSelected = false;
                }
            }
        }
        #endregion

        public void AddRange(CadObject[] cadObjects)
        {
            foreach (CadObject cadObject in cadObjects)
            {
                this.Add(cadObject);
            }
        }

        public CadObject Add(CadObject cadObject)
        {

            if (Objects.Contains(cadObject) == false)
            {
                if (cadObject is CadObjectsGroup cadGeometries)
                {
                    foreach (CadObject obj in cadGeometries.cadObjects)
                    {
                        Objects.Add(obj);
                    }
                }
                else
                {
                    Objects.Add(cadObject);
                }
            }
            return cadObject;
        }

        public void Add(ProjectionScene scene)
        {
            foreach (CadObject cadObject in scene.Objects)
            {
                this.Add(cadObject);
            }
        }

        public void Remove(ProjectionScene scene)
        {
            foreach (CadObject cadObject in scene.Objects)
            {
                this.Remove(cadObject);
            }
        }

        public void Remove(CadObject cadObject)
        {
            Objects.Remove(cadObject);
        }

        public void Clear()
        {
            Objects.Clear();
        }
    }
}
