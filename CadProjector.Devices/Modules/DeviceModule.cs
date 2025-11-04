using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace CadProjector.Devices.Modules
{
    public abstract class DeviceModule : INotifyPropertyChanged
    {
        public virtual string Name => GetType().Name;

        public virtual string Description => string.Empty;

        public bool IsEnabled
        {
            get => isEnabled;
            set
            {
                if (isEnabled != value)
                {
                    isEnabled = value;
                    OnPropertyChanged();
                    OnEnabledChanged();
                }
            }
        }
        private bool isEnabled = true;

        protected virtual void OnEnabledChanged() { }

        public event EventHandler<DeviceModule> ModuleUpdated;

        protected void Update(DeviceModule module)
        {
            ModuleUpdated?.Invoke(this, module);
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}