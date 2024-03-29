﻿using CadProjectorSDK.Device.Modules;
using CadProjectorSDK.Device.Modules.Transforming;
using Microsoft.Xaml.Behaviors.Core;
using System;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace CadProjectorViewer.ViewModel.Devices
{
    internal class AddDeviceModule : NotifyModel
    {
        public Type[] AvailableType { get; } = new Type[]
        {
            typeof(ZCorrector),
            typeof(RectProportion),
            typeof(ModulesGroup)
        };

        public ObservableCollection<DeviceModule> DeviceModules => new ObservableCollection<DeviceModule>(this.MGroup.Modules);

        public DeviceModule SelectModule { get; set; }

        private ModulesGroup MGroup { get; }

        public AddDeviceModule(ModulesGroup device)
        {
            this.MGroup = device;
        }

        public void AddModule(Type type)
        {
            var module = (DeviceModule)Activator.CreateInstance(type);
            this.MGroup.Modules.Add(module);
            OnPropertyChanged(nameof(DeviceModules));
        }

        public void RemoveModule(DeviceModule module)
        {
            this.MGroup.Modules.Remove(module);
            OnPropertyChanged(nameof(DeviceModules));
        }

        public void MoveItem(DeviceModule module, int newindex)
        {
            this.RemoveModule(module);
            this.MGroup.Modules.Insert(newindex, module);
            OnPropertyChanged(nameof(DeviceModules));
        }

        public ICommand AddModuleCommand => new ActionCommand((object obj) => AddModule((Type)obj));

        public ICommand RemoveModuleCommand => new ActionCommand(() =>
        {
            if (this.SelectModule != null)
            {
                this.RemoveModule(this.SelectModule);
            }
        });
    }
}
