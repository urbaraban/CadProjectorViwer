using CadProjectorSDK.CadObjects.Abstract;
using CadProjectorSDK.Interfaces;
using CadProjectorSDK.Scenes;
using CadProjectorViewer.Services;
using CadProjectorViewer.ViewModel.Modules;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Input;
using static CadProjectorSDK.Interfaces.IRemoveObject;

namespace CadProjectorViewer.Modeles
{
    public class TaskCollection : ObservableCollection<SceneTask>, INotifyPropertyChanged
    {
        public static TaskCollection Instance = new TaskCollection();

        public event EventHandler<SceneTask> SelectedTask;

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

        private TaskCollection() { }

        public async Task AddTask(SceneTask NewTsk)
        {
            LogList.Instance.PostLog($"Load task {NewTsk.TaskID}", "Tasks");

            if (StreamAdd == true) this.Clear();

            if (this.GetTaskID(NewTsk.TaskID) is SceneTask oldTask
                && NewTsk.Command.Contains("RELOAD"))
            {
                oldTask.Remove();
                this.Add(NewTsk);
            }
            else if (NewTsk.TaskID < 0 || this.Contains(NewTsk.TaskID) == false)
            {
                this.Add(NewTsk);
            }

            LogList.Instance.PostLog($"Task command:{string.Join("&", NewTsk.Command)}", "Tasks");

            if (NewTsk.Command.Contains("SHOW") == true)
            {
                SelectedTask?.Invoke(this, NewTsk);
            }
            LogList.Instance.PostLog($"End load task:{NewTsk.TaskInfo}", "Tasks");
        }

        public bool Contains(int TaskID)
        {
            foreach (SceneTask sceneTask in this)
            {
                if (sceneTask.TaskID == TaskID) return true;
            }
            return false;
        }

        public bool Contains(UidObject uidObject)
        {
            foreach (SceneTask sceneTask in this)
            {
                if (sceneTask.Object == uidObject) return true;
            }
            return false;
        }

        protected override void RemoveItem(int index)
        {
            this[index].Select -= Scene_SelectedScene;
            this[index].Removed -= Scene_RemovedScene;
            base.RemoveItem(index);
        }

        protected override void InsertItem(int index, SceneTask item)
        {
            if (item.Object is UidObject uidObject)
            {
                uidObject.UpdateTransform(uidObject.Bounds, false, "Left%Top");
            }
            base.InsertItem(index, item);
            item.Select += Scene_SelectedScene;
            item.Removed += Scene_RemovedScene;

            item.Selecting();
        }

        protected override void ClearItems()
        {
            for (int i = this.Count - 1; i >-1; i -= 1)
            {
                this[i].Remove();
            }
        }

        private void Scene_RemovedScene(object sender)
        {
            if (sender is SceneTask scene)
            {
                base.Remove(scene);
            }
            
        }

        private void Scene_SelectedScene(SceneTask sender, bool e)
        {
            var send = sender.Clone();

            if (sender.Command.Count == 0)
            {
                sender.Command.Add("SHOW");
                sender.Command.Add("ALIGN");
                sender.Command.Add("PLAY");
                sender.Command.Add("CLEAR");
            }

            if (Keyboard.Modifiers == ModifierKeys.Shift && sender.Command.Contains("CLEAR") == true)
                sender.Command.RemoveAt(sender.Command.IndexOf("CLEAR"));

            if (send.Object is UidObject uidObject && uidObject.FileInfo == null)
            {
                uidObject.FileInfo = sender.TaskInfo;
            }

               SelectedTask?.Invoke(this, sender);
        }

        internal SceneTask GetTaskID(int taskID)
        {
            foreach(SceneTask sceneTask in this)
            {
                if (sceneTask.TaskID == taskID) return sceneTask;
            }
            return null;
        }

        #region INotifyPropertyChanged
        public event PropertyChangedEventHandler PropertyChanged;

        public void OnPropertyChanged([CallerMemberName] string prop = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop));
        }
        #endregion
    }
}
