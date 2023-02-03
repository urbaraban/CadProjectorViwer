using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace CadProjectorViewer.ViewModel.Scenes.Commands
{
    public class CommandCollection : ObservableCollection<ISceneCommand>, INotifyPropertyChanged
    {
        public int CommandLimit
        {
            get => commandlimit;
            set
            {
                commandlimit = value;
                OnPropertyChanged("CommandLimit");
            }
        }
        private int commandlimit = 30;

        protected override void InsertItem(int index, ISceneCommand item)
        {
            int EndClearIndex = -1;
            for (int i = this.Count - 1; i > -1; i -= 1)
            {
                if (this[i].Status == true)
                {
                    EndClearIndex = i;
                    i = 0;
                }
            }
            for (int i = this.Count - 1; i > EndClearIndex; i -= 1)
            {
                this.RemoveAt(i);
            }

            if (this.Count > commandlimit) this.RemoveAt(0);

            if (item.Status == false) item.Run();

            base.InsertItem(this.Count, item);
        }

        protected override void RemoveItem(int index)
        {
            base.RemoveItem(index);
        }

        public void UndoCommand(ISceneCommand sceneCommand)
        {
            sceneCommand.Undo();
        }

        public void UndoLast()
        {
            for (int i = this.Count - 1; i >= 0; i -= 1)
            {
                if (this[i].Status == true)
                {
                    this[i].Undo();
                    return;
                }
            }
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
