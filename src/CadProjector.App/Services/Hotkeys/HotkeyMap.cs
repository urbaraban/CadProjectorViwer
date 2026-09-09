using Avalonia.Input;
using CadProjector.App.Services;

namespace CadProjector.App.Services.Hotkeys;

/// <summary>
/// Remappable chords for scene control. Defaults match the WPF viewer:
/// WASD / numpad 8456 to nudge, Q/E and num 7/9 to page contours.
/// </summary>
public sealed class HotkeyMap
{
    public static IReadOnlyList<HotkeyActionDef> Catalog { get; } =
    [
        new(HotkeyActionId.MoveUp, HotkeyKind.Nudge, "Ui.HotkeyMoveUp", "Move up"),
        new(HotkeyActionId.MoveDown, HotkeyKind.Nudge, "Ui.HotkeyMoveDown", "Move down"),
        new(HotkeyActionId.MoveLeft, HotkeyKind.Nudge, "Ui.HotkeyMoveLeft", "Move left"),
        new(HotkeyActionId.MoveRight, HotkeyKind.Nudge, "Ui.HotkeyMoveRight", "Move right"),
        new(HotkeyActionId.SelectNext, HotkeyKind.Command, "Ui.HotkeySelectNext", "Next contour"),
        new(HotkeyActionId.SelectBack, HotkeyKind.Command, "Ui.HotkeySelectBack", "Previous contour"),
        new(HotkeyActionId.Undo, HotkeyKind.Command, "Ui.Undo", "Undo"),
        new(HotkeyActionId.Redo, HotkeyKind.Command, "Ui.Redo", "Redo"),
        new(HotkeyActionId.Resend, HotkeyKind.Command, "Ui.Resend", "Resend graphics"),
        new(HotkeyActionId.Delete, HotkeyKind.Command, "Ui.HotkeyDelete", "Delete object")
    ];

    private readonly Dictionary<HotkeyActionId, List<KeyChord>> _bindings = [];

    public HotkeyMap() => ResetDefaults();

    public IReadOnlyList<KeyChord> GetBindings(HotkeyActionId action)
        => _bindings.TryGetValue(action, out var list) ? list : [];

    public void ResetDefaults()
    {
        _bindings.Clear();
        _bindings[HotkeyActionId.MoveUp] = [KeyChord.Of(Key.W), KeyChord.Of(Key.NumPad8)];
        _bindings[HotkeyActionId.MoveDown] = [KeyChord.Of(Key.S), KeyChord.Of(Key.NumPad5)];
        _bindings[HotkeyActionId.MoveLeft] = [KeyChord.Of(Key.A), KeyChord.Of(Key.NumPad4)];
        _bindings[HotkeyActionId.MoveRight] = [KeyChord.Of(Key.D), KeyChord.Of(Key.NumPad6)];
        _bindings[HotkeyActionId.SelectNext] = [KeyChord.Of(Key.Q), KeyChord.Of(Key.NumPad7)];
        _bindings[HotkeyActionId.SelectBack] = [KeyChord.Of(Key.E), KeyChord.Of(Key.NumPad9)];
        _bindings[HotkeyActionId.Undo] = [KeyChord.Of(Key.Z, KeyModifiers.Control)];
        _bindings[HotkeyActionId.Redo] =
        [
            KeyChord.Of(Key.Y, KeyModifiers.Control),
            KeyChord.Of(Key.Z, KeyModifiers.Control | KeyModifiers.Shift)
        ];
        _bindings[HotkeyActionId.Resend] = [KeyChord.Of(Key.R, KeyModifiers.Control)];
        _bindings[HotkeyActionId.Delete] = [KeyChord.Of(Key.Delete)];
    }

    public void ResetAction(HotkeyActionId action)
    {
        var fresh = new HotkeyMap();
        _bindings[action] = fresh.GetBindings(action).ToList();
    }

    public bool AddBinding(HotkeyActionId action, KeyChord chord)
    {
        if (chord.IsModifierOnly)
            return false;

        foreach (var list in _bindings.Values)
            list.RemoveAll(c => c == chord);

        if (!_bindings.TryGetValue(action, out var host))
        {
            host = [];
            _bindings[action] = host;
        }

        if (!host.Contains(chord))
            host.Add(chord);
        return true;
    }

    public void RemoveBinding(HotkeyActionId action, KeyChord chord)
    {
        if (!_bindings.TryGetValue(action, out var list))
            return;
        list.Remove(chord);
    }

    public HotkeyActionId? Match(Key key, KeyModifiers modifiers)
    {
        HotkeyActionId? found = null;
        var bestMods = -1;
        foreach (var def in Catalog)
        {
            if (!_bindings.TryGetValue(def.Id, out var list))
                continue;
            foreach (var chord in list)
            {
                if (chord.Key != key)
                    continue;
                if (!MatchesModifiers(def.Kind, chord.Modifiers, modifiers))
                    continue;
                var specificity = BitCount(chord.Modifiers);
                if (specificity < bestMods)
                    continue;
                bestMods = specificity;
                found = def.Id;
            }
        }

        return found;
    }

    public static bool IsNudge(HotkeyActionId action)
        => Catalog.First(d => d.Id == action).Kind == HotkeyKind.Nudge;

    public static HotkeyActionDef Def(HotkeyActionId action)
        => Catalog.First(d => d.Id == action);

    public List<HotkeyPref> ToPrefs()
    {
        var list = new List<HotkeyPref>();
        foreach (var def in Catalog)
        {
            var chords = GetBindings(def.Id);
            if (chords.Count == 0)
            {
                list.Add(new HotkeyPref { Action = def.Id.ToString(), Key = "" });
                continue;
            }

            foreach (var chord in chords)
                list.Add(chord.ToPref(def.Id));
        }

        return list;
    }

    public void LoadPrefs(IReadOnlyList<HotkeyPref>? prefs)
    {
        ResetDefaults();
        if (prefs is null || prefs.Count == 0)
            return;

        var grouped = new Dictionary<HotkeyActionId, List<KeyChord>>();
        foreach (var pref in prefs)
        {
            if (!Enum.TryParse<HotkeyActionId>(pref.Action, ignoreCase: true, out var action))
                continue;
            if (!grouped.ContainsKey(action))
                grouped[action] = [];
            if (KeyChord.TryParse(pref, out _, out var chord) && !chord.IsModifierOnly)
                grouped[action].Add(chord);
        }

        foreach (var (action, list) in grouped)
            _bindings[action] = list;
    }

    private static bool MatchesModifiers(HotkeyKind kind, KeyModifiers required, KeyModifiers actual)
    {
        var compare = actual;
        // Shift is a speed / paging modifier (legacy WASD ×10, SelectNext row jump), not a chord
        // unless the binding itself requires it.
        if ((required & KeyModifiers.Shift) == 0)
            compare &= ~KeyModifiers.Shift;
        if (kind == HotkeyKind.Nudge && (required & KeyModifiers.Control) == 0)
            compare &= ~KeyModifiers.Control;

        return compare == required;
    }

    private static int BitCount(KeyModifiers mods)
    {
        var n = 0;
        var v = (int)mods;
        while (v != 0)
        {
            n += v & 1;
            v >>= 1;
        }

        return n;
    }
}
