using System.Globalization;

namespace CadProjector.Core.Devices;

/// <summary>Invariant text encoding for module parameter values.</summary>
public static class ModuleValue
{
    public static string Format(object? value) => value switch
    {
        null => "",
        bool b => b ? "true" : "false",
        Enum e => Convert.ToInt32(e).ToString(CultureInfo.InvariantCulture),
        double d => d.ToString("R", CultureInfo.InvariantCulture),
        float f => ((double)f).ToString("R", CultureInfo.InvariantCulture),
        string s => s,
        IFormattable f => f.ToString(null, CultureInfo.InvariantCulture),
        _ => Convert.ToString(value, CultureInfo.InvariantCulture) ?? ""
    };

    public static bool TryDouble(string? text, out double value) =>
        double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out value);

    public static double Double(string? text, double fallback) =>
        TryDouble(text, out var v) ? v : fallback;

    public static bool Bool(string? text, bool fallback) =>
        bool.TryParse(text, out var v) ? v : fallback;
}
