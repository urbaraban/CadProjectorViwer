using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using CadProjector.Core.Project;
using CadProjector.Core.Scene;
using CadProjector.Geometry.Camera;
using CadProjector.Geometry.Mesh;
using CadProjector.Geometry.Primitives;

namespace CadProjector.App.Controls;

/// <summary>Software 3D orbit view of the scene plane, shaded STL and projected drawings.</summary>
public sealed class SceneViewport3D : Control
{
    private WriteableBitmap? _shade;
    private float[]? _zbuf;

    public static readonly StyledProperty<ProjectionScene?> SceneProperty =
        AvaloniaProperty.Register<SceneViewport3D, ProjectionScene?>(nameof(Scene));

    public static readonly StyledProperty<ProjectDocument?> ProjectProperty =
        AvaloniaProperty.Register<SceneViewport3D, ProjectDocument?>(nameof(Project));

    public static readonly StyledProperty<int> RevisionProperty =
        AvaloniaProperty.Register<SceneViewport3D, int>(nameof(Revision));

    public static readonly StyledProperty<MeshAlignPickKind> AlignPickProperty =
        AvaloniaProperty.Register<SceneViewport3D, MeshAlignPickKind>(nameof(AlignPick));

    public static readonly StyledProperty<IReadOnlyList<MeshAlignMarker>?> AlignMarkersProperty =
        AvaloniaProperty.Register<SceneViewport3D, IReadOnlyList<MeshAlignMarker>?>(nameof(AlignMarkers));

    public event Action<Point3, bool>? AlignPicked;

    public OrbitCamera Camera { get; } = new();

    public ProjectionScene? Scene
    {
        get => GetValue(SceneProperty);
        set => SetValue(SceneProperty, value);
    }

    public ProjectDocument? Project
    {
        get => GetValue(ProjectProperty);
        set => SetValue(ProjectProperty, value);
    }

    public int Revision
    {
        get => GetValue(RevisionProperty);
        set => SetValue(RevisionProperty, value);
    }

    public MeshAlignPickKind AlignPick
    {
        get => GetValue(AlignPickProperty);
        set => SetValue(AlignPickProperty, value);
    }

    public IReadOnlyList<MeshAlignMarker>? AlignMarkers
    {
        get => GetValue(AlignMarkersProperty);
        set => SetValue(AlignMarkersProperty, value);
    }

    private enum DragMode { None, Orbit, Pan }
    private DragMode _drag;
    private Point _last;
    private bool _needsFit = true;

    static SceneViewport3D()
    {
        AffectsRender<SceneViewport3D>(SceneProperty, ProjectProperty, RevisionProperty, AlignPickProperty, AlignMarkersProperty);
        FocusableProperty.OverrideDefaultValue<SceneViewport3D>(true);
        ClipToBoundsProperty.OverrideDefaultValue<SceneViewport3D>(true);
    }

