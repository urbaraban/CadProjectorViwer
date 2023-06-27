using CadProjectorSDK.Scenes;
using CadProjectorViewer.ViewModel.Modules;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace CadProjectorViewer.Modeles
{
    public class ScenesCollection : ObservableCollection<ProjectionScene>, INotifyPropertyChanged
    {

        public ProjectionScene SelectedScene 
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
        private ProjectionScene _selectedscene = new ProjectionScene();

        public ScenesCollection()
        {

        }

        public ProjectionScene GetSceneID(int ID)
        {
            foreach (ProjectionScene scene in this)
            {
                if (scene.TableID == ID) return scene;
            }
            return this.SelectedScene;
        }

        protected override void RemoveItem(int index)
        {
            this[index].SelectedScene -= Scene_SelectedScene;
            this[index].Removed -= Scene_RemovedScene;
            base.RemoveItem(index);
        }

        protected override void InsertItem(int index, ProjectionScene item)
        {
            if (this.Contains(item) == true) item.IsSelected = true;
            else
            {
                base.InsertItem(index, item);
                item.SelectedScene += Scene_SelectedScene;
                item.Removed += Scene_RemovedScene;
                item.IsSelected = true;
                if (SelectedScene == null) SelectedScene = item;
                if (item.TableID == 0 && this.Count > 0) item.TableID = this.Max(x => x.TableID) + 1;
            }
        }


        protected async void SelectLoaded(SceneTask sceneTask)
        {
            await SelectedScene.RunTask(sceneTask, true);
        }

        protected override void ClearItems()
        {
            for (int i = this.Count - 1; i > -1; i -= 1)
            {
                base[i].Stop();
                base.Remove(this[i]);
            }
        }

        private void Scene_RemovedScene(object sender)
        {
            if (sender is ProjectionScene scene)
            {
                base.Remove(scene);
            }
            
        }

        private void Scene_SelectedScene(ProjectionScene sender, bool e)
        {
            try
            {
                if (e == true)
                {
                    foreach (ProjectionScene scene in this)
                    {
                        if (scene != sender) scene.IsSelected = false;
                    }

                    this.SelectedScene = sender;

                }
            }
            catch {  }

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
