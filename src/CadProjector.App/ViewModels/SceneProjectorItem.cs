using CadProjector.Core.Devices;
using CommunityToolkit.Mvvm.ComponentModel;

namespace CadProjector.App.ViewModels;

/// <summary>One projector row in the Scene panel — toggle binds it to the active scene.</summary>
public partial class SceneProjectorItem : ObservableObject
{
    private readonly Action<string, bool> _setBound;
    private readonly bool _ready;

    public SceneProjectorItem(ProjectorProfile projector, bool isBound, Action<string, bool> setBound)
    {
        ProjectorId = projector.Id;
        DisplayName = projector.DisplayName;
        Detail = $"{projector.Host}:{projector.Port}";
        _setBound = setBound;
        IsBound = isBound;
        _ready = true;
    }

    public string ProjectorId { get; }
    public string DisplayName { get; }
    public string Detail { get; }

    [ObservableProperty] public partial bool IsBound { get; set; }

    partial void OnIsBoundChanged(bool value)
    {
        if (!_ready) return;
        _setBound(ProjectorId, value);
    }
}
