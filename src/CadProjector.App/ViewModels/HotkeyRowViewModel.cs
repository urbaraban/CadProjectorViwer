using CadProjector.App.Services;
using CadProjector.App.Services.Hotkeys;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace CadProjector.App.ViewModels;

public partial class HotkeyRowViewModel : ObservableObject
{
    private readonly Action _changed;
    private readonly HotkeyMap _map;

    public HotkeyRowViewModel(HotkeyMap map, HotkeyActionDef def, Action changed)
    {
        _map = map;
        _changed = changed;
        Def = def;
    }

    public HotkeyActionDef Def { get; }
    public HotkeyActionId ActionId => Def.Id;
    public string Title => UiLanguage.Text(Def.TitleKey, Def.TitleFallback);
    public string KindHint => Def.Kind == HotkeyKind.Nudge
        ? UiLanguage.Text("Ui.HotkeyNudgeHint", "Hold to repeat. Shift ×10, Ctrl ×0.1")
        : "";

    public bool HasKindHint => Def.Kind == HotkeyKind.Nudge;

    public IReadOnlyList<HotkeyChordItem> ChordItems =>
        _map.GetBindings(ActionId).Select(c => new HotkeyChordItem(c, RemoveChordCommand)).ToList();

    public string BindingsText => ChordItems.Count == 0
        ? UiLanguage.Text("Ui.HotkeyUnbound", "(none)")
        : string.Join("  ·  ", ChordItems.Select(c => c.Display));

    public void Refresh()
    {
        OnPropertyChanged(nameof(Title));
        OnPropertyChanged(nameof(KindHint));
        OnPropertyChanged(nameof(HasKindHint));
        OnPropertyChanged(nameof(BindingsText));
        OnPropertyChanged(nameof(ChordItems));
    }

    [RelayCommand]
    private void RemoveChord(KeyChord chord)
    {
        _map.RemoveBinding(ActionId, chord);
        Refresh();
        _changed();
    }

    [RelayCommand]
    private void Reset()
    {
        _map.ResetAction(ActionId);
        Refresh();
        _changed();
    }
}

public sealed class HotkeyChordItem
{
    public HotkeyChordItem(KeyChord chord, IRelayCommand<KeyChord> remove)
    {
        Chord = chord;
        RemoveCommand = remove;
    }

    public KeyChord Chord { get; }
    public string Display => Chord.ToDisplay();
    public IRelayCommand<KeyChord> RemoveCommand { get; }
}