    public void ResetView()
    {
        _needsFit = true;
        InvalidateVisual();
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == SceneProperty)
            _needsFit = true;
    }

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        Focus();
        _last = e.GetPosition(this);
        var p = e.GetCurrentPoint(this).Properties;
        if (AlignPick != MeshAlignPickKind.Off && p.IsLeftButtonPressed)
        {
            FireAlignPick(_last);
            e.Handled = true;
            return;
        }

        _drag = p.IsLeftButtonPressed ? DragMode.Orbit
            : p.IsRightButtonPressed || p.IsMiddleButtonPressed ? DragMode.Pan
            : DragMode.None;
        if (_drag != DragMode.None)
            e.Pointer.Capture(this);
        e.Handled = true;
        base.OnPointerPressed(e);
    }

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        if (_drag == DragMode.None)
            return;
        var pos = e.GetPosition(this);
        var dx = pos.X - _last.X;
        var dy = pos.Y - _last.Y;
        _last = pos;
        if (_drag == DragMode.Orbit)
            Camera.Orbit(dx * 0.35, -dy * 0.35);
        else
            Camera.Pan(dx, dy, Bounds.Width, Bounds.Height);
        InvalidateVisual();
        e.Handled = true;
    }

    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        _drag = DragMode.None;
        e.Pointer.Capture(null);
        base.OnPointerReleased(e);
    }

    protected override void OnPointerWheelChanged(PointerWheelEventArgs e)
    {
        Camera.Zoom(e.Delta.Y > 0 ? 0.9 : 1.12);
        InvalidateVisual();
        e.Handled = true;
    }

    protected override void OnPointerCaptureLost(PointerCaptureLostEventArgs e)
    {
        _drag = DragMode.None;
        base.OnPointerCaptureLost(e);
    }

    protected override void OnDoubleTapped(TappedEventArgs e)
    {
        ResetView();
        e.Handled = true;
    }

    public override void Render(DrawingContext context)
    {
        var bounds = Bounds;
        context.FillRectangle(new SolidColorBrush(Color.FromRgb(15, 17, 21)), new Rect(bounds.Size));
        var scene = Scene;
        if (scene is null || bounds.Width < 4 || bounds.Height < 4)
            return;

        if (_needsFit)
        {
            FitToScene(scene, bounds.Width / Math.Max(1, bounds.Height));
            _needsFit = false;
        }

        var w = bounds.Width;
        var h = bounds.Height;

        if (scene.MeshTarget?.WorldMesh is { TriangleCount: > 0 } world)
        {
            var cam = Camera;
            MeshShadeBlit.Draw(
                context, ref _shade, ref _zbuf, bounds.Size, world, cam.Eye,
                (Point3 p, out float x, out float y, out float d) =>
                {
                    if (!cam.TryProject(p, w, h, out var s, out var depth))
                    {
                        x = y = d = 0;
                        return false;
                    }
                    x = (float)s.X;
                    y = (float)s.Y;
                    d = (float)depth;
                    return true;
                });
        }

        var lines = new List<(double Depth, IPen Pen, Point2 A, Point2 B)>();

        var planePen = new Pen(new SolidColorBrush(Color.FromRgb(70, 78, 90)), 1.2);
        AddRect(lines, planePen,
            new Point3(0, 0, 0),
            new Point3(scene.Target.WidthMm, 0, 0),
            new Point3(scene.Target.WidthMm, scene.Target.HeightMm, 0),
            new Point3(0, scene.Target.HeightMm, 0),
            w, h);

        var axis = Math.Max(scene.Target.WidthMm, scene.Target.HeightMm) * 0.08;
        AddSeg(lines, new Pen(new SolidColorBrush(Color.FromRgb(220, 80, 80)), 1.6), Point3.Zero, new Point3(axis, 0, 0), w, h);
        AddSeg(lines, new Pen(new SolidColorBrush(Color.FromRgb(80, 200, 90)), 1.6), Point3.Zero, new Point3(0, axis, 0), w, h);
        AddSeg(lines, new Pen(new SolidColorBrush(Color.FromRgb(80, 140, 255)), 1.6), Point3.Zero, new Point3(0, 0, axis), w, h);

        if (scene.MeshTarget is { } mesh)
        {
            var boxPen = new Pen(new SolidColorBrush(Color.FromArgb(80, 120, 200, 255)), 1.0);
            foreach (var (a, b) in mesh.GetBoundsEdges())
                AddSeg(lines, boxPen, a, b, w, h);
        }

        var project = Project;
        var hitColor = project is null
            ? Colors.OrangeRed
            : Color.FromUInt32(project.SolidColorArgb);
        var hitPen = new Pen(new SolidColorBrush(hitColor), 1.8);
        var missPen = new Pen(new SolidColorBrush(Color.FromRgb(255, 90, 90)), 1.4);

          foreach (var stroke in SceneStrokes3.FromScene(scene))
            AddSeg(lines, stroke.OnSurface ? hitPen : missPen, stroke.A, stroke.B, w, h);

        foreach (var seg in lines.OrderByDescending(s => s.Depth))
        {
            context.DrawLine(seg.Pen,
                new Avalonia.Point(seg.A.X, seg.A.Y),
                new Avalonia.Point(seg.B.X, seg.B.Y));
        }

        if (AlignMarkers is { Count: > 0 } marks)
        {
            foreach (var m in marks)
            {
                if (!Camera.TryProject(m.World, w, h, out var s, out _))
                    continue;
                var color = m.OnMesh ? Color.FromRgb(255, 170, 60) : Color.FromRgb(80, 200, 255);
                var pen = new Pen(new SolidColorBrush(color), 1.6);
                var p = new Avalonia.Point(s.X, s.Y);
                context.DrawLine(pen, new Avalonia.Point(p.X - 8, p.Y), new Avalonia.Point(p.X + 8, p.Y));
                context.DrawLine(pen, new Avalonia.Point(p.X, p.Y - 8), new Avalonia.Point(p.X, p.Y + 8));
                context.DrawEllipse(null, pen, p, 5, 5);
            }
        }
    }

    private void FireAlignPick(Avalonia.Point screen)
    {
        var scene = Scene;
        if (scene is null)
            return;
        var w = Bounds.Width;
        var h = Bounds.Height;
        if (!Camera.TryRay(screen.X, screen.Y, w, h, out var origin, out var dir))
            return;

        if (AlignPick == MeshAlignPickKind.OnMesh)
        {
            if (scene.MeshTarget is { } mesh && mesh.TryHitRay(origin, dir, out var hit))
                AlignPicked?.Invoke(hit, true);
            else
                AlignPicked?.Invoke(default, false);
            return;
        }

        if (Math.Abs(dir.Z) < 1e-9)
            return;
        var t = -origin.Z / dir.Z;
        if (t <= 0)
            return;
        AlignPicked?.Invoke(origin + dir * t, false);
    }

    private void FitToScene(ProjectionScene scene, double aspect)
    {
        var box = Aabb3.Empty
            .Encapsulate(Point3.Zero)
            .Encapsulate(new Point3(scene.Target.WidthMm, scene.Target.HeightMm, 0));
        if (scene.MeshTarget is { } mesh)
            box = box.Encapsulate(mesh.WorldBounds);
        Camera.Fit(box, aspect);
    }

    private void AddRect(
        List<(double Depth, IPen Pen, Point2 A, Point2 B)> lines,
        IPen pen,
        Point3 a, Point3 b, Point3 c, Point3 d,
        double w, double h)
    {
        AddSeg(lines, pen, a, b, w, h);
        AddSeg(lines, pen, b, c, w, h);
        AddSeg(lines, pen, c, d, w, h);
        AddSeg(lines, pen, d, a, w, h);
    }

    private void AddSeg(
        List<(double Depth, IPen Pen, Point2 A, Point2 B)> lines,
        IPen pen,
        Point3 a,
        Point3 b,
        double w,
        double h)
    {
        if (!Camera.TryProject(a, w, h, out var sa, out var da))
            return;
        if (!Camera.TryProject(b, w, h, out var sb, out var db))
            return;
        lines.Add(((da + db) * 0.5, pen, sa, sb));
    }
}
