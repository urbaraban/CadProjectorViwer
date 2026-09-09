using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CadProjector.Core.Scene;

namespace CadProjector.App.ViewModels;

public partial class ObjectListItem : ObservableObject
{
    public ObjectListItem(Drawable drawable, int rootIndex, ObjectListItem? parent = null)
    {
        Drawable = drawable;
        RootIndex = rootIndex;
        Parent = parent;
        IsVisible = drawable.IsVisible;
        IsLocked = drawable.IsLocked;
        Name = drawable.Name;
        IsExpanded = drawable.IsGroup;
        foreach (var child in drawable.Children)
            Children.Add(new ObjectListItem(child, rootIndex, this));
    }

    public Drawable Drawable { get; }
    public ObjectListItem? Parent { get; }
    public int RootIndex { get; }
    public ObservableCollection<ObjectListItem> Children { get; } = [];

    public ObjectListItem Root => Parent?.Root ?? this;

    public string DisplayName
    {
        get
        {
            if (!string.IsNullOrWhiteSpace(Drawable.LayerName)
                && !string.Equals(Drawable.LayerName, Name, StringComparison.OrdinalIgnoreCase))
                return $"{Drawable.LayerName} / {Name}";
            return Name;
        }
    }

    public string? LayerName => Drawable.LayerName;

    [ObservableProperty] public partial string Name { get; set; }
    [ObservableProperty] public partial bool IsVisible { get; set; }
    [ObservableProperty] public partial bool IsLocked { get; set; }
    [ObservableProperty] public partial bool IsExpanded { get; set; }

    partial void OnNameChanged(string value)
    {
        if (string.Equals(Drawable.Name, value, StringComparison.Ordinal))
            return;
        Drawable.Name = value;
        OnPropertyChanged(nameof(DisplayName));
        NameChanged?.Invoke(this, EventArgs.Empty);
    }

    partial void OnIsVisibleChanged(bool value)
    {
        Drawable.IsVisible = value;
        VisibilityChanged?.Invoke(this, EventArgs.Empty);
    }

    partial void OnIsLockedChanged(bool value)
    {
        Drawable.IsLocked = value;
        LockChanged?.Invoke(this, EventArgs.Empty);
    }

    public event EventHandler? VisibilityChanged;
    public event EventHandler? LockChanged;
    public event EventHandler? NameChanged;

    public void WireTreeEvents(EventHandler visibility, EventHandler lockChanged, EventHandler nameChanged)
    {
        VisibilityChanged += visibility;
        LockChanged += lockChanged;
        NameChanged += nameChanged;
        foreach (var child in Children)
            child.WireTreeEvents(visibility, lockChanged, nameChanged);
    }

    public void UnwireTreeEvents(EventHandler visibility, EventHandler lockChanged, EventHandler nameChanged)
    {
        VisibilityChanged -= visibility;
        LockChanged -= lockChanged;
        NameChanged -= nameChanged;
        foreach (var child in Children)
            child.UnwireTreeEvents(visibility, lockChanged, nameChanged);
    }

    public IEnumerable<ObjectListItem> EnumerateSelfAndDescendants()
    {
        yield return this;
        foreach (var child in Children)
        foreach (var d in child.EnumerateSelfAndDescendants())
            yield return d;
    }
}
