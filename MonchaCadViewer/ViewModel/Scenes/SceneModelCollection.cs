using CadProjectorSDK;
using CadProjectorSDK.Scenes;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace CadProjectorViewer.ViewModel.Scenes
{
    public class SceneModelCollection : ObservableCollection<SceneModel>, INotifyPropertyChanged
    {
        public bool StreamAdd
        {
            get => _streamadd;
            set
            {
                _streamadd = value;
                OnPropertyChanged("StreamAdd");
            }
        }
        private bool _streamadd = false;

        public TaskCollection LoadedObjects { get; } = new TaskCollection();

        public SceneModel SelectedScene
        {
            get
            {
                return _selectedscene;
            }
            set
            {
                _selectedscene = value;
                OnPropertyChanged("SelectedScene");
            }
        }
        private SceneModel _selectedscene = new SceneModel();

        public SceneModelCollection()
        {
            LoadedObjects.Selected = SelectLoaded;
        }

        public SceneModel GetSceneByID(int ID)
        {
            foreach (SceneModel scene in this)
            {
                if (scene.TableID == ID) return scene;
            }
            return null;
        }

        protected override void RemoveItem(int index)
        {
            this[index].SelectedScene -= Scene_SelectedScene;
            this[index].Removed -= Scene_RemovedScene;
            base.RemoveItem(index);
        }

        protected override void InsertItem(int index, SceneModel item)
        {
            if (this.Contains(item) == true) item.IsSelected = true;
            else
            {
                base.InsertItem(index, item);
                item.SelectedScene += Scene_SelectedScene;
                item.Removed += Scene_RemovedScene;
                item.IsSelected = true;
                if (SelectedScene == null) SelectedScene = item;
            }
        }

        public async Task AddTask(SceneTask NewTsk)
        {
            ProjectorHub.Log?.Invoke($"Load task {NewTsk.TaskID}", "HUB");

            if (StreamAdd == true) this.LoadedObjects.Clear();

            if (this.LoadedObjects.GetTaskID(NewTsk.TaskID) is SceneTask oldTask
                && NewTsk.Command.Contains("RELOAD"))
            {
                oldTask.Remove();
                this.LoadedObjects.Add(NewTsk);
            }
            else if (NewTsk.TaskID < 0 || this.LoadedObjects.Contains(NewTsk.TaskID) == false)
            {
                this.LoadedObjects.Add(NewTsk);
            }
            ProjectorHub.Log?.Invoke($"Task command:{string.Join("&", NewTsk.Command)}", "HUB");
            if (GetSceneByID(NewTsk.TableID) is SceneModel Scene)
            {
                await Scene.ProjectScene.RunTask(NewTsk, NewTsk.Command.Contains("SHOW"));
            }
            else ProjectorHub.Log?.Invoke($"Not fount Table ID:{NewTsk.TableID}", "HUB");
        }

        protected async void SelectLoaded(SceneTask sceneTask)
        {
            await this.SelectedScene.ProjectScene.RunTask(sceneTask, true);
        }

        protected override void ClearItems()
        {
            for (int i = this.Count - 1; i > -1; i -= 1)
            {
                base[i].ProjectScene.Stop();
                base.Remove(this[i]);
            }
        }

        private void Scene_RemovedScene(object sender)
        {
            if (sender is SceneModel scene)
            {
                base.Remove(scene);
            }

        }

        private void Scene_SelectedScene(SceneModel sender, bool e)
        {
            try
            {
                if (e == true)
                {
                    foreach (SceneModel scene in this)
                    {
                        if (scene != sender) scene.IsSelected = false;
                    }

                    this.SelectedScene = sender;

                }
            }
            catch { }

        }

        #region INotifyPropertyChanged
        public event PropertyChangedEventHandler PropertyChanged;

        public virtual void OnPropertyChanged([CallerMemberName] string prop = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop));
        }
        #endregion
    }
}
