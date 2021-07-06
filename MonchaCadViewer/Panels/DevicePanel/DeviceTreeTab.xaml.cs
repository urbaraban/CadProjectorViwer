using MonchaCadViewer.CanvasObj;
using MonchaSDK;
using MonchaSDK.Device;
using MonchaSDK.Object;
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
using System.Windows.Navigation;
using AppSt = MonchaCadViewer.Properties.Settings;

namespace MonchaCadViewer.Panels
{
    /// <summary>
    /// Логика взаимодействия для DeviceTreeTab.xaml
    /// </summary>
    public partial class DeviceTreeTab : UserControl
    {
        public event EventHandler<bool> NeedRefresh;
        public event EventHandler<MonchaDevice> DeviceChange;
        public event EventHandler<List<FrameworkElement>> DrawObjects;

        private MonchaDevice selectdevice;

        public DeviceTreeTab()
        {
            InitializeComponent();
            Refresh();
        }

        public void Refresh()
        {
            DeviceTree.Items.Clear();

            TreeViewItem LaserScanners = new TreeViewItem();
            LaserScanners.Header = "LaserScanners";

            DeviceTree.Items.Add(LaserScanners);

            foreach (MonchaDevice device in MonchaHub.Devices)
            {
                if (device != null)
                {
                    Brush brushes = device.Virtual == true ? Brushes.DarkGray : Brushes.Black;

                    TreeViewItem treeViewDevice = new TreeViewItem();
                    treeViewDevice.Header = device.HWIdentifier;
                    treeViewDevice.Foreground = brushes;
                    treeViewDevice.FontStyle = device.Virtual == true ? FontStyles.Italic : FontStyles.Normal;
                    treeViewDevice.DataContext = device;
                    treeViewDevice.ContextMenu = new ContextMenu();
                    ContextMenuLib.DeviceTreeMenu(treeViewDevice.ContextMenu);
                    treeViewDevice.MouseLeftButtonUp += LoadDeviceSetting;
                    treeViewDevice.ContextMenuClosing += TreeViewDevice_ContextMenuClosing;

                    if (device.PolyMeshUsed)
                    {
                        TreeViewItem treeViewBaseMesh = new TreeViewItem();
                        treeViewBaseMesh.Header = "BaseMesh";
                        treeViewBaseMesh.DataContext = device.BaseMesh;
                        treeViewBaseMesh.MouseDoubleClick += TreeBaseMesh;

                        if (treeViewBaseMesh.ContextMenu == null) treeViewBaseMesh.ContextMenu = new System.Windows.Controls.ContextMenu();
                        ContextMenuLib.MeshMenu(treeViewBaseMesh.ContextMenu);
                        treeViewBaseMesh.ContextMenuClosing += TreeViewBaseMesh_ContextMenuClosing;

                        treeViewDevice.Items.Add(treeViewBaseMesh);
                    }

                    TreeViewItem treeViewCalculationMesh = new TreeViewItem();
                    treeViewCalculationMesh.Header = "SelectMesh";
                    treeViewCalculationMesh.FontStyle = device.Virtual == true ? FontStyles.Italic : FontStyles.Normal;
                    treeViewCalculationMesh.Foreground = brushes;
                    treeViewCalculationMesh.DataContext = device;
                    treeViewCalculationMesh.MouseDoubleClick += TreeViewCalculationMesh_MouseDoubleClick; ;

                    if (treeViewCalculationMesh.ContextMenu == null) treeViewCalculationMesh.ContextMenu = new System.Windows.Controls.ContextMenu();
                    ContextMenuLib.MeshMenu(treeViewCalculationMesh.ContextMenu);
                    treeViewCalculationMesh.ContextMenuClosing += TreeViewBaseMesh_ContextMenuClosing;

                    treeViewDevice.Items.Add(treeViewCalculationMesh);

                    LaserScanners.Items.Add(treeViewDevice);
                    LaserScanners.ExpandSubtree();
                }
            }

            TreeViewItem LaserMeters = new TreeViewItem();
            LaserMeters.Header = "LaserMeters";
            LaserMeters.ContextMenu = new ContextMenu();
            ContextMenuLib.LaserMeterHeadTreeMenu(LaserMeters.ContextMenu);
            LaserMeters.ContextMenuClosing += LaserMeters_ContextMenuClosing;
            DeviceTree.Items.Add(LaserMeters);


            if (MonchaHub.LMeters.Count > 0)
            {
                foreach (VLTLaserMeters device in MonchaHub.LMeters)
                {
                    TreeViewItem treeLaserMeterDevice = new TreeViewItem();
                    treeLaserMeterDevice.Header = "LaserMeter " + device.IP;
                    treeLaserMeterDevice.DataContext = device;
                    treeLaserMeterDevice.ContextMenu = new ContextMenu();
                    treeLaserMeterDevice.MouseDoubleClick += TreeLaserMeterDevice_MouseDoubleClick;
                    ContextMenuLib.LaserMeterDeviceTreeMenu(treeLaserMeterDevice.ContextMenu);
                    LaserMeters.Items.Add(treeLaserMeterDevice);
                }
            }
        }

