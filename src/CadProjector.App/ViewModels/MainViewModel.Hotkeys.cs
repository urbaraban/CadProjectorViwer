using System.Collections.ObjectModel;
using Avalonia.Input;
using CadProjector.App.Services;
using CadProjector.App.Services.Hotkeys;
using CadProjector.App.Views;
using CadProjector.Core.Devices;
using CadProjector.Core.Editing;
using CadProjector.Core.Scene;
using CadProjector.Geometry.Primitives;
using CadProjector.Rendering;
using CadProjector.Rendering.Modules;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace CadProjector.App.ViewModels;

public partial class MainViewModel
{
    public HotkeyMap Hotkeys { get; } = new();
    public ObservableCollection<HotkeyRowViewModel> HotkeyRows { get; } = [];

    [ObservableProperty] public partial double NudgeStepMm { get; set; } = 1;
    [ObservableProperty] public partial string HotkeyCaptureHint { get; set; } = "";

    public HotkeyActionId? CapturingAction { get; private set; }

    public bool IsCapturingHotkey => CapturingAction is not null;

    private void InitHotkeys()
    {
        HotkeyRows.Clear();
        foreach (var def in HotkeyMap.Catalog)
            HotkeyRows.Add(new HotkeyRowViewModel(Hotkeys, def, PersistHotkeys));
        RefreshHotkeyRows();
    }

    private void LoadHotkeysFromPrefs(AppPrefsState prefs)
    {
        Hotkeys.LoadPrefs(prefs.Hotkeys);
        if (prefs.NudgeStepMm > 0)
            NudgeStepMm = prefs.NudgeStepMm;
        RefreshHotkeyRows();
    }

    private void PersistHotkeys()
    {
        if (_suppressPrefs) return;
        PersistPrefs();
        RefreshHotkeyRows();
    }

    private void RefreshHotkeyRows()
    {
        foreach (var row in HotkeyRows)
            row.Refresh();
        OnPropertyChanged(nameof(HotkeyCaptureHint));
        OnPropertyChanged(nameof(IsCapturingHotkey));
    }

    [RelayCommand]
    private async Task OpenHotkeys()
    {
        CancelHotkeyCapture();
        RefreshHotkeyRows();
        if (HostWindow is null) return;
        var win = new HotkeysWindow { DataContext = this };
        await win.ShowDialog(HostWindow);
        CancelHotkeyCapture();
    }

    [RelayCommand]
    private void ResetHotkeys()
    {
        CancelHotkeyCapture();
        Hotkeys.ResetDefaults();
        NudgeStepMm = 1;
        PersistHotkeys();
        Log(UiLanguage.Text("Ui.HotkeyReset", "Hotkeys restored to defaults"));
    }

    [RelayCommand]
    private void BeginCaptureHotkey(HotkeyActionId action)
    {
        CapturingAction = action;
        var title = UiLanguage.Text(HotkeyMap.Def(action).TitleKey, HotkeyMap.Def(action).TitleFallback);
        HotkeyCaptureHint = string.Format(
            UiLanguage.Text("Ui.HotkeyCapture", "Press a key for «{0}» (Esc to cancel)"),
            title);
        HostWindow?.Focus();
        RefreshHotkeyRows();
    }

    public void CancelHotkeyCapture()
    {
        if (CapturingAction is null) return;
        CapturingAction = null;
        HotkeyCaptureHint = "";
        RefreshHotkeyRows();
    }

    public bool TryCaptureHotkey(Key key, KeyModifiers modifiers)
    {
        if (CapturingAction is not { } action)
            return false;
        var chord = new KeyChord(key, modifiers);
        if (chord.IsModifierOnly)
            return true;
        if (HotkeyMap.IsNudge(action))
            chord = new KeyChord(key, modifiers & ~(KeyModifiers.Shift | KeyModifiers.Control));

        Hotkeys.AddBinding(action, chord);
        CapturingAction = null;
        HotkeyCaptureHint = "";
        PersistHotkeys();
        Log($"{UiLanguage.Text(HotkeyMap.Def(action).TitleKey, HotkeyMap.Def(action).TitleFallback)} → {chord.ToDisplay()}");
        return true;
    }

