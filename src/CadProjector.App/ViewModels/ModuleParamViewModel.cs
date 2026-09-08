using System.Globalization;
using CadProjector.Core.Devices;
using CadProjector.Rendering.Modules;
using CommunityToolkit.Mvvm.ComponentModel;

namespace CadProjector.App.ViewModels;

/// <summary>One editable parameter of a module, shaped by the descriptor the registry reflected.</summary>
public partial class ModuleParamViewModel : ObservableObject
{
    private readonly DeviceModuleConfig _config;
    private readonly ModuleParamDescriptor _descriptor;
    private readonly Action<DeviceModuleConfig, ModuleState>? _changed;
    private readonly bool _ready;

    public ModuleParamViewModel(
        DeviceModuleConfig config,
        ModuleParamDescriptor descriptor,
        Action<DeviceModuleConfig, ModuleState>? changed)
    {
        _config = config;
        _descriptor = descriptor;
        _changed = changed;
        Choices = descriptor.Choices;

        var raw = config.Params.TryGetValue(descriptor.Key, out var stored) && !string.IsNullOrEmpty(stored)
            ? stored
            : descriptor.DefaultValue;

        NumberValue = ModuleValue.Double(raw, 0);
        BoolValue = ModuleValue.Bool(raw, false);
        TextValue = raw;
        SelectedChoice = FindChoice(raw) ?? Choices.FirstOrDefault();
        _ready = true;
    }

    public string Label => _descriptor.Label;
    public bool IsNumber => _descriptor.Kind is ModuleParamKind.Double or ModuleParamKind.Int;
    public bool IsBool => _descriptor.Kind == ModuleParamKind.Bool;
    public bool IsText => _descriptor.Kind == ModuleParamKind.Text;
    public bool IsChoice => _descriptor.Kind == ModuleParamKind.Choice;
    public IReadOnlyList<ModuleParamChoice> Choices { get; }
    public double Minimum => _descriptor.Min;
    public double Maximum => _descriptor.Max;
    public double Increment => _descriptor.Increment;
    public string Format => _descriptor.Format;

    [ObservableProperty] public partial double NumberValue { get; set; }
    [ObservableProperty] public partial bool BoolValue { get; set; }
    [ObservableProperty] public partial string TextValue { get; set; } = "";
    [ObservableProperty] public partial ModuleParamChoice? SelectedChoice { get; set; }

    partial void OnNumberValueChanged(double value)
    {
        if (!_ready || !IsNumber) return;
        Store(_descriptor.Kind == ModuleParamKind.Int
            ? Math.Round(value).ToString("0", CultureInfo.InvariantCulture)
            : ModuleValue.Format(value));
    }

    partial void OnBoolValueChanged(bool value)
    {
        if (!_ready || !IsBool) return;
        Store(ModuleValue.Format(value));
    }

    partial void OnTextValueChanged(string value)
    {
        if (!_ready || !IsText) return;
        Store(value);
    }

    partial void OnSelectedChoiceChanged(ModuleParamChoice? value)
    {
        if (!_ready || !IsChoice || value is null) return;
        Store(value.Value);
    }

    private ModuleParamChoice? FindChoice(string raw)
    {
        foreach (var c in Choices)
        {
            if (string.Equals(c.Value, raw, StringComparison.OrdinalIgnoreCase)
                || string.Equals(c.Label, raw, StringComparison.OrdinalIgnoreCase))
                return c;
        }
        return null;
    }

    private void Store(string raw)
    {
        var before = _config.CaptureState();
        _config.Params[_descriptor.Key] = raw;
        _changed?.Invoke(_config, before);
    }
}
