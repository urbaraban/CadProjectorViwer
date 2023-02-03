using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace CadProjectorViewer.ViewModel.Scenes.Commands
{
    public abstract class SceneCommand : INotifyPropertyChanged
    {
        public bool Status
        {
            get => _status;
            set
            {
                _status = value;
                OnPropertyChanged("Status");
            }
        }
        private bool _status = false;

        #region INotifyPropertyChanged
        public event PropertyChangedEventHandler PropertyChanged;

        public virtual void OnPropertyChanged([CallerMemberName] string prop = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop));
        }
        #endregion
    }
}
