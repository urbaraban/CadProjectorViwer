using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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

        }

        public static void ViewContourMenu(ContextMenu contextMenu)
        {
            if (contextMenu.Items.Count > 0)
                contextMenu.Items.Add(new Separator());

            List<MenuItem> items = new List<MenuItem>();
            MenuItem menuItem = new MenuItem();
            menuItem.Header = "Mirror";
            menuItem.Click += MenuItem_Click;
            contextMenu.Items.Add(menuItem);
        }

        public static void CadObjMenu(ContextMenu contextMenu)
        {
            MenuItem menuItem = new MenuItem();
            menuItem.Header = "Fix";
            menuItem.Click += MenuItem_Click;

            MenuItem menuItem2 = new MenuItem();
            menuItem2.Header = "Remove";
            menuItem2.Click += MenuItem_Click;

            contextMenu.Items.Add(menuItem);
            contextMenu.Items.Add(menuItem2);
        }

        public static void MeshMenu(ContextMenu contextMenu)
        {
            MenuItem menuItem = new MenuItem();
            menuItem.Header = "Create";
            menuItem.Click += MenuItem_Click;

            MenuItem menuItem2 = new MenuItem();
            menuItem2.Header = "Refresh";
            menuItem2.Click += MenuItem_Click;

            contextMenu.Items.Add(menuItem);
            contextMenu.Items.Add(menuItem2);
        }

        public static void DeviceTreeMenu(ContextMenu contextMenu)
        {
            MenuItem menuItem = new MenuItem();
            menuItem.Header = "Rectangle";
            menuItem.Click += MenuItem_Click;
            contextMenu.Items.Add(menuItem);
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
