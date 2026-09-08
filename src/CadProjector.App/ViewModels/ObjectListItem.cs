using CommunityToolkit.Mvvm.ComponentModel;
using CadProjector.Core.Scene;

namespace CadProjector.App.ViewModels;

public partial class ObjectListItem : ObservableObject
{
    public ObjectListItem(Drawable drawable, int index)
    {
        Drawable = drawable;
        Index = index;
        IsVisible = drawable.IsVisible;
    }

    public Drawable Drawable { get; }
    public int Index { get; }

    public string DisplayName =>
        string.IsNullOrWhiteSpace(Drawable.LayerName)
            ? Drawable.Name
            : $"{Drawable.LayerName} / {Drawable.Name}";

    public string? LayerName => Drawable.LayerName;

    [ObservableProperty] public partial bool IsVisible { get; set; }

    partial void OnIsVisibleChanged(bool value)
    {
        Drawable.IsVisible = value;
        VisibilityChanged?.Invoke(this, EventArgs.Empty);
    }

    public event EventHandler? VisibilityChanged;
}
