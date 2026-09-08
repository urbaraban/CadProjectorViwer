using System.Collections.ObjectModel;
using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using CadProjector.Core.Devices;
using CadProjector.Core.Project;
using CadProjector.Core.Scene;
using CadProjector.Geometry.Primitives;

namespace CadProjector.App.Controls;

/// <summary>
/// Host panel: nested <see cref="Surface"/> draws the scene; chrome buttons sit outside the plane.
/// </summary>
public sealed class SceneCanvas : Panel
{
    public static readonly StyledProperty<ProjectionScene?> SceneProperty =
        AvaloniaProperty.Register<SceneCanvas, ProjectionScene?>(nameof(Scene));

    public static readonly StyledProperty<ProjectDocument?> ProjectProperty =
        AvaloniaProperty.Register<SceneCanvas, ProjectDocument?>(nameof(Project));

    public static readonly StyledProperty<int> RevisionProperty =
        AvaloniaProperty.Register<SceneCanvas, int>(nameof(Revision));

    public static readonly StyledProperty<ObservableCollection<FovOverlay>?> FovOverlaysProperty =
        AvaloniaProperty.Register<SceneCanvas, ObservableCollection<FovOverlay>?>(nameof(FovOverlays));

    public static readonly StyledProperty<ObservableCollection<ModuleOverlay>?> ModuleOverlaysProperty =
        AvaloniaProperty.Register<SceneCanvas, ObservableCollection<ModuleOverlay>?>(nameof(ModuleOverlays));

    public static readonly StyledProperty<bool> MaskEditableProperty =
        AvaloniaProperty.Register<SceneCanvas, bool>(nameof(MaskEditable));

    public static readonly StyledProperty<string?> MeshOwnerLabelProperty =
        AvaloniaProperty.Register<SceneCanvas, string?>(nameof(MeshOwnerLabel));

    /// <summary>Module id, anchor index and the new position in the module's 0..1 space.</summary>
    public event Action<string, int, Point2>? ModuleAnchorChanged;

    /// <summary>Mask rectangle as it was when the drag began, and as it is now.</summary>
    public event Action<Rect2, Rect2>? MaskBoundsChanged;

    /// <summary>Drawable index, its translation when the drag began, and its translation now.</summary>
    public event Action<int, Point3, Point3>? DrawableMoved;

    /// <summary>Pointer released — the edit history can seal the entry for this gesture.</summary>
    public event Action? GestureEnded;

    private const double ChromeGap = 6;
    private const double ChromeStrip = 30;
    private const double ChromeCorner = 36;

    private readonly Surface _surface;
    private readonly Button _btnRefresh;
    private readonly Button _btnUndo;
    private readonly Button _btnRedo;
    private readonly Button _btnClear;
    private readonly Button _btnCenter;
    private readonly Button _btnTop;
    private readonly Button _btnBottom;
    private readonly Button _btnLeft;
    private readonly Button _btnRight;
    private readonly Button _btnRotate;
    private readonly Button[] _chrome;

    private Rect? _centerRect;
    private Rect? _topRect;
    private Rect? _bottomRect;
    private Rect? _leftRect;
    private Rect? _rightRect;
    private Rect? _rotateRect;
    private Rect? _refreshRect;
    private Rect? _undoRect;
    private Rect? _redoRect;
    private Rect? _clearRect;
    private bool _chromeVisible;

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

    public ObservableCollection<FovOverlay>? FovOverlays
    {
        get => GetValue(FovOverlaysProperty);
        set => SetValue(FovOverlaysProperty, value);
    }

    public ObservableCollection<ModuleOverlay>? ModuleOverlays
    {
        get => GetValue(ModuleOverlaysProperty);
        set => SetValue(ModuleOverlaysProperty, value);
    }

    public bool MaskEditable
    {
        get => GetValue(MaskEditableProperty);
        set => SetValue(MaskEditableProperty, value);
    }

    public string? MeshOwnerLabel
    {
        get => GetValue(MeshOwnerLabelProperty);
        set => SetValue(MeshOwnerLabelProperty, value);
    }

    static SceneCanvas()
    {
        AffectsArrange<SceneCanvas>(SceneProperty, RevisionProperty, FovOverlaysProperty);
    }

