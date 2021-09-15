using MonchaCadViewer.Interface;
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
            }
        }
        private CadObject lastselectobject;

        public ObservableCollection<CadObject> SelectedObjects = new ObservableCollection<CadObject>();

        public ObservableCollection<CadObject> Objects { get; } = new ObservableCollection<CadObject>();
        public ObservableCollection<CadRectangle> Masks { get; } = new ObservableCollection<CadRectangle>();

        public IDrawingObject ActiveDrawingObject { get; set; }

        public ProjectionScene()
        {
            Objects.CollectionChanged += Objects_CollectionChanged;
            SelectedObjects.CollectionChanged += SelectedObjects_CollectionChanged;
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
                    Obj.Removed += Obj_Removed;
                    Obj.PropertyChanged += Obj_PropertyChanged;
                }
            }
            if (e.OldItems != null)
            {
                foreach (CadObject Obj in e.OldItems)
                {
                    Obj.Selected -= Obj_Selected;
                    Obj.Removed -= Obj_Removed;
                    Obj.PropertyChanged -= Obj_PropertyChanged;
                }
            }
        }

        private void Obj_Removed(object sender, CadObject e)
        {
            this.Remove(e);
        }

        private void Obj_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            Console.WriteLine($"FrameUpdate: {e.PropertyName}");
            UpdateFrame?.Invoke(this, null);
        }


        #region SelectionManager
        private void Obj_Selected(object sender, bool e)
        {
            if (sender is CadObject cadObject)
            {
                if (e == true)
                {
                    Console.WriteLine("Obj Select");
                    SelectedObjects.Add(cadObject);
                }
                else
                {
                    if (SelectedObjects.Contains(cadObject))
                    {
                        SelectedObjects.Remove(cadObject);
                    }
                }
            }
        }

        private void SelectedObjects_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            if (e.NewItems != null)
            {
                LastSelectObject = (CadObject)e.NewItems[e.NewItems.Count - 1];

                if (Keyboard.Modifiers != ModifierKeys.Shift)
                {
                    for (int i = SelectedObjects.Count - 2; i > -1 ; i -= 1)
                    {
                        SelectedObjects[i].IsSelected = false;
                    }
                }

                

            }
            if (e.OldItems != null)
            {
                foreach (CadObject Obj in e.OldItems)
                {
                    if (LastSelectObject == Obj && SelectedObjects.Count > 0)
                    {
                        LastSelectObject = SelectedObjects.Last();
                    }
                    else if (LastSelectObject == Obj)
                    {
                        LastSelectObject = null;
                    }
                }
            }
        }

        #endregion

        public void AddRange(IList<CadObject> cadObjects)
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
                    foreach (CadObject obj in cadGeometries.Children)
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
                if (cadObject is CadObjectsGroup cadGeometries)
                {
                    Remove(cadGeometries);
                }
                else
                {
                    this.Remove(cadObject);
                }
            }
        }

        public void Remove(CadObjectsGroup cadObjectsGroup)
        {
            if (this.Objects.Where(i => i.Uid == cadObjectsGroup.Uid).FirstOrDefault() is CadObject remobj)
            {
                Remove(remobj);
            }
            else
            {
                foreach (CadObject cadObject in cadObjectsGroup) 
                {
                    if (cadObject is CadObjectsGroup objectsGroup)
                    {
                        Remove(objectsGroup);
                    }
                    else Remove(cadObject);
                }
            }
        }

        public void Remove(CadObject obj)
        {
            if (obj is CadObject cadObject)
            {
                if (this.Objects.Remove(this.Objects.Where(i => i.Uid == cadObject.Uid).FirstOrDefault()) == true)
                {
                    if (cadObject is CadRectangle rectangle)
                    {
                        this.Masks.Remove(this.Masks.Where(i => i.Uid == cadObject.Uid).FirstOrDefault());
                    }
                }
            }
        }

        public void Clear()
        {
            Objects.Clear();
            Masks.Clear();
        }

        public void Cancel()
        {
            if (this.ActiveDrawingObject != null)
            {
                this.Remove((CadObject)this.ActiveDrawingObject);
                this.ActiveDrawingObject = null;
            }
        }
    }
}
