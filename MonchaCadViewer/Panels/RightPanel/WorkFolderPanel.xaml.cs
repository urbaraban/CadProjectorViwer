using CadProjectorViewer.ViewModel.Modules;
using System;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;

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
                if (this.DataContext is WorkFolderList workFolderList && 
                    WorkFolderListBox.SelectedItem is FileSystemInfo fileInfo)
                {
                    workFolderList.SelectPathSendCommand(fileInfo).Execute(sender);
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

        private void GridViewColumnHeader_Click(object sender, RoutedEventArgs e)
        {
            if (e.OriginalSource is GridViewColumnHeader columnHeader)
            {
                var columnBinding = columnHeader.Column.DisplayMemberBinding as Binding;
                var sortBy = columnBinding?.Path.Path ?? columnHeader.Column.Header as string;
                if (sortBy != null)
                {
                    SortList(sortBy);
                }
            }
        }

        private void SortList(string HeaderName)
        {
            ListSortDirection listSortDirection = ListSortDirection.Ascending;
            ICollectionView dataView = CollectionViewSource.GetDefaultView(WorkFolderListBox.ItemsSource);
            if (dataView.SortDescriptions.Count > 0 &&
                dataView.SortDescriptions[0].PropertyName == HeaderName &&
                dataView.SortDescriptions[0].Direction == ListSortDirection.Ascending)
            {
                listSortDirection = ListSortDirection.Descending;
            }

            dataView.SortDescriptions.Clear();
            SortDescription sd = new SortDescription(HeaderName, listSortDirection);
            dataView.SortDescriptions.Add(sd);
            dataView.Refresh();

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