    public SceneCanvas()
    {
        ClipToBounds = true;
        Background = Brushes.Transparent;

        _surface = new Surface();
        _surface.ModuleAnchorChanged += (id, i, p) => ModuleAnchorChanged?.Invoke(id, i, p);
        _surface.MaskBoundsChanged += (from, to) => MaskBoundsChanged?.Invoke(from, to);
        _surface.DrawableMoved += (i, from, to) => DrawableMoved?.Invoke(i, from, to);
        _surface.GestureEnded += () => GestureEnded?.Invoke();
        _surface.ViewChanged += ScheduleChrome;

        _btnRefresh = MakeChrome("↻", "PlayCommand", tipKey: "Ui.Resend");
        _btnUndo = MakeChrome("↶", "UndoCommand");
        _btnRedo = MakeChrome("↷", "RedoCommand");
        _btnClear = MakeChrome("✕", "ClearSceneCommand", tipKey: "Ui.Clear");
        _btnClear.Foreground = new SolidColorBrush(Color.FromRgb(230, 100, 100));
        _btnCenter = MakeChrome("CENTI", "SnapObjectCommand", "CM", "Ui.SnapHint");
        _btnTop = MakeChrome("TOP", "SnapObjectCommand", "CT", "Ui.SnapHint");
        _btnBottom = MakeChrome("BOTTOM", "SnapObjectCommand", "CB", "Ui.SnapHint");
        _btnLeft = MakeChrome("LEFT", "SnapObjectCommand", "LM", "Ui.SnapHint");
        _btnRight = MakeChrome("RIGHT", "SnapObjectCommand", "RM", "Ui.SnapHint");
        _btnRotate = MakeChrome("90", "RotateObject90Command", tipKey: "Ui.Rotate90");

        _chrome =
        [
            _btnRefresh, _btnUndo, _btnRedo, _btnClear,
            _btnCenter, _btnTop, _btnBottom, _btnLeft, _btnRight, _btnRotate
        ];

        Children.Add(_surface);
        foreach (var btn in _chrome)
            Children.Add(btn);

        SyncAllToSurface();
        DataContextChanged += (_, _) => BindChromeCommands();
        AttachedToVisualTree += (_, _) => BindChromeCommands();
    }

    private static Button MakeChrome(string content, string commandName, object? parameter = null, string? tipKey = null)
    {
        return new Button
        {
            Content = content,
            Padding = new Thickness(4, 2),
            FontSize = 11,
            MinWidth = ChromeCorner,
            MinHeight = ChromeStrip,
            HorizontalContentAlignment = HorizontalAlignment.Center,
            VerticalContentAlignment = VerticalAlignment.Center,
            Tag = (commandName, parameter, tipKey)
        };
    }

    private void BindChromeCommands()
    {
        var dc = DataContext;
        if (dc is null) return;
        foreach (var btn in _chrome)
        {
            if (btn.Tag is not (string cmdName, var param, var tipKey))
                continue;
            var prop = dc.GetType().GetProperty(cmdName);
            if (prop?.GetValue(dc) is ICommand command)
            {
                btn.Command = command;
                btn.CommandParameter = param;
            }
            if (tipKey is not null &&
                Application.Current?.Resources.TryGetResource(tipKey, null, out var tip) == true &&
                tip is string s)
                ToolTip.SetTip(btn, s);
            else if (cmdName == "UndoCommand" && dc.GetType().GetProperty("UndoHint")?.GetValue(dc) is string uh)
                ToolTip.SetTip(btn, uh);
            else if (cmdName == "RedoCommand" && dc.GetType().GetProperty("RedoHint")?.GetValue(dc) is string rh)
                ToolTip.SetTip(btn, rh);
        }
    }

    /// <summary>Re-fit the view to everything currently on the table.</summary>
    public void ResetView() => _surface.ResetView();

