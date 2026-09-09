using Avalonia.Input;
using CadProjector.App.Services;

namespace CadProjector.App.Services.Hotkeys;

public readonly record struct KeyChord(Key Key, KeyModifiers Modifiers)
{
    public static KeyChord Of(Key key, KeyModifiers modifiers = KeyModifiers.None)
        => new(key, modifiers);

    public bool IsModifierOnly => Key is Key.LeftShift or Key.RightShift
        or Key.LeftCtrl or Key.RightCtrl
        or Key.LeftAlt or Key.RightAlt
        or Key.LWin or Key.RWin
        or Key.None;

    public string ToDisplay()
    {
        var key = FormatKey(Key);
        if (Modifiers == KeyModifiers.None)
            return key;

        var parts = new List<string>(4);
        if (Modifiers.HasFlag(KeyModifiers.Control)) parts.Add("Ctrl");
        if (Modifiers.HasFlag(KeyModifiers.Alt)) parts.Add("Alt");
        if (Modifiers.HasFlag(KeyModifiers.Shift)) parts.Add("Shift");
        if (Modifiers.HasFlag(KeyModifiers.Meta)) parts.Add("Win");
        parts.Add(key);
        return string.Join("+", parts);
    }

    public static string FormatKey(Key key) => key switch
    {
        Key.NumPad0 => "Num 0",
        Key.NumPad1 => "Num 1",
        Key.NumPad2 => "Num 2",
        Key.NumPad3 => "Num 3",
        Key.NumPad4 => "Num 4",
        Key.NumPad5 => "Num 5",
        Key.NumPad6 => "Num 6",
        Key.NumPad7 => "Num 7",
        Key.NumPad8 => "Num 8",
        Key.NumPad9 => "Num 9",
        Key.Add => "Num +",
        Key.Subtract => "Num −",
        Key.Multiply => "Num *",
        Key.Divide => "Num /",
        Key.Decimal => "Num .",
        Key.Escape => "Esc",
        Key.Delete => "Del",
        Key.Return => "Enter",
        Key.Up => "↑",
        Key.Down => "↓",
        Key.Left => "←",
        Key.Right => "→",
        _ => key.ToString()
    };

    public HotkeyPref ToPref(HotkeyActionId action) => new()
    {
        Action = action.ToString(),
        Key = Key.ToString(),
        Modifiers = Modifiers.ToString()
    };

    public static bool TryParse(HotkeyPref pref, out HotkeyActionId action, out KeyChord chord)
    {
        action = default;
        chord = default;
        if (!Enum.TryParse(pref.Action, ignoreCase: true, out action))
            return false;
        if (!Enum.TryParse<Key>(pref.Key, ignoreCase: true, out var key) || key == Key.None)
            return false;
        var mods = KeyModifiers.None;
        if (!string.IsNullOrWhiteSpace(pref.Modifiers)
            && !Enum.TryParse(pref.Modifiers, ignoreCase: true, out mods))
            return false;
        chord = new KeyChord(key, mods);
        return true;
    }
}
