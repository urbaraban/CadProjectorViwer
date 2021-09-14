using MonchaCadViewer.CanvasObj;
using MonchaCadViewer.DeviceManager;
using MonchaSDK;
using MonchaSDK.Device;
using MonchaSDK.Object;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using AppSt = MonchaCadViewer.Properties.Settings;

namespace MonchaCadViewer.Panels
{
    /// <summary>
    /// Логика взаимодействия для DeviceTreeTab.xaml
    /// </summary>
    public partial class DeviceTreeTab : UserControl, INotifyPropertyChanged
    {
        public event EventHandler<bool> NeedRefresh;
        public event EventHandler<LDevice> DeviceChange;


        MainWindow mainWindow => (MainWindow)this.DataContext;

        public DeviceTreeTab()
        {
            InitializeComponent();
        }


        private void TreeBaseMesh(object sender, MouseButtonEventArgs e)
        {
            if (sender is TreeViewItem BaseMeshItem && BaseMeshItem.Parent is TreeViewItem DeviceTree)
            {
                if (DeviceTree.DataContext is LDevice device && BaseMeshItem.DataContext is LDeviceMesh mesh)
                {
                    mainWindow.MainScene.AddRange(CadCanvas.GetMesh(mesh, LaserHub.GetThinkess * AppSt.Default.anchor_size, false, MeshType.NONE).ToArray());
                }
            }
        }


        private void LaserMeters_ContextMenuClosing(object sender, ContextMenuEventArgs e)
        {
            if (sender is TreeViewItem viewItem)
            {
                if (viewItem.ContextMenu.DataContext is MenuItem cmindex && sender is TreeViewItem treeView)

                    switch (cmindex.Tag)
                    {
                        case "common_ADD":
                            MonchaCadViewer.DeviceManager.LaserMeterWindows laserMeterWindows = new MonchaCadViewer.DeviceManager.LaserMeterWindows(new VLTLaserMeters());
                            laserMeterWindows.ShowDialog();
                            break;
                    }
            }
        }


        private void RemoveLaser_Click(object sender, RoutedEventArgs e)
        {
            mainWindow.LaserHub.Devices.Remove(selectdevice);
        }


        private void RefreshLaser_Click_1(object sender, RoutedEventArgs e)
        {
            mainWindow.LaserHub.Play = false;
            mainWindow.LaserHub.Load(AppSt.Default.cl_moncha_path);
        }

        private void AddLaser_Click(object sender, RoutedEventArgs e)
        {
            LaserSearcher deviceManager = new LaserSearcher(mainWindow.LaserHub);
            deviceManager.Show();
        }


        private void MenuItem_Click(object sender, RoutedEventArgs e)
        {
            DependencyObject obj = sender as DependencyObject;

            while (obj != null && !(obj is ContextMenu))
            {
                obj = VisualTreeHelper.GetParent(obj);
            }
            (obj as ContextMenu).DataContext = sender;
        }

        private void DeviceBorder_ContextMenuClosing(object sender, ContextMenuEventArgs e)
        {
            if (sender is FrameworkElement element)
            {
                if (element.ContextMenu.DataContext is MenuItem cmindex &&
                            element.DataContext is LDevice device)
                {
                    switch (cmindex.Tag)
                    {
                        case "dvc_showrect":
                            //device.Frame = device.CutZone.DrawCutZone();
                            break;
                        case "dvc_showzone":
                            CadRectangle cadRectangle = new CadRectangle(device.Size, device.HWIdentifier);
                            mainWindow.MainScene.Add(cadRectangle);
                            cadRectangle.Init();
                            break;
                        case "dvc_polymesh":
                            device.PolyMeshUsed = !device.PolyMeshUsed;
                            break;
                        case "dvc_center":
                            mainWindow.MainScene.Add(new CadAnchor(device.Size.Center) { Render = false });
                            break;
                        case "dvc_view":
                            ProjectorView projectorView = new ProjectorView() { DataContext = device };
                            projectorView.Show();
                            break;

                    }
                }
            }
        }

        private void MeshBorder_ContextMenuClosing(object sender, ContextMenuEventArgs e)
        {
            if (sender is FrameworkElement element)
            {
                if (element.ContextMenu.DataContext is MenuItem cmindex)
                {
                    if (element.DataContext is LDeviceMesh mesh)
                    {
                        switch (cmindex.Tag)
                        {
                            case "mesh_create":
                                CreateGridWindow createGridWindow = new CreateGridWindow() { DataContext = mesh };
                                createGridWindow.ShowDialog();
                                break;
                            case "mesh_showvirtual":
                                mainWindow.MainScene.Clear();
                                mainWindow.MainScene.AddRange(CadCanvas.GetMesh(mesh.VirtualMesh, LaserHub.GetThinkess * AppSt.Default.anchor_size, false, MeshType.BASE).ToArray());
                                break;
                            case "mesh_inverse":
                                mesh.InverseYPosition();
                                break;
                            case "mesh_returnpoint":
                                mesh.ReturnPoint();
                                break;
                            case "mesh_morph":
                                mesh.Morph = !mesh.Morph;
                                break;
                            case "mesh_affine":
                                mesh.Affine = !mesh.Affine;
                                break;
                            case "mesh_showrect":
                                CadRectangle rectangle = new CadRectangle(mesh.Size, mesh.Name);
                                mainWindow.MainScene.Add(rectangle);
                                rectangle.Init();
                                break;
                        }
                    }
                }
            }
        }

        #region INotifyPropertyChanged
        public event PropertyChangedEventHandler PropertyChanged;

        public void OnPropertyChanged([CallerMemberName] string prop = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop));
        }
        #endregion

        private void MeshBorder_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount == 2)
            {
                if (sender is FrameworkElement frameworkElement)
                {
                    if (frameworkElement.DataContext is LDeviceMesh mesh)
                    {
                        mainWindow.MainScene.Clear();
                        mainWindow.MainScene.AddRange(CadCanvas.GetMesh(mesh, LaserHub.GetThinkess * AppSt.Default.anchor_size, false, MeshType.NONE).ToArray());
                    }
                }
            }
        }

        private LDevice selectdevice;
        private void treeView_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (e.NewValue is LDevice device)
            {
                selectdevice = device;
            }
        }
    }
}
