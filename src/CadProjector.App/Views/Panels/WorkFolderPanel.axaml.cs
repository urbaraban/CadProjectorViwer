using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using CadProjector.App.ViewModels;

namespace CadProjector.App.Views.Panels;

public partial class WorkFolderPanel : UserControl
{
    public WorkFolderPanel()
    {
        InitializeComponent();
        WorkFolderList.DoubleTapped += WorkFolderList_DoubleTapped;
        WorkFolderList.KeyUp += WorkFolderList_KeyUp;
    }

    private async void WorkFolderList_DoubleTapped(object? sender, TappedEventArgs e)
    {
        if (DataContext is not MainViewModel vm) return;
        if (WorkFolderList.SelectedItem is WorkFolderEntry entry)
            await vm.WorkFolderBrowser.ActivateCommand.ExecuteAsync(entry);
    }

    private async void WorkFolderList_KeyUp(object? sender, KeyEventArgs e)
    {
        if (DataContext is not MainViewModel vm) return;

        if (e.Key == Key.Escape)
        {
            vm.WorkFolderBrowser.ClearFilterCommand.Execute(null);
            e.Handled = true;
            return;
        }

        if (e.Key == Key.Enter && WorkFolderList.SelectedItem is WorkFolderEntry entry)
        {
            await vm.WorkFolderBrowser.ActivateCommand.ExecuteAsync(entry);
            e.Handled = true;
        }
    }
}
