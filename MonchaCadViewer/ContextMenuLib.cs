using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace MonchaCadViewer.CanvasObj
{
    public static class ContextMenuLib
    {
        public static void LineContextMenu(ContextMenu contextMenu)
        {
            if (contextMenu.Items.Count > 0)
                contextMenu.Items.Add(new Separator());

        }

        public static void DotContextMenu(ContextMenu contextMenu)
        {
            if (contextMenu.Items.Count > 0)
                contextMenu.Items.Add(new Separator());
            AddItem("common_Edit", contextMenu);

        }

        public static void ViewContourMenu(ContextMenu contextMenu)
        {
            if (contextMenu != null)
            {
                if (contextMenu.Items.Count > 0)
                    contextMenu.Items.Add(new Separator());
            }

            AddItem("obj_Mirror", contextMenu);
        }

        public static void CadObjMenu(ContextMenu contextMenu)
        {
            AddItem("obj_Fix", contextMenu);
            AddItem("common_Remove", contextMenu);
            AddItem("obj_Render", contextMenu);
            AddItem("m_Open", contextMenu);
        }


        public static void MeshMenu(ContextMenu contextMenu)
        {
            AddItem("common_Create", contextMenu);
            AddItem("m_Refresh", contextMenu);
            AddItem("mesh_Inverse", contextMenu);
            AddItem("mesh_Returnpoint", contextMenu);
            AddItem("mesh_Morph", contextMenu);
            AddItem("mesh_Affine", contextMenu);
        }

        public static void CanvasMenu(ContextMenu contextMenu)
        {
            AddItem("canvas_freezall", contextMenu);
            AddItem("canvas_unselectall", contextMenu);
        }

        public static void DeviceTreeMenu(ContextMenu contextMenu)
        {
            AddItem("dvc_showrect", contextMenu);
            AddItem("dvc_showzone", contextMenu);
            AddItem("dvc_polymesh", contextMenu);
        }

        public static void LaserMeterHeadTreeMenu(ContextMenu contextMenu)
        {
            AddItem("common_ADD", contextMenu);
        }
        public static void LaserMeterDeviceTreeMenu(ContextMenu contextMenu)
        {
            AddItem("common_Edit", contextMenu);
            AddItem("common_Remove", contextMenu);
        }

        private static void AddItem(string Name, ContextMenu menu)
        {
            MenuItem menuItem = new MenuItem();
            menuItem.SetResourceReference(MenuItem.HeaderProperty, Name);
            menuItem.Tag = Name;
            menuItem.Click += MenuItem_Click;

            menu.Items.Add(menuItem);
        }

        private static void MenuItem_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            DependencyObject obj = sender as DependencyObject;

            while (obj != null && !(obj is ContextMenu))
            {
                obj = VisualTreeHelper.GetParent(obj);
            }
            (obj as ContextMenu).DataContext = sender;
        }

    }
}
