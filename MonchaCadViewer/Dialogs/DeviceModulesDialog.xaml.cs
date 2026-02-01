using CadProjectorSDK.Device.Modules;
using CadProjectorViewer.Services;
using CadProjectorViewer.ViewModel.Devices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace CadProjectorViewer.Dialogs
{
    /// <summary>
    /// Логика взаимодействия для DeviceModulesDialog.xaml
    /// </summary>
    public partial class DeviceModulesDialog : Window
    {
        public DeviceModulesDialog()
        {
            InitializeComponent();
        }

        private void Label_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is Label label)
            {
                if (label.DataContext is DeviceModuleViewModel vm)
                {
                    DragDrop.DoDragDrop(label, new DraggingClass(vm.Module), DragDropEffects.Move);
                }
            }
        }

        private void StackPanel_Drop(object sender, DragEventArgs e)
        {
            if (sender is StackPanel stackPanel && 
                stackPanel.DataContext is DeviceModuleViewModel vm1 && 
                this.DataContext is AddDeviceModule model)
            {
                var dc = (DraggingClass)e.Data.GetData(typeof(DraggingClass));
                if (dc.Data is DeviceModule module2)
                {
                    int index = model.DeviceModules.IndexOf(vm1);
                    model.MoveItem(module2, index);
                }
            }
        }

        private void ListViewItem_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (sender is ListViewItem item && item.DataContext is DeviceModuleViewModel vm)
            {
                var panel = new ModulesEditor();
                panel.DataContext = vm.Module;
                var wnd = new Window()
                {
                    Content = panel,
                    SizeToContent = SizeToContent.WidthAndHeight
                };
                wnd.Show();
            }
        }
    }
}
