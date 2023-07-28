using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace CadProjectorViewer.ViewModel
{
    internal abstract class NotifyModel : INotifyPropertyChanged
    {
        #region INotifyPropertyChanged
        public event PropertyChangedEventHandler PropertyChanged;

        public void OnPropertyChanged([CallerMemberName] string prop = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop));
        }
        #endregion
    }
}
