using CadProjector.Geometry.Primitives;

namespace CadProjector.Core.Scene;

public sealed class Drawable
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = "Object";
    public string? LayerName { get; set; }
    public uint? ColorArgb { get; set; }
    public bool IsVisible { get; set; } = true;
    public Point3 Translation { get; set; } = Point3.Zero;
    public double RotationDeg { get; set; }
    public double Scale { get; set; } = 1;

    /// <summary>One or more polylines in local mm coordinates.</summary>
    public List<List<Point2>> Contours { get; set; } = [];

    /// <summary>
    /// Nested drawables that share this object's transform until ungrouped.
    /// Contours on the group itself are optional; geometry usually lives on children.
    /// </summary>
    public List<Drawable> Children { get; set; } = [];

    public bool IsGroup => Children.Count > 0;

    /// <summary>Wrap members into a group; they keep their current local transforms.</summary>
    public static Drawable CreateGroup(IReadOnlyList<Drawable> members, string name)
    {
        if (members.Count == 0)
            throw new ArgumentException("Group needs at least one object.", nameof(members));

        return new Drawable
        {
            Name = string.IsNullOrWhiteSpace(name) ? "Group" : name,
            Children = members.ToList()
        };
    }

    /// <summary>Wrap imported pieces as one top-level group (file = one aiming unit).</summary>
    public static List<Drawable> WrapAsImportGroup(IReadOnlyList<Drawable> items, string name)
    {
        if (items.Count == 0)
            return [];

        return [CreateGroup(items, name)];
    }

    /// <summary>
    /// Promote children to siblings: bake this transform into each child and clear the group.
    /// </summary>
    public List<Drawable> Ungroup()
    {
        if (Children.Count == 0)
            return [];

        var promoted = new List<Drawable>(Children.Count);
        foreach (var child in Children)
        {
            DrawableSpace.BakeParentTransform(this, child);
            promoted.Add(child);
        }

        Children.Clear();
        Contours.Clear();
        return promoted;
    }

    public Drawable CloneTree() => new()
    {
        Id = Id,
        Name = Name,
        LayerName = LayerName,
        ColorArgb = ColorArgb,
        IsVisible = IsVisible,
        Translation = Translation,
        RotationDeg = RotationDeg,
        Scale = Scale,
        Contours = Contours.Select(c => c.ToList()).ToList(),
        Children = Children.Select(c => c.CloneTree()).ToList()
    };
}
