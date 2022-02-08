using CadProjectorViewer.CanvasObj;
using CadProjectorViewer.DeviceManaged;
using CadProjectorSDK;
using CadProjectorSDK.Device;
using CadProjectorSDK.CadObjects;
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
using AppSt = CadProjectorViewer.Properties.Settings;
using CadProjectorSDK.Device.Mesh;
using CadProjectorSDK.Device.Controllers;
using CadProjectorSDK.Interfaces;

namespace CadProjectorViewer.Panels
{
    /// <summary>
    /// Логика взаимодействия для DeviceTreeTab.xaml
    /// </summary>
    public partial class DeviceTreeTab : UserControl, INotifyPropertyChanged
    {
        public event EventHandler<bool> NeedRefresh;
        public event EventHandler<LDevice> DeviceChange;


        ProjectorHub projectorHub => (ProjectorHub)this.DataContext;

        public DeviceTreeTab()
        {
            InitializeComponent();
        }


        private void TreeBaseMesh(object sender, MouseButtonEventArgs e)
        {
            if (sender is TreeViewItem BaseMeshItem && BaseMeshItem.Parent is TreeViewItem DeviceTree)
            {
                if (DeviceTree.DataContext is LDevice device && BaseMeshItem.DataContext is ProjectorMesh mesh)
                {
                    //projectorHub.Scene.AddRange(CadCanvas.GetMesh(mesh, ProjectorHub.GetThinkess * AppSt.Default.anchor_size, false, MeshType.NONE).ToArray());
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
                            CadProjectorViewer.DeviceManaged.LaserMeterWindows laserMeterWindows = new CadProjectorViewer.DeviceManaged.LaserMeterWindows(new VLTLaserMeters());
                            laserMeterWindows.ShowDialog();
                            break;
                    }
            }
        }


        private void RemoveLaser_Click(object sender, RoutedEventArgs e)
        {
            if (selectdevice != null)
            {
                selectdevice.Remove();
            }
        }


        private void RefreshLaser_Click_1(object sender, RoutedEventArgs e)
        {
            projectorHub.Load(AppSt.Default.cl_moncha_path);
        }

        private void AddLaser_Click(object sender, RoutedEventArgs e)
        {
            LaserSearcher DeviceManaged = new LaserSearcher(projectorHub);
            DeviceManaged.Show();
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
                            projectorHub.ScenesCollection.SelectedScene.Add(device.Size);
                            break;
                        case "dvc_polymesh":
                            device.PolyMeshUsed = !device.PolyMeshUsed;
                            break;
                        case "dvc_center":
                            projectorHub.ScenesCollection.SelectedScene.Add(new CadAnchor(device.Size.Center) { IsRender = false });
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
                    if (element.DataContext is ProjectorMesh mesh)
                    {
                        switch (cmindex.Tag)
                        {
                            case "mesh_create":
                                CreateGridWindow createGridWindow = new CreateGridWindow(mesh);
                                createGridWindow.ShowDialog();
                                break;
                            case "convert_on_ellipsoid":
                                mesh.ConvertOnEllipsoid();
                                break;
                            case "mesh_inverse":
                                mesh.InverseYPosition();
                                break;
                            case "mesh_returnpoint":
                                mesh.ReturnPoint();
                                break;
                            case "mesh_showrect":
                                projectorHub.ScenesCollection.SelectedScene.Add(mesh.Size);
                                break;
                        }
                        element.ContextMenu.DataContext = null;
                    }
                }

            }
        }

        private void MeshBorder_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount == 2)
            {
                if (sender is FrameworkElement frameworkElement)
                {
                    if (frameworkElement.DataContext is ProjectorMesh mesh)
                    {
                        if (Keyboard.Modifiers != ModifierKeys.Shift) projectorHub.ScenesCollection.SelectedScene.Clear();
                        projectorHub.ScenesCollection.SelectedScene.Add(mesh);
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

        #region INotifyPropertyChanged
        public event PropertyChangedEventHandler PropertyChanged;

        public void OnPropertyChanged([CallerMemberName] string prop = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop));
        }
        #endregion

        private async void ReconnectButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.DataContext is IConnected connected)
            {
                await connected.Reconnect();
            }
        }
    }
}
