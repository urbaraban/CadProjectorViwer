using CadProjectorSDK;
using CadProjectorSDK.Device;
using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using CadProjectorSDK.Device.Mesh;
using CadProjectorSDK.Device.Controllers;
using CadProjectorSDK.Interfaces;
using CadProjectorSDK.Scenes;
using CadProjectorViewer.ViewModel;

namespace CadProjectorViewer.Panels
{
    /// <summary>
    /// Логика взаимодействия для DeviceTreeTab.xaml
    /// </summary>
    public partial class DeviceTreeTab : UserControl, INotifyPropertyChanged
    {
        public event EventHandler<bool> NeedRefresh;
        public event EventHandler<LProjector> DeviceChange;

        private DeviceManagmentVM deviceManagment => (DeviceManagmentVM)this.DataContext;

        public DeviceTreeTab()
        {
            InitializeComponent();
        }


        private void TreeBaseMesh(object sender, MouseButtonEventArgs e)
        {
            if (sender is TreeViewItem BaseMeshItem && BaseMeshItem.Parent is TreeViewItem DeviceTree)
            {
                if (DeviceTree.DataContext is LProjector device && BaseMeshItem.DataContext is ProjectorMesh mesh)
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

        private void AddLaser_Click(object sender, RoutedEventArgs e)
        {
            LaserSearcher DeviceManaged = new LaserSearcher(deviceManagment.AppMain);
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
                            case "mesh_YMirror":
                                mesh.MirrorMesh();
                                break;
                            case "mesh_returnpoint":
                                mesh.ReturnPoint();
                                break;
                            case "mesh_showrect":
                                deviceManagment.AppMain.Scenes.SelectedScene.Add(mesh.Size);
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
                        var task = new SceneTask(mesh);
                        task.Command.Add("PLAY");
                        task.Command.Add("SHOW");

                        if (Keyboard.Modifiers != ModifierKeys.Shift)
                        {
                            task.Command.Add("CLEAR");
                        }

                        deviceManagment.AppMain.Scenes.RunTask(task);
                    }
                }
            }
        }

        private LProjector selectdevice;
        private void treeView_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (e.NewValue is LProjector device)
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
