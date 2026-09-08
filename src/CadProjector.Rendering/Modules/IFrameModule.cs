using CadProjector.Geometry.Primitives;

namespace CadProjector.Rendering.Modules;

public interface IFrameModule
{
    string Name { get; }
    bool IsEnabled { get; set; }
    LinesCollection Apply(LinesCollection input);
}

/// <summary>A handle the user can grab on the table, in the device's normalized 0..1 space.</summary>
public readonly record struct ModuleAnchor(int Index, Point2 Position, string Label = "");

/// <summary>
/// A module that owns geometry: it can be drawn on the table, dragged by its anchors,
/// and optionally injected into the projected frame.
/// </summary>
public interface IRenderableModule : IFrameModule
{
    /// <summary>Outline of the module in normalized 0..1 device space.</summary>
    LinesCollection GetGeometry();

    /// <summary>Draggable handles; empty when the module is display-only.</summary>
    IReadOnlyList<ModuleAnchor> GetAnchors();

    /// <summary>Moves one handle. Returns false when the module has nothing to move.</summary>
    bool MoveAnchor(int index, Point2 position);
}

/// <summary>Builds the small line collections that renderable modules return.</summary>
public static class GeometryBuilder
{
    public static void AddRect(LinesCollection lines, double x, double y, double width, double height) =>
        AddPolygon(lines,
        [
            new Point2(x, y),
            new Point2(x + width, y),
            new Point2(x + width, y + height),
            new Point2(x, y + height)
        ]);

    public static void AddPolygon(LinesCollection lines, IReadOnlyList<Point2> points)
    {
        if (points.Count < 2)
            return;
        AddSegment(lines, points[^1], points[0]);
        for (var i = 1; i < points.Count; i++)
            AddSegment(lines, points[i - 1], points[i]);
    }

    public static void AddSegment(LinesCollection lines, Point2 a, Point2 b)
    {
        lines.Points.Add(new RenderPoint { X = a.X, Y = a.Y, Blanked = true });
        lines.Points.Add(new RenderPoint { X = b.X, Y = b.Y, Blanked = false });
    }

    /// <summary>Corner handles of a rectangle, counter-clockwise from its origin.</summary>
    public static ModuleAnchor[] RectAnchors(double x, double y, double width, double height) =>
    [
        new(0, new Point2(x, y), "origin"),
        new(1, new Point2(x + width, y), ""),
        new(2, new Point2(x + width, y + height), ""),
        new(3, new Point2(x, y + height), "")
    ];

    /// <summary>Applies a corner drag, keeping the opposite corner pinned.</summary>
    public static (double X, double Y, double Width, double Height) DragRectCorner(
        int corner, Point2 to, double x, double y, double width, double height, double minSize = 0.01)
    {
        double x0 = x, y0 = y, x1 = x + width, y1 = y + height;
        switch (corner)
        {
            case 0: x0 = to.X; y0 = to.Y; break;
            case 1: x1 = to.X; y0 = to.Y; break;
            case 2: x1 = to.X; y1 = to.Y; break;
            case 3: x0 = to.X; y1 = to.Y; break;
            default: return (x, y, width, height);
        }
        return (
            Math.Min(x0, x1),
            Math.Min(y0, y1),
            Math.Max(minSize, Math.Abs(x1 - x0)),
            Math.Max(minSize, Math.Abs(y1 - y0)));
    }
}

/// <summary>Base for modules that only move points (legacy <c>DeviceModule.CorrectPoint</c>).</summary>
public abstract class PointModule : IFrameModule
{
    public abstract string Name { get; }
    public bool IsEnabled { get; set; } = true;

    public LinesCollection Apply(LinesCollection input)
    {
        if (!IsEnabled)
            return input;

        var output = new LinesCollection();
        foreach (var p in input.Points)
            output.Points.Add(Correct(p.Clone()));
        return output;
    }

    protected abstract RenderPoint Correct(RenderPoint point);
}

/// <summary>Editor hints for a module property exposed as a user-facing parameter.</summary>
[AttributeUsage(AttributeTargets.Property)]
public sealed class ModuleParamAttribute : Attribute
{
    public string? Label { get; set; }
    public double Min { get; set; } = double.NaN;
    public double Max { get; set; } = double.NaN;
    public double Increment { get; set; } = double.NaN;
    public string? Format { get; set; }
}

/// <summary>Excludes a public property from the generated parameter list.</summary>
[AttributeUsage(AttributeTargets.Property)]
public sealed class ModuleParamIgnoreAttribute : Attribute;