    public HotkeyActionId? MatchHotkey(Key key, KeyModifiers modifiers)
        => Hotkeys.Match(key, modifiers);

    /// <summary>WASD hold: combine held directions, apply Shift/Ctrl speed, one history merge.</summary>
    public void ApplyHeldNudges(IReadOnlyCollection<HotkeyActionId> held, KeyModifiers modifiers)
    {
        if (held.Count == 0) return;
        var dx = 0.0;
        var dy = 0.0;
        foreach (var action in held)
        {
            switch (action)
            {
                case HotkeyActionId.MoveUp: dy += 1; break;
                case HotkeyActionId.MoveDown: dy -= 1; break;
                case HotkeyActionId.MoveLeft: dx -= 1; break;
                case HotkeyActionId.MoveRight: dx += 1; break;
            }
        }

        if (dx == 0 && dy == 0) return;

        var step = NudgeStepMm <= 0 ? 1 : NudgeStepMm;
        var mult = modifiers.HasFlag(KeyModifiers.Control) ? 0.1
            : modifiers.HasFlag(KeyModifiers.Shift) ? 10
            : 1;
        NudgeSelection(dx * step * mult, dy * step * mult);
    }

    [RelayCommand]
    private void SelectNext() => CycleSelection(forward: true, shift: false);

    [RelayCommand]
    private void SelectBack() => CycleSelection(forward: false, shift: false);

    public void ExecuteHotkey(HotkeyActionId action, KeyModifiers modifiers = KeyModifiers.None)
    {
        var shift = modifiers.HasFlag(KeyModifiers.Shift);
        switch (action)
        {
            case HotkeyActionId.SelectNext: CycleSelection(forward: true, shift); break;
            case HotkeyActionId.SelectBack: CycleSelection(forward: false, shift); break;
            case HotkeyActionId.Undo: if (CanUndo()) Undo(); break;
            case HotkeyActionId.Redo: if (CanRedo()) Redo(); break;
            case HotkeyActionId.Resend:
                BumpCanvas();
                _ = PlayAsync();
                break;
            case HotkeyActionId.Delete:
                DeleteSelectedObjects();
                break;
        }
    }

    public void EndNudgeGesture() => History.Break();

    private enum KeyboardFocusKind { Drawables, Module }

    private KeyboardFocusKind _keyboardFocus = KeyboardFocusKind.Drawables;

    private void FocusKeyboardOnDrawables()
    {
        _keyboardFocus = KeyboardFocusKind.Drawables;
        NotifyTransformTarget();
    }

    private void FocusKeyboardOnModule()
    {
        var switched = _keyboardFocus != KeyboardFocusKind.Module;
        _keyboardFocus = KeyboardFocusKind.Module;
        if (switched)
            Workspace.Reveal(PanelId.Transform);
        NotifyTransformTarget();
    }

    public bool IsEditingModuleAnchor => ShouldEditModule();

    public string TransformTargetLabel
    {
        get
        {
            if (ShouldEditModule() && TryGetSelectedAnchor(out var cfg, out var module, out var current, out _))
            {
                if (cfg.TypeId == ModuleTypes.Mesh && module is CalibrationMeshModule { Grid: { } grid })
                    return string.Format(
                        UiLanguage.Text("Ui.TransformMeshPoint", "Mesh point {0},{1}"),
                        grid.SelectedCol, grid.SelectedRow);
                var tag = string.IsNullOrEmpty(current.Label) ? current.Index.ToString() : current.Label;
                return $"{cfg.DisplayName} [{tag}]";
            }

            return GetFocusedDrawable()?.Name
                   ?? UiLanguage.Text("Ui.TransformNoTarget", "Nothing selected");
        }
    }

