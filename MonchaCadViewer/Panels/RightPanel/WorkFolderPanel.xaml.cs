using CadProjectorSDK;
using CadProjectorSDK.CadObjects.Abstract;
using CadProjectorSDK.Scenes;
using CadProjectorViewer.StaticTools;
using CadProjectorViewer.ViewModel.Modules;
using Microsoft.WindowsAPICodePack.Dialogs;
using Microsoft.Xaml.Behaviors.Core;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
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
using System.Windows.Shapes;
using ToGeometryConverter;
using AppSt = CadProjectorViewer.Properties.Settings;

namespace CadProjectorViewer.Panels.RightPanel
{
    /// <summary>
    /// Логика взаимодействия для WorkFolderPanel.xaml
    /// </summary>
    public partial class WorkFolderPanel : UserControl
    {
        public WorkFolderPanel()
        {
            InitializeComponent();
        }


        private void WorkFolderFilter_GotFocus(object sender, RoutedEventArgs e)
        {
            if (sender is TextBox textBox)
                textBox.SelectAll();
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

        private void WorkFolderFilter_KeyUp(object sender, KeyEventArgs e)
        {
            if (sender is TextBox textBox)
            {
                if (e.Key == Key.Escape)
                {
                    WorkFolderFilter.Text = string.Empty;
                    WorkFolderFilter.Focus();
                }
                else if (e.Key == Key.Enter || e.Key == Key.Down)
                {
                    WorkFolderListBox.Focus();
                    WorkFolderListBox.SelectedIndex = 0;
                }
            }
        }

        private void WorkFolderListBox_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                WorkFolderFilter.Text = string.Empty;
                WorkFolderFilter.Focus();
            }
            else if (e.Key == Key.Enter)
            {
                if (WorkFolderListBox.SelectedItem is FileSystemInfo fileInfo)
                {

                }
            }
        }

        private void ListViewItem_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (this.DataContext is WorkFolderList workFolderList)
            {
                if (sender is ListViewItem viewItem && viewItem.DataContext is FileSystemInfo systemInfo)
                {
                    workFolderList.SelectPathSendCommand(systemInfo).Execute(sender);
                }
            }
        }
    }

    [ValueConversion(typeof(string), typeof(string))]
    public class IconConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string result = string.Empty;
            if (value is string str) {
                if (string.IsNullOrEmpty(str))
                    result = "\uE838";
                else
                    result = "\uE7C3";
            }

            return result;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return null;
        }
    }
}
