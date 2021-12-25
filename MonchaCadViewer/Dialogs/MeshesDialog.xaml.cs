using CadProjectorSDK.Device;
using CadProjectorSDK.Device.Mesh;
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

namespace CadProjectorViewer.Panels.DevicePanel
{
    /// <summary>
    /// Логика взаимодействия для MeshesDialog.xaml
    /// </summary>
    public partial class MeshesDialog : Window
    {
        LDevice Device => (LDevice)this.DataContext;

        public MeshesDialog()
        {
            InitializeComponent();
        }

        private void AddBtn_Click(object sender, RoutedEventArgs e)
        {
            Device.AddSelectMesh((ProjectorMesh)ReadyMeshesList.SelectedItem);
        }

        private void RemoveBtn_Click(object sender, RoutedEventArgs e)
        {
            Device.RemoveSelectMesh((ProjectorMesh)SelectMeshesList.SelectedItem);
        }

        private void ReadyPlusBtn_Click(object sender, RoutedEventArgs e)
        {
            ProjectorMesh mesh = new ProjectorMesh(ProjectorMesh.MakeMeshPoint(5, 5, Device.Size), $"Mesh_{Device.Meshes.Count}", MeshType.NONE);
            Device.Meshes.Add(mesh);
            CreateGridWindow createGridWindow = new CreateGridWindow(mesh);
            createGridWindow.Show();
        }

        private void ReadyMinusBtn_Click(object sender, RoutedEventArgs e)
        {
            ProjectorMesh mesh = (ProjectorMesh)ReadyMeshesList.SelectedItem;
            Device.RemoveSelectMesh(mesh);
            Device.Meshes.Remove(mesh);
        }
    }
}
