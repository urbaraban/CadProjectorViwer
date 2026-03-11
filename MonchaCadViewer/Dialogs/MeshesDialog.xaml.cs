using CadProjectorSDK.CadObjects;
using CadProjectorSDK.Device;
using CadProjectorSDK.Device.Mesh;
using CadProjectorSDK.Device.Modules.Mesh;
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
        LProjector Device => (LProjector)this.DataContext;

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
            ProjectorMesh mesh = 
                new ProjectorMesh(
                    ProjectorMesh.MakeMeshPoint(5, 5, Device.GetSize),
                    $"Mesh_{Device.Meshes.Count}", RenderModuleTypes.MESH);

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

        private void CreateMorphModuleBtn_Click(object sender, RoutedEventArgs e)
        {
            var mesh = SelectMeshesList.SelectedItem as ProjectorMesh
                ?? ReadyMeshesList.SelectedItem as ProjectorMesh
                ?? Device.SelectedMesh;

            if (mesh == null)
            {
                MessageBox.Show("Select mesh first.");
                return;
            }

            var morph = new MorphMeshCorrector
            {
                StrokeCount = mesh.GetLength(0),
                ColumnCount = mesh.GetLength(1),
                Morph = mesh.Morph
            };

            var points = new CadAnchor[mesh.GetLength(0) * mesh.GetLength(1)];
            for (int y = 0; y < mesh.GetLength(0); y += 1)
            {
                for (int x = 0; x < mesh.GetLength(1); x += 1)
                {
                    var p = mesh[y, x];
                    points[y * mesh.GetLength(1) + x] = new CadAnchor(p.X, p.Y, p.Z);
                }
            }

            morph.PointsSerialized = points;
            Device.PreMeshModulesGroup.Modules.Add(morph);
        }
    }
}
