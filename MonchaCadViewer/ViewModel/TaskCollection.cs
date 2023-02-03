using CadProjectorSDK.CadObjects.Abstract;
using CadProjectorSDK.Interfaces;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Windows.Input;


namespace CadProjectorSDK.Scenes
{
    public class TaskCollection : ObservableCollection<SceneTask>, INotifyPropertyChanged
    {
        public delegate void SelectedDelegate(SceneTask Scene);
        public SelectedDelegate Selected;

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

               Selected?.Invoke(sender);
        }

        public SceneTask GetTaskID(int taskID)
        {
            foreach(SceneTask sceneTask in this)
            {
                if (sceneTask.TaskID == taskID) return sceneTask;
            }
            return null;
        }
    }


    public class SceneTask : IRemoveObject
    {
        public SceneTask()
        {

        }

        public SceneTask(object Obj)
        {
            this.Object = Obj;
        }

        public SceneTask(SceneTask sceneTask)
        {
            this.Object = sceneTask.Object;
            this.TableID = sceneTask.TableID;
            this.TaskID = sceneTask.TaskID;
            this.TaskInfo = sceneTask.TaskInfo;
            this.Command = new List<string>(sceneTask.Command);
        }

        #region IRemoveObject
        public RemovingDelegate Removed { get; set; }
        public void Remove() => Removed?.Invoke(this);
        #endregion


        public void Selecting()
        {
            Select?.Invoke(this, true);
        }

        internal SceneTask Clone()
        {
            return new SceneTask(this);
        }

        public SelectingDelegate Select { get; set; }
        public delegate void SelectingDelegate(SceneTask sceneTask, bool stat);

        public FileInfo TaskInfo { get; set; } = new FileInfo("Task");

        public int TaskID { get; set; } = -1;
        /// <summary>
        /// Table ID for targeting place
        /// </summary>
        public int TableID { get; set; } = -1;

        /// <summary>
        /// Commands are supplied as a string and are separated by an &
        /// CLEAR — clear table
        /// SHOW — show UIDObject on table
        /// PLAY — turn on all device on table
        /// ALIGN — attach the object according to the settings
        /// RELOAD — reloads the object ignore TaskID
        /// </summary>
        public List<string> Command { get; set; } = new List<string>();
        public object Object { get; set; }
    }
}
