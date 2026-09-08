using System.Collections.ObjectModel;
using CadProjector.Core.Devices;
using CadProjector.Rendering.Modules;
using CommunityToolkit.Mvvm.ComponentModel;

namespace CadProjector.App.ViewModels;

public partial class ModuleItemViewModel : ObservableObject
{
    private readonly Action<DeviceModuleConfig, ModuleState>? _changed;
    private readonly bool _ready;

    public ModuleItemViewModel(
        DeviceModuleConfig model,
        Action<DeviceModuleConfig, ModuleState>? changed = null)
    {
        Model = model;
        _changed = changed;
        Descriptor = ModuleRegistry.Find(model.TypeId);
        IsEnabled = model.IsEnabled;
        ShowOnTable = model.ShowOnTable;
        ProjectGeometry = model.ProjectGeometry;

        foreach (var p in Descriptor?.Parameters ?? [])
            Parameters.Add(new ModuleParamViewModel(model, p, changed));

        _ready = true;
    }

    public DeviceModuleConfig Model { get; }
    public ModuleDescriptor? Descriptor { get; }
    public ObservableCollection<ModuleParamViewModel> Parameters { get; } = [];

    public string DisplayName => Descriptor?.DisplayName ?? Model.DisplayName;
    public string Description => Descriptor?.Description ?? "";
    public string TypeId => Model.TypeId;
    public bool HasParameters => Parameters.Count > 0;

    /// <summary>True for modules that draw an outline and handles on the table.</summary>
    public bool HasGeometry => Descriptor?.HasGeometry ?? false;

    [ObservableProperty] public partial bool IsEnabled { get; set; }
    [ObservableProperty] public partial bool ShowOnTable { get; set; }
    [ObservableProperty] public partial bool ProjectGeometry { get; set; }
    [ObservableProperty] public partial bool IsExpanded { get; set; }

    partial void OnIsEnabledChanged(bool value)
    {
        if (!_ready) return;
        Toggle(() => Model.IsEnabled = value);
    }

    partial void OnShowOnTableChanged(bool value)
    {
        if (!_ready) return;
        Toggle(() => Model.ShowOnTable = value);
    }

    partial void OnProjectGeometryChanged(bool value)
    {
        if (!_ready) return;
        Toggle(() => Model.ProjectGeometry = value);
    }

    private void Toggle(Action mutate)
    {
        var before = Model.CaptureState();
        mutate();
        _changed?.Invoke(Model, before);
    }
}
