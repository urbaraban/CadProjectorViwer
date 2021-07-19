using MonchaSDK.Device;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace MonchaCadViewer.Panels.DevicePanel
{
    /// <summary>
    /// Логика взаимодействия для MeshesDialog.xaml
    /// </summary>
    public partial class MeshesDialog : Window
    {

        MonchaDevice Device;

        public MeshesDialog(MonchaDevice device)
        {
            InitializeComponent();

            Device = device;

            SelectMeshesList.SelectedValuePath = "Name";
            SelectMeshesList.DisplayMemberPath = "Name";
            SelectMeshesList.ItemsSource = device.SelectedMeshes;

            ReadyMeshesList.SelectedValuePath = "Name";
            ReadyMeshesList.DisplayMemberPath = "Name";
            ReadyMeshesList.ItemsSource = device.Meshes;
            
        }

        private void AddBtn_Click(object sender, RoutedEventArgs e)
        {
            Device.AddSelectMesh((LDeviceMesh)ReadyMeshesList.SelectedItem);
        }

        private void RemoveBtn_Click(object sender, RoutedEventArgs e)
        {
            Device.RemoveSelectMesh((LDeviceMesh)SelectMeshesList.SelectedItem);
        }

        private void ReadyPlusBtn_Click(object sender, RoutedEventArgs e)
        {
            LDeviceMesh mesh = new LDeviceMesh(LDeviceMesh.MakeMeshPoint(5, 5), $"Mesh_{Device.Meshes.Count}");
            Device.Meshes.Add(mesh);
            CreateGridWindow createGridWindow = new CreateGridWindow(Device, mesh);
            createGridWindow.Show();
        }

        private void ReadyMinusBtn_Click(object sender, RoutedEventArgs e)
        {
            LDeviceMesh mesh = (LDeviceMesh)SelectMeshesList.SelectedItem;
            Device.Meshes.Remove(mesh);
            Device.RemoveSelectMesh(mesh);
        }
    }
}
