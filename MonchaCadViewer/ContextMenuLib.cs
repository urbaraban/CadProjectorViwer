using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace CadProjectorViewer.CanvasObj
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

        public static void CadRectMenu(ContextMenu contextMenu)
        {
            contextMenu.Items.Add(new Separator());
            AddItem("common_Setting", contextMenu);
        }


        public static void MeshMenu(ContextMenu contextMenu)
        {
            AddItem("common_Create", contextMenu);
            AddItem("mesh_ShowVirtual", contextMenu);
            AddItem("m_Refresh", contextMenu);
            AddItem("mesh_Inverse", contextMenu);
            AddItem("mesh_Returnpoint", contextMenu);
            AddItem("mesh_Morph", contextMenu);
            AddItem("mesh_Affine", contextMenu);
            AddItem("mesh_ShowRect", contextMenu);
        }

        public static void CanvasMenu(ContextMenu contextMenu)
        {
            AddItem("canvas_freezall", contextMenu);
            AddItem("canvas_unselectall", contextMenu);
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

        private static void AddItem(string Name, ContextMenu menu) => AddItem(Name, null, menu);

        public static void AddItem(string Name, System.Windows.Input.ICommand command, ContextMenu menu)
        {
            MenuItem menuItem = new MenuItem();
            menuItem.SetResourceReference(MenuItem.HeaderProperty, Name);
            menuItem.Tag = Name;
            menuItem.Click += MenuItem_Click;
            menuItem.Command = command;

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