    private void SyncAllToSurface()
    {
        _surface.Scene = Scene;
        _surface.Project = Project;
        _surface.Revision = Revision;
        _surface.FovOverlays = FovOverlays;
        _surface.ModuleOverlays = ModuleOverlays;
        _surface.MaskEditable = MaskEditable;
        _surface.MeshOwnerLabel = MeshOwnerLabel;
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == SceneProperty)
        {
            _surface.Scene = Scene;
            ResetView();
        }
        else if (change.Property == ProjectProperty)
            _surface.Project = Project;
        else if (change.Property == RevisionProperty)
            _surface.Revision = Revision;
        else if (change.Property == FovOverlaysProperty)
            _surface.FovOverlays = FovOverlays;
        else if (change.Property == ModuleOverlaysProperty)
            _surface.ModuleOverlays = ModuleOverlays;
        else if (change.Property == MaskEditableProperty)
            _surface.MaskEditable = MaskEditable;
        else if (change.Property == MeshOwnerLabelProperty)
            _surface.MeshOwnerLabel = MeshOwnerLabel;
    }

    private void ScheduleChrome()
    {
        InvalidateArrange();
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        foreach (var child in Children)
            child.Measure(availableSize);
        return availableSize;
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        _surface.Arrange(new Rect(finalSize));
        UpdateChromeLayout();
        ArrangeChrome();
        return finalSize;
    }

    private void UpdateChromeLayout()
    {
        _chromeVisible = false;
        if (!_surface.TryGetPlaneScreenRect(out var plane))
            return;

        var left = plane.X;
        var right = plane.Right;
        var top = plane.Y;
        var bottom = plane.Bottom;
        var width = Math.Max(1, plane.Width);
        var height = Math.Max(1, plane.Height);

        var strip = ChromeStrip;
        var corner = ChromeCorner;
        var gap = ChromeGap;

        var chromeTop = top - strip - gap;
        var toolbarTop = chromeTop - strip - gap;

        _refreshRect = new Rect(left, toolbarTop, corner, strip);
        _undoRect = new Rect(right - corner * 3 - 4, toolbarTop, corner, strip);
        _redoRect = new Rect(right - corner * 2 - 2, toolbarTop, corner, strip);
        _clearRect = new Rect(right - corner, toolbarTop, corner, strip);

        _centerRect = new Rect(left - corner - gap, chromeTop, corner, strip);
        // TOP / BOTTOM — compact, centered on the plane (not full-width bars).
        var midW = Math.Clamp(width * 0.28, corner * 1.6, 120);
        var midX = left + (width - midW) * 0.5;
        _topRect = new Rect(midX, chromeTop, midW, strip);
        _rotateRect = new Rect(right + gap, chromeTop, corner, strip);

        _leftRect = new Rect(left - corner - gap, top, corner, height);
        _rightRect = new Rect(right + gap, top, corner, height);
        _bottomRect = new Rect(midX, bottom + gap, midW, strip);

        _chromeVisible = true;
    }

    private void ArrangeChrome()
    {
        foreach (var btn in _chrome)
            btn.IsVisible = _chromeVisible;

        if (!_chromeVisible)
        {
            var empty = new Rect();
            foreach (var btn in _chrome)
                btn.Arrange(empty);
            return;
        }

        ArrangeBtn(_btnRefresh, _refreshRect);
        ArrangeBtn(_btnUndo, _undoRect);
        ArrangeBtn(_btnRedo, _redoRect);
        ArrangeBtn(_btnClear, _clearRect);
        ArrangeBtn(_btnCenter, _centerRect);
        ArrangeBtn(_btnTop, _topRect);
        ArrangeBtn(_btnRotate, _rotateRect);
        ArrangeBtn(_btnLeft, _leftRect);
        ArrangeBtn(_btnRight, _rightRect);
        ArrangeBtn(_btnBottom, _bottomRect);
    }

    private static void ArrangeBtn(Control c, Rect? r)
    {
        if (r is null)
        {
            c.Arrange(new Rect());
            return;
        }
        var rect = r.Value;
        var w = Math.Max(20, rect.Width);
        var h = Math.Max(20, rect.Height);
        c.Arrange(new Rect(rect.X, rect.Y, w, h));
    }

    /// <summary>Drawing surface + pan/zoom/drag. Panel for Background hit-test; Drawer paints.</summary>
    private sealed class Surface : Panel
    {
        private enum MaskDragMode { None, Move, TL, TR, BR, BL }

        public static readonly StyledProperty<ProjectionScene?> SceneProperty =
            AvaloniaProperty.Register<Surface, ProjectionScene?>(nameof(Scene));

        public static readonly StyledProperty<ProjectDocument?> ProjectProperty =
            AvaloniaProperty.Register<Surface, ProjectDocument?>(nameof(Project));

        public static readonly StyledProperty<int> RevisionProperty =
            AvaloniaProperty.Register<Surface, int>(nameof(Revision));

        public static readonly StyledProperty<ObservableCollection<FovOverlay>?> FovOverlaysProperty =
            AvaloniaProperty.Register<Surface, ObservableCollection<FovOverlay>?>(nameof(FovOverlays));

        public static readonly StyledProperty<ObservableCollection<ModuleOverlay>?> ModuleOverlaysProperty =
            AvaloniaProperty.Register<Surface, ObservableCollection<ModuleOverlay>?>(nameof(ModuleOverlays));

        public static readonly StyledProperty<bool> MaskEditableProperty =
            AvaloniaProperty.Register<Surface, bool>(nameof(MaskEditable));

        public static readonly StyledProperty<string?> MeshOwnerLabelProperty =
            AvaloniaProperty.Register<Surface, string?>(nameof(MeshOwnerLabel));

        public event Action<string, int, Point2>? ModuleAnchorChanged;
        public event Action<Rect2, Rect2>? MaskBoundsChanged;
        public event Action<int, Point3, Point3>? DrawableMoved;
        public event Action? GestureEnded;
        public event Action? ViewChanged;

        private const double MinZoom = 0.1;
        private const double MaxZoom = 40;

        private ModuleOverlay? _dragOverlay;
        private int _dragAnchor = -1;
        private MaskDragMode _maskMode = MaskDragMode.None;
        private Point2 _maskGrabWorld;
        private Rect2 _maskStart;
        private ViewMap? _lastMap;

        private int _dragDrawable = -1;
        private int _hoverDrawable = -1;
        private Point2 _drawableGrabWorld;
        private Point3 _drawableStart;

        private double _zoom = 1;
        private Avalonia.Point _pan;
        private bool _panning;
        private Avalonia.Point _panGrab;

        /// <summary>
        /// World rectangle the view is fitted to. Deliberately sticky: recomputing it from the current
        /// content every frame would re-centre and rescale the scene while the user drags a mask or a
        /// FOV past the table edge, making everything slide away from the cursor.
        /// </summary>
        private Rect2? _fitWorld;
        private int _fitDrawableCount = -1;
        private readonly Drawer _drawer = new();

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

        public ObservableCollection<FovOverlay>? FovOverlays
        {
            get => GetValue(FovOverlaysProperty);
            set => SetValue(FovOverlaysProperty, value);
        }

        public ObservableCollection<ModuleOverlay>? ModuleOverlays
        {
            get => GetValue(ModuleOverlaysProperty);
            set => SetValue(ModuleOverlaysProperty, value);
        }

        public bool MaskEditable
        {
            get => GetValue(MaskEditableProperty);
            set => SetValue(MaskEditableProperty, value);
        }

        public string? MeshOwnerLabel
        {
            get => GetValue(MeshOwnerLabelProperty);
            set => SetValue(MeshOwnerLabelProperty, value);
        }

        static Surface() { }

        public Surface()
        {
            Focusable = true;
            ClipToBounds = true;
            Background = Brushes.Transparent;
            _drawer.IsHitTestVisible = false;
            _drawer.Host = this;
            Children.Add(_drawer);
        }

        public void ResetView()
        {
            _zoom = 1;
            _pan = default;
            _fitWorld = null;
            RequestPaint();
            ViewChanged?.Invoke();
        }

        public bool TryGetPlaneScreenRect(out Rect rect)
        {
            rect = default;
            if (_lastMap is null || Scene is null)
                return false;

            var tl = _lastMap.WorldToScreen(new Point2(0, Scene.Target.HeightMm));
            var br = _lastMap.WorldToScreen(new Point2(Scene.Target.WidthMm, 0));
            var left = Math.Min(tl.X, br.X);
            var right = Math.Max(tl.X, br.X);
            var top = Math.Min(tl.Y, br.Y);
            var bottom = Math.Max(tl.Y, br.Y);
            rect = new Rect(left, top, Math.Max(1, right - left), Math.Max(1, bottom - top));
            return true;
        }

        protected override Size MeasureOverride(Size availableSize)
        {
            _drawer.Measure(availableSize);
            return availableSize;
        }

        protected override Size ArrangeOverride(Size finalSize)
        {
            UpdateMap(finalSize);
            _drawer.Arrange(new Rect(finalSize));
            return finalSize;
        }

        private void RequestPaint() => _drawer.InvalidateVisual();

        private void RebuildView()
        {
            UpdateMap(Bounds.Size);
            RequestPaint();
            ViewChanged?.Invoke();
        }

        protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
        {
            base.OnPropertyChanged(change);
            if (change.Property == SceneProperty
                || change.Property == ProjectProperty
                || change.Property == RevisionProperty
                || change.Property == FovOverlaysProperty
                || change.Property == ModuleOverlaysProperty
                || change.Property == MaskEditableProperty
                || change.Property == MeshOwnerLabelProperty)
                RequestPaint();
        }

        private void UpdateMap(Size size)
        {
            var scene = Scene;
            if (scene is null || size.Width < 2 || size.Height < 2)
            {
                _lastMap = null;
                return;
            }

            if (_fitWorld is null || scene.Drawables.Count != _fitDrawableCount)
            {
                _fitWorld = GetWorldBounds(scene, FovOverlays);
                _fitDrawableCount = scene.Drawables.Count;
            }

            var world = _fitWorld.Value;
            if (world.Width <= 0 || world.Height <= 0)
            {
                _lastMap = null;
                return;
            }

            _lastMap = ViewMap.Create(new Rect(0, 0, size.Width, size.Height), world, _zoom, _pan);
        }

        protected override void OnPointerWheelChanged(PointerWheelEventArgs e)
        {
            base.OnPointerWheelChanged(e);
            if (_lastMap is null || _fitWorld is null) return;

            var pos = e.GetPosition(this);
            var next = Math.Clamp(_zoom * Math.Pow(1.15, e.Delta.Y), MinZoom, MaxZoom);
            e.Handled = true;
            if (Math.Abs(next - _zoom) < 1e-12)
                return;

            var anchor = _lastMap.ScreenToWorld(pos);
            _zoom = next;
            var moved = ViewMap.Create(Bounds, _fitWorld.Value, _zoom, _pan).WorldToScreen(anchor);
            _pan = new Avalonia.Point(_pan.X + (pos.X - moved.X), _pan.Y + (pos.Y - moved.Y));
            RebuildView();
        }

        protected override void OnPointerPressed(PointerPressedEventArgs e)
        {
            base.OnPointerPressed(e);
            if (_lastMap is null || Scene is null) return;
            var pt = e.GetPosition(this);
            var props = e.GetCurrentPoint(this).Properties;

            if (props.IsMiddleButtonPressed || props.IsRightButtonPressed)
            {
                if (e.ClickCount >= 2)
                    ResetView();
                else
                {
                    _panning = true;
                    _panGrab = pt;
                    SetHover(-1);
                    Cursor = new Cursor(StandardCursorType.SizeAll);
                    e.Pointer.Capture(this);
                }
                e.Handled = true;
                return;
            }

            if (TryHitAnchor(pt, out var overlay, out var anchor))
            {
                _dragOverlay = overlay;
                _dragAnchor = anchor;
                ModuleAnchorChanged?.Invoke(overlay.ModuleId, anchor, overlay.Anchors[anchor].Position);
                e.Pointer.Capture(this);
                e.Handled = true;
                RequestPaint();
                return;
            }

            var maskMode = MaskDragMode.None;
            if (MaskEditable && Scene.Mask.IsEnabled)
                TryHitMask(pt, out maskMode);

            // Corner handles win over objects; "move the whole mask" loses to them, otherwise the mask
            // would swallow every click on the geometry inside it.
            if (maskMode is not MaskDragMode.None and not MaskDragMode.Move)
            {
                StartMaskDrag(maskMode, pt, e);
                return;
            }

            if (TryHitDrawable(pt, out var index))
            {
                var d = Scene.Drawables[index];
                _dragDrawable = index;
                _drawableStart = d.Translation;
                _drawableGrabWorld = _lastMap.ScreenToWorld(pt);
                DrawableMoved?.Invoke(index, _drawableStart, d.Translation);
                e.Pointer.Capture(this);
                e.Handled = true;
                RequestPaint();
                return;
            }

            if (maskMode == MaskDragMode.Move)
                StartMaskDrag(maskMode, pt, e);
        }

        protected override void OnPointerExited(PointerEventArgs e)
        {
            base.OnPointerExited(e);
            SetHover(-1);
        }

        private void UpdateHover(Avalonia.Point pt) =>
            SetHover(TryHitDrawable(pt, out var index) ? index : -1);

        private void SetHover(int index)
        {
            if (_hoverDrawable == index)
                return;
            _hoverDrawable = index;
            Cursor = index >= 0 ? new Cursor(StandardCursorType.Hand) : Cursor.Default;
            RequestPaint();
        }

        private void StartMaskDrag(MaskDragMode mode, Avalonia.Point pt, PointerPressedEventArgs e)
        {
            _maskMode = mode;
            _maskStart = Scene!.Mask.Bounds;
            _maskGrabWorld = _lastMap!.ScreenToWorld(pt);
            e.Pointer.Capture(this);
            e.Handled = true;
            RequestPaint();
        }

        protected override void OnPointerMoved(PointerEventArgs e)
        {
            base.OnPointerMoved(e);
            if (_lastMap is null || Scene is null) return;
            var pt = e.GetPosition(this);

            if (!_panning && _dragOverlay is null && _dragDrawable < 0 && _maskMode == MaskDragMode.None)
                UpdateHover(pt);

            if (_panning)
            {
                _pan = new Avalonia.Point(_pan.X + (pt.X - _panGrab.X), _pan.Y + (pt.Y - _panGrab.Y));
                _panGrab = pt;
                RebuildView();
                e.Handled = true;
                return;
            }

            if (_dragOverlay is not null && _dragAnchor >= 0)
            {
                var unit = _dragOverlay.ToUnit(_lastMap.ScreenToWorld(pt));
                unit = new Point2(
                    Math.Clamp(unit.X, -0.25, 1.25),
                    Math.Clamp(unit.Y, -0.25, 1.25));
                ModuleAnchorChanged?.Invoke(_dragOverlay.ModuleId, _dragAnchor, unit);
                RequestPaint();
                e.Handled = true;
                return;
            }

            if (_dragDrawable >= 0 && _dragDrawable < Scene.Drawables.Count)
            {
                var world = _lastMap.ScreenToWorld(pt);
                var d = Scene.Drawables[_dragDrawable];
                d.Translation = new Point3(
                    _drawableStart.X + (world.X - _drawableGrabWorld.X),
                    _drawableStart.Y + (world.Y - _drawableGrabWorld.Y),
                    _drawableStart.Z);
                DrawableMoved?.Invoke(_dragDrawable, _drawableStart, d.Translation);
                RequestPaint();
                e.Handled = true;
                return;
            }

            if (_maskMode != MaskDragMode.None)
            {
                var world = _lastMap.ScreenToWorld(pt);
                var dx = world.X - _maskGrabWorld.X;
                var dy = world.Y - _maskGrabWorld.Y;
                var b = _maskStart;
                const double min = 10;
                // World Y points up, so "top" is Y + Height. Each case moves the grabbed corner and
                // passes its diagonal opposite unchanged.
                Rect2 next = _maskMode switch
                {
                    MaskDragMode.Move => new Rect2(b.X + dx, b.Y + dy, b.Width, b.Height),
                    MaskDragMode.TL => NormalizeRect(b.X + dx, b.Y + b.Height + dy, b.X + b.Width, b.Y, min),
                    MaskDragMode.TR => NormalizeRect(b.X + b.Width + dx, b.Y + b.Height + dy, b.X, b.Y, min),
                    MaskDragMode.BR => NormalizeRect(b.X + b.Width + dx, b.Y + dy, b.X, b.Y + b.Height, min),
                    MaskDragMode.BL => NormalizeRect(b.X + dx, b.Y + dy, b.X + b.Width, b.Y + b.Height, min),
                    _ => b
                };
                Scene.Mask.Bounds = next;
                MaskBoundsChanged?.Invoke(_maskStart, next);
                RequestPaint();
                e.Handled = true;
            }
        }

        protected override void OnPointerReleased(PointerReleasedEventArgs e)
        {
            base.OnPointerReleased(e);
            if (_panning)
            {
                _panning = false;
                Cursor = Cursor.Default;
            }
            else if (_dragOverlay is null && _maskMode == MaskDragMode.None && _dragDrawable < 0)
                return;

            _dragOverlay = null;
            _dragAnchor = -1;
            _dragDrawable = -1;
            _maskMode = MaskDragMode.None;
            e.Pointer.Capture(null);
            e.Handled = true;
            GestureEnded?.Invoke();
        }

        private static Rect2 NormalizeRect(double x0, double y0, double x1, double y1, double min)
        {
            var left = Math.Min(x0, x1);
            var right = Math.Max(x0, x1);
            var bottom = Math.Min(y0, y1);
            var top = Math.Max(y0, y1);
            if (right - left < min) right = left + min;
            if (top - bottom < min) top = bottom + min;
            return new Rect2(left, bottom, right - left, top - bottom);
        }

        private bool TryHitAnchor(Avalonia.Point screen, out ModuleOverlay overlay, out int anchor)
        {
            overlay = null!;
            anchor = -1;
            if (_lastMap is null || ModuleOverlays is null)
                return false;

            const double hitPx = 10;
            var best = hitPx * hitPx;
            // Later overlays sit on top, and the selected module always wins a tie.
            foreach (var candidate in ModuleOverlays)
            {
                foreach (var a in candidate.Anchors)
                {
                    var m = _lastMap.WorldToScreen(candidate.ToWorld(a.Position));
                    var dx = m.X - screen.X;
                    var dy = m.Y - screen.Y;
                    var d2 = dx * dx + dy * dy;
                    if (d2 > best || (d2 == best && anchor >= 0 && !candidate.IsSelected))
                        continue;
                    best = d2;
                    overlay = candidate;
                    anchor = a.Index;
                }
            }
            return anchor >= 0;
        }

        /// <summary>Nearest visible, unlocked drawable whose outline passes under the pointer.</summary>
        private bool TryHitDrawable(Avalonia.Point screen, out int index)
        {
            index = -1;
            if (_lastMap is null || Scene is null) return false;

            const double hitPx = 6;
            var best = hitPx * hitPx;
            // Topmost first, and ties keep it, matching what the user sees on top.
            for (var i = Scene.Drawables.Count - 1; i >= 0; i--)
            {
                var d = Scene.Drawables[i];
                if (!d.IsVisible || d.IsLocked) continue;

                foreach (var contour in d.Contours)
                {
                    if (contour.Count == 0) continue;
                    if (contour.Count == 1)
                    {
                        var only = _lastMap.WorldToScreen(Transform(contour[0], d));
                        var dd = Squared(screen, only.X, only.Y);
                        if (dd < best) { best = dd; index = i; }
                        continue;
                    }

                    var prev = _lastMap.WorldToScreen(Transform(contour[0], d));
                    for (var k = 1; k < contour.Count; k++)
                    {
                        var next = _lastMap.WorldToScreen(Transform(contour[k], d));
                        var dd = SquaredToSegment(screen, prev, next);
                        if (dd < best) { best = dd; index = i; }
                        prev = next;
                    }
                }
            }
            return index >= 0;
        }

        private static double Squared(Avalonia.Point p, double x, double y)
        {
            var dx = p.X - x;
            var dy = p.Y - y;
            return dx * dx + dy * dy;
        }

        private static double SquaredToSegment(Avalonia.Point p, Point2 a, Point2 b)
        {
            var vx = b.X - a.X;
            var vy = b.Y - a.Y;
            var len2 = vx * vx + vy * vy;
            if (len2 < 1e-12)
                return Squared(p, a.X, a.Y);
            var t = Math.Clamp(((p.X - a.X) * vx + (p.Y - a.Y) * vy) / len2, 0, 1);
            return Squared(p, a.X + vx * t, a.Y + vy * t);
        }

        private bool TryHitMask(Avalonia.Point screen, out MaskDragMode mode)
        {
            mode = MaskDragMode.None;
            if (_lastMap is null || Scene is null) return false;
            var b = Scene.Mask.Bounds;
            var corners = new (MaskDragMode Mode, Point2 P)[]
            {
                (MaskDragMode.TL, new Point2(b.X, b.Y + b.Height)),
                (MaskDragMode.TR, new Point2(b.X + b.Width, b.Y + b.Height)),
                (MaskDragMode.BR, new Point2(b.X + b.Width, b.Y)),
                (MaskDragMode.BL, new Point2(b.X, b.Y))
            };
            const double hitPx = 12;
            var best = hitPx * hitPx;
            foreach (var (m, p) in corners)
            {
                var s = _lastMap.WorldToScreen(p);
                var dx = s.X - screen.X;
                var dy = s.Y - screen.Y;
                var d2 = dx * dx + dy * dy;
                if (d2 <= best)
                {
                    best = d2;
                    mode = m;
                }
            }
            if (mode != MaskDragMode.None) return true;

            var world = _lastMap.ScreenToWorld(screen);
            if (b.Contains(world))
            {
                mode = MaskDragMode.Move;
                return true;
            }
            return false;
        }

        internal void Paint(DrawingContext context)
        {
            var bounds = Bounds;
            context.FillRectangle(new SolidColorBrush(Color.FromRgb(15, 17, 21)), new Rect(bounds.Size));

            var scene = Scene;
            if (scene is null || bounds.Width < 2 || bounds.Height < 2 || _lastMap is null)
                return;

            var planePen = new Pen(new SolidColorBrush(Color.FromRgb(60, 65, 75)), 1);
            DrawRect(context, _lastMap, new Rect2(0, 0, scene.Target.WidthMm, scene.Target.HeightMm), planePen);

            if (scene.Mask.IsEnabled)
            {
                var maskPen = new Pen(new SolidColorBrush(Color.FromArgb(180, 80, 180, 255)), 1.5);
                var fill = new SolidColorBrush(Color.FromArgb(35, 80, 180, 255));
                var r = scene.Mask.Bounds;
                var p0 = _lastMap.WorldToScreen(new Point2(r.X, r.Y));
                var p1 = _lastMap.WorldToScreen(new Point2(r.X + r.Width, r.Y + r.Height));
                context.FillRectangle(fill, new Rect(
                    Math.Min(p0.X, p1.X), Math.Min(p0.Y, p1.Y),
                    Math.Abs(p1.X - p0.X), Math.Abs(p1.Y - p0.Y)));
                DrawRect(context, _lastMap, r, maskPen);

                if (MaskEditable)
                {
                    var handle = new SolidColorBrush(Color.FromRgb(120, 200, 255));
                    foreach (var corner in new[]
                    {
                        new Point2(r.X, r.Y),
                        new Point2(r.X + r.Width, r.Y),
                        new Point2(r.X + r.Width, r.Y + r.Height),
                        new Point2(r.X, r.Y + r.Height)
                    })
                    {
                        var m = _lastMap.WorldToScreen(corner);
                        context.DrawEllipse(handle, null, new Avalonia.Point(m.X, m.Y), 4.5, 4.5);
                    }
                }
            }

            if (FovOverlays is not null)
            {
                foreach (var fov in FovOverlays)
                {
                    var c = Color.FromUInt32(fov.ColorArgb);
                    var fill = new SolidColorBrush(Color.FromArgb(40, c.R, c.G, c.B));
                    var pen = new Pen(new SolidColorBrush(c), 1.5);
                    var r = fov.BoundsMm;
                    var p0 = _lastMap.WorldToScreen(new Point2(r.X, r.Y));
                    var p1 = _lastMap.WorldToScreen(new Point2(r.X + r.Width, r.Y + r.Height));
                    var rect = new Rect(
                        Math.Min(p0.X, p1.X), Math.Min(p0.Y, p1.Y),
                        Math.Abs(p1.X - p0.X), Math.Abs(p1.Y - p0.Y));
                    context.FillRectangle(fill, rect);
                    DrawRect(context, _lastMap, r, pen);
                    context.DrawText(
                        new FormattedText(
                            fov.Name,
                            System.Globalization.CultureInfo.CurrentCulture,
                            FlowDirection.LeftToRight,
                            Typeface.Default,
                            12,
                            new SolidColorBrush(c)),
                        new Avalonia.Point(Math.Min(p0.X, p1.X) + 4, Math.Min(p0.Y, p1.Y) + 4));
                }
            }

            if (ModuleOverlays is not null)
            {
                foreach (var overlay in ModuleOverlays)
                    DrawModuleOverlay(context, _lastMap, overlay);
            }

            var project = Project;
            var solid = project is null ? Colors.OrangeRed : Color.FromUInt32(project.SolidColorArgb);

            for (var i = 0; i < scene.Drawables.Count; i++)
            {
                var d = scene.Drawables[i];
                if (!d.IsVisible) continue;
                var color = project?.ColorMode == LaserColorMode.LayerColor && d.ColorArgb is uint argb
                    ? Color.FromUInt32(argb)
                    : solid;
                var active = i == _dragDrawable || (_dragDrawable < 0 && i == _hoverDrawable);
                var pen = active
                    ? new Pen(new SolidColorBrush(Color.FromRgb(79, 168, 255)), 2.5)
                    : new Pen(new SolidColorBrush(color), 1.25);

                foreach (var contour in d.Contours)
                {
                    if (contour.Count < 2) continue;
                    var geo = new StreamGeometry();
                    using (var ctx = geo.Open())
                    {
                        var first = Transform(contour[0], d);
                        var m0 = _lastMap.WorldToScreen(first);
                        ctx.BeginFigure(new Avalonia.Point(m0.X, m0.Y), false);
                        for (var k = 1; k < contour.Count; k++)
                        {
                            var p = _lastMap.WorldToScreen(Transform(contour[k], d));
                            ctx.LineTo(new Avalonia.Point(p.X, p.Y));
                        }
                    }
                    context.DrawGeometry(null, pen, geo);
                }
            }

            if (!string.IsNullOrWhiteSpace(MeshOwnerLabel))
            {
                context.DrawText(
                    new FormattedText(
                        MeshOwnerLabel,
                        System.Globalization.CultureInfo.CurrentCulture,
                        FlowDirection.LeftToRight,
                        Typeface.Default,
                        13,
                        new SolidColorBrush(Color.FromRgb(255, 200, 60))),
                    new Avalonia.Point(12, bounds.Height - 28));
            }
        }

        private static void DrawModuleOverlay(DrawingContext context, ViewMap map, ModuleOverlay overlay)
        {
            var color = Color.FromUInt32(overlay.ColorArgb);
            var alpha = (byte)(overlay.IsSelected ? 220 : 110);
            var pen = new Pen(
                new SolidColorBrush(Color.FromArgb(alpha, color.R, color.G, color.B)),
                overlay.IsSelected ? 1.4 : 1.0);

            var points = overlay.Geometry.Points;
            for (var i = 1; i < points.Count; i++)
            {
                if (points[i].Blanked)
                    continue;
                var a = map.WorldToScreen(overlay.ToWorld(new Point2(points[i - 1].X, points[i - 1].Y)));
                var b = map.WorldToScreen(overlay.ToWorld(new Point2(points[i].X, points[i].Y)));
                context.DrawLine(pen, new Avalonia.Point(a.X, a.Y), new Avalonia.Point(b.X, b.Y));
            }

            var handle = new SolidColorBrush(Color.FromArgb(alpha, color.R, color.G, color.B));
            var hot = new SolidColorBrush(Color.FromRgb(255, 80, 80));
            foreach (var anchor in overlay.Anchors)
            {
                var m = map.WorldToScreen(overlay.ToWorld(anchor.Position));
                var current = overlay.IsSelected && anchor.Index == overlay.SelectedAnchor;
                var radius = current ? 5.0 : overlay.IsSelected ? 3.5 : 2.5;
                context.DrawEllipse(current ? hot : handle, null, new Avalonia.Point(m.X, m.Y), radius, radius);
            }
        }

        private static void DrawRect(DrawingContext context, ViewMap map, Rect2 r, IPen pen)
        {
            var p0 = map.WorldToScreen(new Point2(r.X, r.Y));
            var p1 = map.WorldToScreen(new Point2(r.X + r.Width, r.Y));
            var p2 = map.WorldToScreen(new Point2(r.X + r.Width, r.Y + r.Height));
            var p3 = map.WorldToScreen(new Point2(r.X, r.Y + r.Height));
            context.DrawLine(pen, new Avalonia.Point(p0.X, p0.Y), new Avalonia.Point(p1.X, p1.Y));
            context.DrawLine(pen, new Avalonia.Point(p1.X, p1.Y), new Avalonia.Point(p2.X, p2.Y));
            context.DrawLine(pen, new Avalonia.Point(p2.X, p2.Y), new Avalonia.Point(p3.X, p3.Y));
            context.DrawLine(pen, new Avalonia.Point(p3.X, p3.Y), new Avalonia.Point(p0.X, p0.Y));
        }

        private static Point2 Transform(Point2 local, Drawable d)
        {
            var s = d.Scale;
            var x = local.X * s;
            var y = local.Y * s;
            if (Math.Abs(d.RotationDeg) > 1e-9)
            {
                var rad = d.RotationDeg * Math.PI / 180.0;
                var c = Math.Cos(rad);
                var sn = Math.Sin(rad);
                var rx = x * c - y * sn;
                var ry = x * sn + y * c;
                x = rx;
                y = ry;
            }
            return new Point2(x + d.Translation.X, y + d.Translation.Y);
        }

        private static Rect2 GetWorldBounds(ProjectionScene scene, IEnumerable<FovOverlay>? fovs)
        {
            double minX = 0, minY = 0, maxX = scene.Target.WidthMm, maxY = scene.Target.HeightMm;
            foreach (var d in scene.Drawables)
            foreach (var c in d.Contours)
            foreach (var p in c)
            {
                var t = Transform(p, d);
                minX = Math.Min(minX, t.X);
                minY = Math.Min(minY, t.Y);
                maxX = Math.Max(maxX, t.X);
                maxY = Math.Max(maxY, t.Y);
            }
            if (scene.Mask.IsEnabled)
            {
                var m = scene.Mask.Bounds;
                minX = Math.Min(minX, m.X);
                minY = Math.Min(minY, m.Y);
                maxX = Math.Max(maxX, m.X + m.Width);
                maxY = Math.Max(maxY, m.Y + m.Height);
            }
            // A projector aimed off the table would otherwise be fitted out of sight.
            foreach (var fov in fovs ?? [])
            {
                var f = fov.BoundsMm;
                minX = Math.Min(minX, f.X);
                minY = Math.Min(minY, f.Y);
                maxX = Math.Max(maxX, f.X + f.Width);
                maxY = Math.Max(maxY, f.Y + f.Height);
            }
            return new Rect2(minX, minY, Math.Max(1, maxX - minX), Math.Max(1, maxY - minY));
        }

        private sealed class Drawer : Control
        {
            public Surface? Host { get; set; }

            public override void Render(DrawingContext context)
            {
                base.Render(context);
                Host?.Paint(context);
            }
        }

        private sealed class ViewMap
        {
            private readonly double _ox, _oy, _scale, _worldX, _worldY, _worldH;

            private ViewMap(double ox, double oy, double scale, double worldX, double worldY, double worldH)
            {
                _ox = ox; _oy = oy; _scale = scale;
                _worldX = worldX; _worldY = worldY; _worldH = worldH;
            }

            public static ViewMap Create(Rect bounds, Rect2 world, double zoom, Avalonia.Point pan)
            {
                const double pad = 80;
                var sx = (bounds.Width - pad * 2) / world.Width;
                var sy = (bounds.Height - pad * 2) / world.Height;
                var scale = Math.Min(sx, sy) * zoom;
                var ox = pad + (bounds.Width - pad * 2 - world.Width * scale) * 0.5 + pan.X;
                var oy = pad + (bounds.Height - pad * 2 - world.Height * scale) * 0.5 + pan.Y;
                return new ViewMap(ox, oy, scale, world.X, world.Y, world.Height);
            }

            public Point2 WorldToScreen(Point2 p) => new(
                _ox + (p.X - _worldX) * _scale,
                _oy + (_worldY + _worldH - p.Y) * _scale);

            public Point2 ScreenToWorld(Avalonia.Point s)
            {
                var x = (s.X - _ox) / _scale + _worldX;
                var y = _worldY + _worldH - (s.Y - _oy) / _scale;
                return new Point2(x, y);
            }
        }
    }
}
