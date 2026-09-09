namespace CadProjector.App.Services.Hotkeys;

public enum HotkeyActionId
{
    MoveUp,
    MoveDown,
    MoveLeft,
    MoveRight,
    SelectNext,
    SelectBack,
    Undo,
    Redo,
    Resend,
    Delete
}

public enum HotkeyKind
{
    /// <summary>Repeat while held. Shift ×10, Ctrl ×0.1 — not part of the chord unless bound.</summary>
    Nudge,
    Command
}

public sealed record HotkeyActionDef(
    HotkeyActionId Id,
    HotkeyKind Kind,
    string TitleKey,
    string TitleFallback);