    private void NotifyTransformTarget()
    {
        OnPropertyChanged(nameof(IsEditingModuleAnchor));
        OnPropertyChanged(nameof(TransformTargetLabel));
        SyncTransformPanel();
    }

    private bool TryGetSelectedAnchor(
        out DeviceModuleConfig cfg,
        out IRenderableModule module,
        out ModuleAnchor anchor,
        out Rect2 boundsMm)
    {
        cfg = null!;
        module = null!;
        anchor = default;
        boundsMm = default;
        if (!TryGetKeyboardModule(out cfg, out module))
            return false;
        var anchors = module.GetAnchors();
        if (anchors.Count == 0) return false;
        var index = ResolveSelectedAnchor(anchors);
        anchor = anchors[0];
        foreach (var a in anchors)
        {
            if (a.Index != index) continue;
            anchor = a;
            break;
        }

        boundsMm = GetSelectedDeviceBoundsMm();
        return true;
    }

    private Point2 AnchorToWorld(ModuleAnchor anchor, Rect2 bounds) => new(
        bounds.X + anchor.Position.X * bounds.Width,
        bounds.Y + anchor.Position.Y * bounds.Height);

    private Point2 WorldToAnchorUnit(double xMm, double yMm, Rect2 bounds) => new(
        (xMm - bounds.X) / Math.Max(1e-6, bounds.Width),
        (yMm - bounds.Y) / Math.Max(1e-6, bounds.Height));

    /// <summary>Legacy SelectedObjects: every tree-selected drawable, not only the last one.</summary>
    private List<Drawable> GetSelectedDrawables()
    {
        if (_treeSelection.Count > 0)
            return _treeSelection.Select(i => i.Drawable).Distinct().ToList();
        var d = GetFocusedDrawable();
        return d is null ? [] : [d];
    }

    private bool ShouldEditModule()
    {
        if (_keyboardFocus == KeyboardFocusKind.Module && TryGetKeyboardModule(out _, out var module))
            return module.GetAnchors().Count > 0;
        if (GetSelectedDrawables().Count > 0)
            return false;
        return TryGetKeyboardModule(out _, out module) && module.GetAnchors().Count > 0;
    }

    private bool TryGetKeyboardModule(out DeviceModuleConfig cfg, out IRenderableModule module)
    {
        cfg = null!;
        module = null!;
        var device = SelectedProjector ?? Projectors.FirstOrDefault();
        if (device is null) return false;

        if (SelectedModuleItem is { } item
            && ModuleRegistry.Materialize(item.Model) is IRenderableModule selected
            && selected.GetAnchors().Count > 0)
        {
            cfg = item.Model;
            module = selected;
            return true;
        }

        var mesh = ModuleTypes.FindMesh(device.ModuleChain);
        if (mesh is not null
            && ModuleRegistry.Materialize(mesh) is IRenderableModule meshMod
            && meshMod.GetAnchors().Count > 0)
        {
            cfg = mesh;
            module = meshMod;
            return true;
        }

        return false;
    }

    private Rect2 GetSelectedDeviceBoundsMm()
    {
        var device = SelectedProjector ?? Projectors.FirstOrDefault();
        if (device is null || SelectedScene is null)
            return new Rect2(0, 0, 1000, 1000);
        var w = SelectedScene.Target.WidthMm;
        var h = SelectedScene.Target.HeightMm;
        var fov = GeometrySplitter.GetFovNormalized(device, w, h);
        return new Rect2(fov.X * w, fov.Y * h, fov.Width * w, fov.Height * h);
    }

