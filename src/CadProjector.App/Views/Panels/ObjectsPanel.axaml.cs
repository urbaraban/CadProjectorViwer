using System.ComponentModel;
using Avalonia.Controls;
using Avalonia.Interactivity;
using CadProjector.App.ViewModels;

namespace CadProjector.App.Views.Panels;

public partial class ObjectsPanel : UserControl
{
    private bool _suppressTreeSync;
    private MainViewModel? _wired;

    public ObjectsPanel()
    {
        InitializeComponent();
        ObjectTree.SelectionChanged += ObjectTree_SelectionChanged;
        DataContextChanged += (_, _) => WireViewModel();
        AttachedToVisualTree += (_, _) => WireViewModel();
    }

    private void WireViewModel()
    {
        if (_wired is not null)
            _wired.PropertyChanged -= OnViewModelPropertyChanged;
        _wired = DataContext as MainViewModel;
        if (_wired is not null)
            _wired.PropertyChanged += OnViewModelPropertyChanged;
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName != nameof(MainViewModel.SelectedObjectItem) || _suppressTreeSync)
            return;
        if (sender is not MainViewModel vm)
            return;

        _suppressTreeSync = true;
        try { ObjectTree.SelectedItem = vm.SelectedObjectItem; }
        finally { _suppressTreeSync = false; }
    }

    private void ObjectTree_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (_suppressTreeSync || DataContext is not MainViewModel vm)
            return;
        var items = ObjectTree.SelectedItems.OfType<ObjectListItem>().ToList();
        _suppressTreeSync = true;
        try { vm.SetTreeSelection(items); }
        finally { _suppressTreeSync = false; }
    }
}
