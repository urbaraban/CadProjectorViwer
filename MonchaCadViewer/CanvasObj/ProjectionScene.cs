using CadProjectorViewer.Interface;
using CadProjectorSDK.CadObjects;
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
using CadProjectorSDK;

namespace CadProjectorViewer.CanvasObj
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

        public CanvasObject LastSelectObject 
        {
            get => lastselectobject;
            set
            {
                lastselectobject = value;
                OnPropertyChanged("LastSelectObject");
            }
        }
        private CanvasObject lastselectobject;

        public ObservableCollection<CanvasObject> SelectedObjects = new ObservableCollection<CanvasObject>();

        public ObservableCollection<CanvasObject> Objects { get; } = new ObservableCollection<CanvasObject>();
        public ObservableCollection<CanvasRectangle> Masks { get; } = new ObservableCollection<CanvasRectangle>();

        public IDrawingObject ActiveDrawingObject { get; set; }

        public CadPoint3D MousePosition { get; } = new CadPoint3D(0,0,0);

        public ProjectionScene()
        {
            Objects.CollectionChanged += Objects_CollectionChanged;
            SelectedObjects.CollectionChanged += SelectedObjects_CollectionChanged;
        }

        public ProjectionScene(CanvasObject Obj)
        {
            Objects.Add(Obj);
        }

        private void Objects_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            if (e.NewItems != null)
            {
                foreach (CanvasObject Obj in e.NewItems)
                {
                    Obj.Selected += Obj_Selected;
                    Obj.Removed += Obj_Removed;
                    Obj.PropertyChanged += Obj_PropertyChanged;
                }
                //UpdateFrame?.Invoke(this, null);
            }
            if (e.OldItems != null)
            {
                foreach (CanvasObject Obj in e.OldItems)
                {
                    Obj.Selected -= Obj_Selected;
                    Obj.Removed -= Obj_Removed;
                    Obj.PropertyChanged -= Obj_PropertyChanged;
                }
                //UpdateFrame?.Invoke(this, null);
            }
        }

        private void Obj_Removed(object sender, CanvasObject e)
        {
            this.Remove(e);
        }

        private string[] IgnoreUpdate = new string[]{ "IsMouseOver" };

        private void Obj_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (IgnoreUpdate.Contains(e.PropertyName) == false)
            {
                Console.WriteLine($"FrameUpdate: {e.PropertyName}");
                UpdateFrame?.Invoke(this, null);
            }
        }


        #region SelectionManager
        private void Obj_Selected(object sender, bool e)
        {
            if (sender is CanvasObject cadObject)
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
                LastSelectObject = (CanvasObject)e.NewItems[e.NewItems.Count - 1];

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
                foreach (CanvasObject Obj in e.OldItems)
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

        public void AddRange(IList<CanvasObject> cadObjects)
        {
            foreach (CanvasObject cadObject in cadObjects)
            {
                this.Add(cadObject);
            }
            UpdateFrame?.Invoke(this, null);
        }

        public CanvasObject Add(CanvasObject cadObject)
        {
            if (Objects.Contains(cadObject) == false)
            {
                if (cadObject is CadObjectsGroup cadGeometries)
                {
                    foreach (CanvasObject obj in cadGeometries.Children)
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
            foreach (CanvasObject cadObject in scene.Objects)
            {
                this.Add(cadObject);
            }
        }

        public void Remove(ProjectionScene scene)
        {
            foreach (CanvasObject cadObject in scene.Objects)
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
            if (this.Objects.Where(i => i.Uid == cadObjectsGroup.Uid).FirstOrDefault() is CanvasObject remobj)
            {
                Remove(remobj);
            }
            else
            {
                foreach (CanvasObject cadObject in cadObjectsGroup) 
                {
                    if (cadObject is CadObjectsGroup objectsGroup)
                    {
                        Remove(objectsGroup);
                    }
                    else Remove(cadObject);
                }
            }
        }

        public void Remove(CanvasObject obj)
        {
            if (obj is CanvasObject cadObject)
            {
                if (this.Objects.Remove(this.Objects.Where(i => i.Uid == cadObject.Uid).FirstOrDefault()) == true)
                {
                    if (cadObject is CanvasRectangle rectangle)
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
                this.Remove((CanvasObject)this.ActiveDrawingObject);
                this.ActiveDrawingObject = null;
            }
        }
    }
}