    private void NudgeSelection(double dx, double dy)
    {
        if (Math.Abs(dx) < 1e-12 && Math.Abs(dy) < 1e-12) return;

        if (ShouldEditModule())
        {
            NudgeFocusedAnchor(dx, dy);
            return;
        }

        var targets = GetSelectedDrawables().Where(d => !d.IsLocked).ToList();
        if (targets.Count == 0) return;

        var steps = new List<IEditAction>();
        foreach (var d in targets)
        {
            var from = d.Translation;
            var to = new Point3(from.X + dx, from.Y + dy, from.Z);
            d.Translation = to;
            steps.Add(new ValueEdit<DrawableTransform>(
                d.Name,
                v => v.ApplyTo(d),
                DrawableTransform.Read(d).With(from),
                DrawableTransform.Read(d).With(to)));
        }

        if (steps.Count == 1)
            Record(steps[0], $"drawable:{targets[0].Id}:nudge");
        else
            Record(new CompositeEdit($"{Hist("Move", "Move")} ×{targets.Count}", steps), "nudge:pool");

        SyncTransformPanel();
        BumpCanvas();
    }

    private void NudgeFocusedAnchor(double dxMm, double dyMm)
    {
        if (!TryGetKeyboardModule(out var cfg, out var module))
            return;
        var anchors = module.GetAnchors();
        if (anchors.Count == 0) return;

        var index = ResolveSelectedAnchor(anchors);
        var current = anchors[0];
        foreach (var a in anchors)
        {
            if (a.Index != index) continue;
            current = a;
            break;
        }

        var bounds = GetSelectedDeviceBoundsMm();
        var next = new Point2(
            current.Position.X + dxMm / Math.Max(1e-6, bounds.Width),
            current.Position.Y + dyMm / Math.Max(1e-6, bounds.Height));
        ApplyModuleAnchor(cfg.Id, current.Index, next);
    }

    private int ResolveSelectedAnchor(IReadOnlyList<ModuleAnchor> anchors)
    {
        if (anchors.Count == 0) return -1;
        if (anchors.Any(a => a.Index == _selectedAnchor))
            return _selectedAnchor;
        return anchors[0].Index;
    }

    /// <summary>
    /// Q/E: if a module with handles is in focus, page its anchors (legacy mesh.SelectNext).
    /// Otherwise page exclusive visibility among children of each selected group.
    /// </summary>
    private void CycleSelection(bool forward, bool shift)
    {
        if (ShouldEditModule())
        {
            CycleModuleAnchor(forward, shift);
            return;
        }

        var selected = GetSelectedDrawables();
        if (selected.Count == 0)
        {
            CycleContours(forward, GetFocusedDrawable());
            return;
        }

        foreach (var d in selected)
            CycleContours(forward, d);
    }

    private void CycleModuleAnchor(bool forward, bool shift)
    {
        if (!TryGetKeyboardModule(out var cfg, out var module))
            return;
        var anchors = module.GetAnchors();
        if (anchors.Count == 0) return;

        if (cfg.TypeId == ModuleTypes.Mesh && module is CalibrationMeshModule { Grid: { } grid })
        {
            grid.SelectNext(forward, shift);
            _selectedAnchor = CalibrationMeshModule.AnchorIndex(grid.SelectedCol, grid.SelectedRow, grid.Columns);
            SyncMeshUiFromAnchor(_selectedAnchor);
        }
        else
        {
            var current = anchors.ToList().FindIndex(a => a.Index == _selectedAnchor);
            if (current < 0) current = 0;
            var step = shift ? Math.Max(1, (int)Math.Sqrt(anchors.Count)) : 1;
            var next = (current + (forward ? step : -step) + anchors.Count) % anchors.Count;
            _selectedAnchor = anchors[next].Index;
        }

        if (ModuleItems.FirstOrDefault(i => i.Model.Id == cfg.Id) is { } item
            && !ReferenceEquals(item, SelectedModuleItem))
            SelectedModuleItem = item;

        RefreshModuleOverlays();
        NotifyTransformTarget();
        BumpCanvas();
    }