        private void TreeViewCalculationMesh_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (sender is TreeViewItem BaseMeshItem && BaseMeshItem.Parent is TreeViewItem DeviceTree)
            {
                if (DeviceTree.DataContext is MonchaDevice device)
                {
                    DrawObjects?.Invoke(this, CadCanvas.GetMesh(device.SelectMesh, MonchaHub.GetThinkess * AppSt.Default.anchor_size, false));
                }
            }
        }

        private void LoadDeviceSetting(object sender, MouseButtonEventArgs e)
        {
            if (sender is TreeViewItem treeViewItem)
            {
                selectdevice = (MonchaDevice)treeViewItem.DataContext;
                DeviceChange?.Invoke(this, (MonchaDevice)treeViewItem.DataContext);
            }
        }

        private void TreeViewDevice_ContextMenuClosing(object sender, ContextMenuEventArgs e)
        {
            if (sender is TreeViewItem viewItem)
            {
                if (viewItem.ContextMenu.DataContext is MenuItem cmindex && sender is TreeViewItem treeView &&
                            treeView.DataContext is MonchaDevice device)

                    switch (cmindex.Tag)
                    {
                        case "dvc_showrect":
                            //device.Frame = device.CutZone.DrawCutZone();
                            break;

                        case "dvc_showzone":
                            DrawObjects?.Invoke(this, new List<FrameworkElement>() {
                                new CadRectangle(device.Size.P1, device.Size.P2, false){Render = false}
                            });
                            break;
                        case "dvc_polymesh":
                            device.PolyMeshUsed = !device.PolyMeshUsed;
                            MonchaHub.RefreshDevice();
                            break;
                        case "dvc_center":
                            DrawObjects?.Invoke(this, new List<FrameworkElement>() {
                                new CadAnchor(device.Size.Center, false){Render = false}
                            });
                            break;

                    }
            }
        }

        private void TreeBaseMesh(object sender, MouseButtonEventArgs e)
        {
            if (sender is TreeViewItem BaseMeshItem && BaseMeshItem.Parent is TreeViewItem DeviceTree)
            {
                if (DeviceTree.DataContext is MonchaDevice device && BaseMeshItem.DataContext is LDeviceMesh mesh)
                {
                    DrawObjects?.Invoke(this, CadCanvas.GetMesh(mesh, MonchaHub.GetThinkess * AppSt.Default.anchor_size, false));
                }
            }
        }

        private void TreeLaserMeterDevice_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (sender is TreeViewItem viewItem)
            {
                MonchaCadViewer.DeviceManager.LaserMeterWindows laserMeterWindows = new MonchaCadViewer.DeviceManager.LaserMeterWindows(viewItem.DataContext as VLTLaserMeters);
                laserMeterWindows.ShowDialog();
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
                            MonchaHub.RefreshDevice();
                            break;
                    }
            }
        }

        private void TreeViewBaseMesh_ContextMenuClosing(object sender, ContextMenuEventArgs e)
        {
            if (sender is TreeViewItem viewItem)
            {
                if (viewItem.ContextMenu.DataContext is MenuItem cmindex)
                {
                    if (sender is TreeViewItem meshTree && meshTree.Parent is TreeViewItem DeviceTree && DeviceTree.DataContext is MonchaDevice device)
                    {
                        switch (cmindex.Tag)
                        {
                            case "common_Create":
                                CreateGridWindow createGridWindow = new CreateGridWindow(device, device.SelectMesh);
                                createGridWindow.ShowDialog();
                                break;
                            case "m_Refresh":
                                device.RefreshMeshPoint(device.SelectMesh);
                                break;
                            case "mesh_Inverse":
                                device.SelectMesh.InverseYPosition();
                                break;
                            case "mesh_Returnpoint":
                                device.SelectMesh.ReturnPoint();
                                break;
                            case "mesh_Morph":
                                device.SelectMesh.Morph = !device.SelectMesh.Morph;
                                break;
                            case "mesh_Affine":
                                device.SelectMesh.Affine = !device.SelectMesh.Affine;
                                break;
                            case "mesh_ShowRect":
                                DrawObjects?.Invoke(this, new List<FrameworkElement>() { new CadRectangle(device.SelectMesh.Size, false) });
                                break;
                        }
                    }
                }
            }
        }

        private void RemoveLaser_Click(object sender, RoutedEventArgs e) => MonchaHub.RemoveDevice(selectdevice);


        private void RefreshLaser_Click_1(object sender, RoutedEventArgs e)
        {
            MonchaHub.CanPlay = false;
            NeedRefresh?.Invoke(this, true);
            Refresh();
            treeView.UpdateLayout();
        }

        private void AddLaser_Click(object sender, RoutedEventArgs e)
        {
            LaserManager deviceManager = new LaserManager();
            deviceManager.Show();
        }

    }
}
