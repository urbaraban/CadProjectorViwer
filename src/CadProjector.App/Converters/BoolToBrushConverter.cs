using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace CadProjector.App.Converters;

/// <summary>
/// Maps a bool to one of two brushes via ConverterParameter "TrueKey|FalseKey"
/// (StaticResource brush keys resolved by the view, or hex fallbacks).
/// Prefer passing brushes as MultiBinding; this converter accepts IBrush parameters
/// when used with a fixed True/False brush pair set on the converter instance.
/// </summary>
public sealed class BoolToBrushConverter : IValueConverter
{
    public IBrush? TrueBrush { get; set; }
    public IBrush? FalseBrush { get; set; }

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var on = value is true;
        if (TrueBrush is not null || FalseBrush is not null)
            return on ? TrueBrush : FalseBrush;

        if (parameter is string s)
        {
            var parts = s.Split('|');
            var hex = on
                ? (parts.Length > 0 ? parts[0] : "#4EA3F5")
                : (parts.Length > 1 ? parts[1] : "#4EA3F5");
            return SolidColorBrush.Parse(hex);
        }

        return on ? Brushes.OrangeRed : Brushes.DodgerBlue;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