    private void CycleContours(bool forward, Drawable? focus)
    {
        if (SelectedScene is null) return;
        var pool = ResolveCyclePool(focus, out var exclusive);
        if (pool.Count == 0) return;

        var current = focus is null ? -1 : IndexInPool(pool, focus);
        if (current < 0) current = FirstVisibleIndex(pool);
        if (current < 0) current = 0;

        var next = (current + (forward ? 1 : -1) + pool.Count) % pool.Count;
        if (pool.Count == 1)
        {
            SelectDrawable(pool[0]);
            return;
        }

        if (exclusive)
        {
            var visibleCount = pool.Count(d => d.IsVisible);
            var show = visibleCount <= 1
                ? (visibleCount == 0 ? Math.Max(current, 0) : next)
                : (current < 0 ? 0 : current);
            var before = CaptureVisibility(pool);
            foreach (var d in pool)
                d.IsVisible = false;
            pool[show].IsVisible = true;
            next = show;

            Record(
                new ValueEdit<List<(Guid Id, bool Visible)>>(
                    forward ? Hist("SelectNext", "Next contour") : Hist("SelectBack", "Previous contour"),
                    ApplyVisibility,
                    before,
                    CaptureVisibility(pool)));
            SyncTreeVisibility();
        }

        SelectDrawable(pool[next]);
        BumpCanvas();
    }

    private List<Drawable> ResolveCyclePool(Drawable? focus, out bool exclusive)
    {
        exclusive = false;
        if (SelectedScene is null) return [];

        if (focus is { IsGroup: true } group && group.Children.Count > 0)
        {
            exclusive = true;
            return group.Children;
        }

        var item = focus is null
            ? SelectedObjectItem
            : ObjectItems.SelectMany(r => r.EnumerateSelfAndDescendants())
                .FirstOrDefault(x => x.Drawable.Id == focus.Id);
        if (item?.Parent is { } parent)
        {
            exclusive = true;
            return parent.Drawable.Children;
        }

        if (focus is null
            && SelectedScene.Drawables.Count == 1
            && SelectedScene.Drawables[0] is { IsGroup: true } only
            && only.Children.Count > 0)
        {
            exclusive = true;
            return only.Children;
        }

        return SelectedScene.Drawables;
    }

    private static int IndexInPool(IReadOnlyList<Drawable> pool, Drawable focus)
    {
        for (var i = 0; i < pool.Count; i++)
        {
            if (pool[i].Id == focus.Id)
                return i;
        }

        return -1;
    }

    private static int FirstVisibleIndex(IReadOnlyList<Drawable> pool)
    {
        for (var i = 0; i < pool.Count; i++)
        {
            if (pool[i].IsVisible)
                return i;
        }

        return -1;
    }

    private static List<(Guid Id, bool Visible)> CaptureVisibility(IReadOnlyList<Drawable> pool)
        => pool.Select(d => (d.Id, d.IsVisible)).ToList();

    private void ApplyVisibility(List<(Guid Id, bool Visible)> snap)
    {
        if (SelectedScene is null) return;
        var byId = EnumerateDrawables(SelectedScene.Drawables).ToDictionary(d => d.Id);
        foreach (var (id, visible) in snap)
        {
            if (byId.TryGetValue(id, out var d))
                d.IsVisible = visible;
        }

        SyncTreeVisibility();
        BumpCanvas();
    }

    private void SyncTreeVisibility()
    {
        foreach (var item in ObjectItems.SelectMany(r => r.EnumerateSelfAndDescendants()))
        {
            if (item.IsVisible != item.Drawable.IsVisible)
                item.IsVisible = item.Drawable.IsVisible;
        }
    }

    private void SelectDrawable(Drawable target)
    {
        var match = ObjectItems.SelectMany(r => r.EnumerateSelfAndDescendants())
            .FirstOrDefault(x => x.Drawable.Id == target.Id);
        if (match is null) return;
        _treeSelection = [match];
        SelectedObjectItem = match;
    }

    private static IEnumerable<Drawable> EnumerateDrawables(IEnumerable<Drawable> roots)
    {
        foreach (var d in roots)
        {
            yield return d;
            foreach (var child in EnumerateDrawables(d.Children))
                yield return child;
        }
    }

    partial void OnNudgeStepMmChanged(double value)
    {
        if (_suppressPrefs) return;
        PersistPrefs();
    }
}
