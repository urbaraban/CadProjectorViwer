using CadProjector.Devices.Modules;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace CadProjector.Devices
{
    public abstract class ProjectorBase : ILaserDevice, IConnectedDevice, INotifyPropertyChanged
    {
        public Guid Uid { get; } = Guid.NewGuid();
        public abstract string Name { get; }

        private bool isActive;
        public bool IsActive
        {
            get => isActive;
            protected set
            {
                if (isActive != value)
                {
                    isActive = value;
                    OnPropertyChanged();
                }
            }
        }

        private double width = 100;
        public double Width
        {
            get => width;
            set
            {
                if (width != value)
                {
                    width = value;
                    OnPropertyChanged();
                }
            }
        }

        private double height = 100;
        public double Height
        {
            get => height;
            set
            {
                if (height != value)
                {
                    height = value;
                    OnPropertyChanged();
                }
            }
        }

        private double powerPercent = 100;
        public double PowerPercent
        {
            get => powerPercent;
            set
            {
                if (powerPercent != value)
                {
                    powerPercent = Math.Max(0, Math.Min(100, value));
                    OnPropertyChanged();
                }
            }
        }



        public ObservableCollection<DeviceModule> Modules { get; } = new();

        public virtual string IpAddress { get; protected set; }
        public virtual int Port { get; protected set; }
        public abstract bool IsConnected { get; protected set; }

        public abstract Task<bool> Connect();
        public abstract Task Disconnect();

        public virtual async Task Reconnect()
        {
            await Disconnect();
            await Connect();
        }

        public abstract Task Start();
        public abstract Task Stop();

        protected void AddModule(DeviceModule module)
        {
            if (module != null && !Modules.Contains(module))
            {
                Modules.Add(module);
                module.ModuleUpdated += Module_Updated;
            }
        }

        protected void RemoveModule(DeviceModule module)
        {
            if (module != null && Modules.Contains(module))
            {
                module.ModuleUpdated -= Module_Updated;
                Modules.Remove(module);
            }
        }

        private void Module_Updated(object sender, DeviceModule module)
        {
            OnModuleUpdated(module);
        }

        protected virtual void OnModuleUpdated(DeviceModule module) { }

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}