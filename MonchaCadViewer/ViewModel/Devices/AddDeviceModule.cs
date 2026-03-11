using CadProjectorSDK.Device;
using CadProjectorSDK.Device.Modules;
using CadProjectorSDK.CadObjects;
using Microsoft.Xaml.Behaviors.Core;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Windows.Controls;
using System.Windows.Input;
using static Microsoft.WindowsAPICodePack.Shell.PropertySystem.SystemProperties.System;

namespace CadProjectorViewer.ViewModel.Devices
{
    public enum DeviceModulesStage
    {
        FinalRender,
        PreMesh
    }

    internal class DeviceModuleViewModel : NotifyModel
    {
        private readonly LProjector _projector;
        private readonly DeviceModulesStage _stage;

        public DeviceModule Module { get; }
        public ModulesGroup OwnerGroup { get; }

        public DeviceModuleViewModel(LProjector projector, ModulesGroup ownerGroup, DeviceModule module, DeviceModulesStage stage)
        {
            _projector = projector;
            OwnerGroup = ownerGroup;
            Module = module;
            _stage = stage;
        }

        public string Name => Module.Name;
        public string GroupDisplayName => _stage == DeviceModulesStage.PreMesh ? "Модули до сетки" : "Финальные модули";

        public bool CanShow => Module is RenderableDeviceModule;

        public bool IsOn
        {
            get => Module.IsOn;
            set
            {
                Module.IsOn = value;
                OnPropertyChanged(nameof(IsOn));
            }
        }

        public bool IsShown
        {
            get
            {
                var scene = _projector.GetParentScene?.Invoke();
                if (scene == null)
                    return false;

                return scene.OfType<CadDeviceModule>().Any(x => ReferenceEquals(x.Module, Module));
            }
        }

        public ICommand ShowCommand => new ActionCommand(() =>
        {
            if (!CanShow)
                return;

            var scene = _projector.GetParentScene?.Invoke();
            if (scene == null)
                return;

            var wrapper = scene.OfType<CadDeviceModule>()
             .FirstOrDefault(x => ReferenceEquals(x.Module, Module) && x.IsRender);

            if (wrapper != null)
            {
                wrapper.Remove();
            }
            else
            {
                scene.Add(new CadDeviceModule(Module)
                {
                    DeviceMultiply = GetProjectorLocalSize
                });
            }

            scene.RefreshScene();
            OnPropertyChanged(nameof(IsShown));
        });

        private CadRect3D GetProjectorLocalSize()
        {
            var size = _projector?.Size;
            if (size == null)
                return null;

            return new CadRect3D(
                new CadAnchor(size.BL.X, size.BL.Y, size.BL.Z),
                new CadAnchor(size.TR.X, size.TR.Y, size.TR.Z));
        }

        public void HideFromSceneIfShown()
        {
            var scene = _projector.GetParentScene?.Invoke();
            if (scene == null)
                return;

            var wrapper = scene.OfType<CadDeviceModule>().FirstOrDefault(x => ReferenceEquals(x.Module, Module));
            if (wrapper != null)
            {
                wrapper.Remove();
                scene.RefreshScene();
            }
        }
    }

    internal class AddDeviceModule : NotifyModel
    {
        public IEnumerable<Type> AvailableType { get; }

        public ObservableCollection<DeviceModuleViewModel> DeviceModules { get; } = new ObservableCollection<DeviceModuleViewModel>();

        public DeviceModuleViewModel SelectModule
        {
            get => _selectModule;
            set
            {
                if (ReferenceEquals(_selectModule, value))
                {
                    return;
                }

                _selectModule = value;
                OnPropertyChanged(nameof(SelectModule));
            }
        }
        private DeviceModuleViewModel _selectModule;

        public string DialogTitle => $"Модули устройства: {Projector?.DisplayName}";

        public bool IsOn
        {
            get => MGroup.IsOn;
            set
            {
                MGroup.IsOn = value;
            }
        }

        public DeviceModulesStage SelectedStage
        {
            get => _selectedStage;
            set
            {
                if (_selectedStage == value)
                {
                    return;
                }

                _selectedStage = value;
                OnPropertyChanged(nameof(SelectedStage));
                OnPropertyChanged(nameof(IsOn));
                SyncModules();
            }
        }
        private DeviceModulesStage _selectedStage = DeviceModulesStage.FinalRender;

        private ModulesGroup MGroup => SelectedStage == DeviceModulesStage.PreMesh
            ? Projector.PreMeshModulesGroup
            : Projector.ModulesGroup;

        private LProjector Projector { get; }

        public AddDeviceModule(LProjector device)
        {
            Projector = device;
            this.AvailableType = typeof(DeviceModule).Assembly.GetTypes()
                .Where(x => typeof(DeviceModule).IsAssignableFrom(x))
                .Where(x => x != typeof(DeviceModule) && !x.IsAbstract)
                .Where(x => x.GetConstructor(Type.EmptyTypes) != null)
                .OrderBy(x => x.Name);

            SyncModules();
            this.Projector.ModulesGroup.Modules.CollectionChanged += Modules_CollectionChanged;
            this.Projector.PreMeshModulesGroup.Modules.CollectionChanged += Modules_CollectionChanged;
        }

        private void Modules_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            SyncModules();
        }

        private void SyncModules()
        {
            DeviceModules.Clear();
            foreach (var module in this.MGroup.Modules)
            {
                DeviceModulesStage stage = ReferenceEquals(MGroup, Projector.PreMeshModulesGroup)
                    ? DeviceModulesStage.PreMesh
                    : DeviceModulesStage.FinalRender;
                DeviceModules.Add(new DeviceModuleViewModel(Projector, MGroup, module, stage));
            }
            if (SelectModule == null && DeviceModules.Count > 0)
            {
                SelectModule = DeviceModules[0];
            }
            else if (SelectModule != null && DeviceModules.Contains(SelectModule) == false)
            {
                SelectModule = DeviceModules.FirstOrDefault();
            }
            OnPropertyChanged(nameof(DeviceModules));
        }

        public void AddModule(Type type)
        {
            var module = (DeviceModule)Activator.CreateInstance(type);
            this.MGroup.Modules.Add(module);
            OnPropertyChanged(nameof(DeviceModules));
        }

        public void RemoveModule(DeviceModule module)
        {
            DeviceModules.FirstOrDefault(x => ReferenceEquals(x.Module, module))?.HideFromSceneIfShown();

            this.MGroup.Modules.Remove(module);
            OnPropertyChanged(nameof(DeviceModules));
        }

        public void MoveItem(DeviceModule module, int newindex)
        {
            this.MGroup.Modules.Remove(module);
            this.MGroup.Modules.Insert(newindex, module);
            OnPropertyChanged(nameof(DeviceModules));
        }

        public ICommand AddModuleCommand => new ActionCommand((object obj) => AddModule((Type)obj));

        public ICommand RemoveModuleCommand => new ActionCommand(() =>
        {
            if (this.SelectModule != null)
            {
                this.RemoveModule(this.SelectModule.Module);
            }
        });

        protected void HandleDoubleClick(object sender, MouseButtonEventArgs e)
        {
            var track = ((ListViewItem)sender).Content as DeviceModule; //Casting back to the binded Track
        }
    }
}
