using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using CadProjector.Core.Devices;
using CadProjector.Core.Project;
using CadProjector.Core.Scene;
using CadProjector.Geometry.Camera;
using CadProjector.Geometry.Mesh;
using CadProjector.Geometry.Primitives;

namespace CadProjector.App.Controls;

/// <summary>
/// Software 3D view of the scene plane, shaded STL and projected drawings. Looks either
/// through the free orbit camera or through a projector standing where the real one stands.
/// </summary>
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

    /// <summary>When set, the view looks through this projector instead of the orbit camera.</summary>
    public static readonly StyledProperty<ProjectorProfile?> ViewProjectorProperty =
        AvaloniaProperty.Register<SceneViewport3D, ProjectorProfile?>(nameof(ViewProjector));

    /// <summary>When set, the STL is tinted by what this projector can reach.</summary>
    public static readonly StyledProperty<ProjectorProfile?> CoverageProjectorProperty =
        AvaloniaProperty.Register<SceneViewport3D, ProjectorProfile?>(nameof(CoverageProjector));

    /// <summary>Live projectors, drawn as rig markers so their placement is visible.</summary>
    public static readonly StyledProperty<IReadOnlyList<ProjectorProfile>?> RigsProperty =
        AvaloniaProperty.Register<SceneViewport3D, IReadOnlyList<ProjectorProfile>?>(nameof(Rigs));

    public event Action<Point3, bool>? AlignPicked;

    /// <summary>Fresh coverage numbers whenever the tint is recomputed.</summary>
    public event Action<MeshCoverageStats, bool>? CoverageComputed;

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

    public ProjectorProfile? ViewProjector
    {
        get => GetValue(ViewProjectorProperty);
        set => SetValue(ViewProjectorProperty, value);
    }

    public ProjectorProfile? CoverageProjector
    {
        get => GetValue(CoverageProjectorProperty);
        set => SetValue(CoverageProjectorProperty, value);
    }

    public IReadOnlyList<ProjectorProfile>? Rigs
    {
        get => GetValue(RigsProperty);
        set => SetValue(RigsProperty, value);
    }

    private enum DragMode { None, Orbit, Pan }
    private DragMode _drag;
    private Point _last;
    private bool _needsFit = true;

    private byte[]? _coverage;
    private (object Mesh, double X, double Y, double Z, double Pitch, double Yaw, double Roll, double Fh, double Fv)? _coverageKey;

    static SceneViewport3D()
    {
        AffectsRender<SceneViewport3D>(
            SceneProperty, ProjectProperty, RevisionProperty,
            AlignPickProperty, AlignMarkersProperty,
            ViewProjectorProperty, CoverageProjectorProperty, RigsProperty);
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
        {
            _needsFit = true;
            _coverageKey = null;
        }
        else if (change.Property == RevisionProperty)
            _coverageKey = null;
    }

    /// <summary>Orbit gestures are meaningless while the view is pinned to a projector.</summary>
    private bool IsProjectorView => ViewProjector is not null;

    private ISceneCamera ActiveCamera =>
        ViewProjector is { } p ? p.Pose.ToCamera() : Camera;

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

        if (IsProjectorView)
        {
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
        if (IsProjectorView)
            return;
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
        var cam = ActiveCamera;
        var lines = new List<(double Depth, IPen Pen, Point2 A, Point2 B)>();

        void AddSeg(IPen pen, Point3 a, Point3 b)
        {
            if (!cam.TryProject(a, w, h, out var sa, out var da))
                return;
            if (!cam.TryProject(b, w, h, out var sb, out var db))
                return;
            lines.Add(((da + db) * 0.5, pen, sa, sb));
        }

        if (scene.MeshTarget?.WorldMesh is { TriangleCount: > 0 } world)
        {
            var tint = ResolveCoverage(scene.MeshTarget, world.TriangleCount);
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
                },
                tint,
                tint is null ? null : MeshShadeBlit.CoverageRgb);
        }

        var planePen = new Pen(new SolidColorBrush(Color.FromRgb(70, 78, 90)), 1.2);
        var c0 = new Point3(0, 0, 0);
        var c1 = new Point3(scene.Target.WidthMm, 0, 0);
        var c2 = new Point3(scene.Target.WidthMm, scene.Target.HeightMm, 0);
        var c3 = new Point3(0, scene.Target.HeightMm, 0);
        AddSeg(planePen, c0, c1);
        AddSeg(planePen, c1, c2);
        AddSeg(planePen, c2, c3);
        AddSeg(planePen, c3, c0);

        var axis = Math.Max(scene.Target.WidthMm, scene.Target.HeightMm) * 0.08;
        AddSeg(new Pen(new SolidColorBrush(Color.FromRgb(220, 80, 80)), 1.6), Point3.Zero, new Point3(axis, 0, 0));
        AddSeg(new Pen(new SolidColorBrush(Color.FromRgb(80, 200, 90)), 1.6), Point3.Zero, new Point3(0, axis, 0));
        AddSeg(new Pen(new SolidColorBrush(Color.FromRgb(80, 140, 255)), 1.6), Point3.Zero, new Point3(0, 0, axis));

        if (scene.MeshTarget is { } mesh)
        {
            var boxPen = new Pen(new SolidColorBrush(Color.FromArgb(80, 120, 200, 255)), 1.0);
            foreach (var (a, b) in mesh.GetBoundsEdges())
                AddSeg(boxPen, a, b);
        }

        DrawProjectorRigs(scene, AddSeg);

        var project = Project;
        var hitColor = project is null
            ? Colors.OrangeRed
            : Color.FromUInt32(project.SolidColorArgb);
        var hitPen = new Pen(new SolidColorBrush(hitColor), 1.8);
        var missPen = new Pen(new SolidColorBrush(Color.FromRgb(255, 90, 90)), 1.4);

        foreach (var stroke in SceneStrokes3.FromScene(scene))
            AddSeg(stroke.OnSurface ? hitPen : missPen, stroke.A, stroke.B);

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
                if (!cam.TryProject(m.World, w, h, out var s, out _))
                    continue;
                var color = m.OnMesh ? Color.FromRgb(255, 170, 60) : Color.FromRgb(80, 200, 255);
                var pen = new Pen(new SolidColorBrush(color), 1.6);
                var p = new Avalonia.Point(s.X, s.Y);
                context.DrawLine(pen, new Avalonia.Point(p.X - 8, p.Y), new Avalonia.Point(p.X + 8, p.Y));
                context.DrawLine(pen, new Avalonia.Point(p.X, p.Y - 8), new Avalonia.Point(p.X, p.Y + 8));
                context.DrawEllipse(null, pen, p, 5, 5);
            }
        }

        if (ViewProjector is { } viewer)
        {
            context.DrawText(
                new FormattedText(
                    viewer.DisplayName,
                    System.Globalization.CultureInfo.CurrentCulture,
                    FlowDirection.LeftToRight,
                    Typeface.Default,
                    13,
                    new SolidColorBrush(Color.FromRgb(255, 200, 60))),
                new Avalonia.Point(12, 12));
        }
    }

    /// <summary>Rig markers so the operator can see where each device sits relative to the part.</summary>
    private void DrawProjectorRigs(ProjectionScene scene, Action<IPen, Point3, Point3> addSeg)
    {
        if (Rigs is not { Count: > 0 } rigs)
            return;

        var span = Math.Max(scene.Target.WidthMm, scene.Target.HeightMm) * 0.05;
        foreach (var p in rigs)
        {
            if (ReferenceEquals(p, ViewProjector))
                continue;

            var pos = p.Pose.PositionMm;
            var selected = ReferenceEquals(p, CoverageProjector);
            var color = selected ? Color.FromRgb(255, 200, 60) : Color.FromArgb(140, 150, 170, 200);
            var pen = new Pen(new SolidColorBrush(color), selected ? 1.6 : 1.0);

            addSeg(pen, pos - new Point3(span, 0, 0), pos + new Point3(span, 0, 0));
            addSeg(pen, pos - new Point3(0, span, 0), pos + new Point3(0, span, 0));
            addSeg(pen, pos - new Point3(0, 0, span), pos + new Point3(0, 0, span));

            p.Pose.ToCamera().GetBasis(out var forward, out _, out _);
            addSeg(pen, pos, pos + forward * (span * 6));
        }
    }

    private byte[]? ResolveCoverage(MeshTarget mesh, int triangleCount)
    {
        if (CoverageProjector is not { } p)
        {
            _coverage = null;
            _coverageKey = null;
            return null;
        }

        var pose = p.Pose;
        var key = (
            (object)mesh,
            pose.PositionMm.X, pose.PositionMm.Y, pose.PositionMm.Z,
            pose.PitchDeg, pose.YawDeg, pose.RollDeg,
            pose.FovHDeg, pose.FovVDeg);

        if (_coverage is { } cached && cached.Length == triangleCount && _coverageKey == key)
            return cached;

        var result = MeshCoverage.Classify(mesh, pose.ToCamera());
        _coverage = result.Facets;
        _coverageKey = key;
        var stats = result.Stats;
        var shadows = result.ShadowsTested;
        Dispatcher.UIThread.Post(() => CoverageComputed?.Invoke(stats, shadows));
        return _coverage.Length == triangleCount ? _coverage : null;
    }

    private void FireAlignPick(Avalonia.Point screen)
    {
        var scene = Scene;
        if (scene is null)
            return;
        var w = Bounds.Width;
        var h = Bounds.Height;
        if (!ActiveCamera.TryRay(screen.X, screen.Y, w, h, out var origin, out var dir))
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
}
