using CadProjectorSDK.CadObjects.Abstract;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace CadProjectorViewer.ViewModel.Scenes
{
    public class SelectedObjectCollection : ObservableCollection<UidObject>, INotifyPropertyChanged
    {
        public bool OneSelectMode
        {
            get => _oneselectmode;
            set
            {
                _oneselectmode = value;
                OnPropertyChanged("OneselectMode");
            }
        }
        private bool _oneselectmode = true;

        public UidObject LastSelectObject
        {
            get => lastselectobject;
            set
            {
                lastselectobject = value;
                OnPropertyChanged("LastSelectObject");
            }
        }
        private UidObject lastselectobject;

        private void Obj_Selected(UidObject sender, bool e)
        {
            if (e == false)
            {
                this.Remove(sender);
            }
        }

        private void Obj_Remove(object sender)
        {
            if (sender is UidObject uidObject)
            {
                this.Remove(uidObject);
            }
        }

        public new void Add(UidObject uidObject)
        {
            base.Add(uidObject);
        }

        protected override void InsertItem(int index, UidObject item)
        {
            if (base.Contains(item) == false)
            {
                if (OneSelectMode == true)
                {
                    this.Clear();
                    index = 0;
                }

                item.IsSelected = true;
                base.InsertItem(index, item);
                LastSelectObject = item;
            }
        }

        protected override void RemoveItem(int index)
        {
            this[index].IsSelected = false;
            if (this[index] == lastselectobject) LastSelectObject = null;
            base.RemoveItem(index);
        }

        public new void Clear()
        {
            for (int i = this.Count - 1; i > -1; i -= 1)
            {
                this.Remove(this[i]);
            }
        }

        public new void Remove(UidObject uidObject)
        {
            base.Remove(this.Where(i => i.Uid == uidObject.Uid).FirstOrDefault());
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
