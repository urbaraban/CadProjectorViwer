using CadProjectorSDK.Device;
using CadProjectorSDK.Interfaces;
using CadProjectorSDK.Scenes;
using CadProjectorViewer.DeviceManaged;
using CadProjectorViewer.Dialogs;
using CadProjectorViewer.ViewModel;
using CadProjectorViewer.ViewModel.Devices;
using Microsoft.Xaml.Behaviors.Core;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace CadProjectorViewer.Panels
{
    /// <summary>
    /// Логика взаимодействия для DeviceItem.xaml
    /// </summary>
    public partial class DeviceItem : UserControl
    {
        public DeviceItem()
        {
            InitializeComponent();
        }
    }
}
