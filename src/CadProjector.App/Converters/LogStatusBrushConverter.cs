using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;
using CadProjector.Logging;

namespace CadProjector.App.Converters;

public sealed class LogStatusBrushConverter : IValueConverter
{
    public static LogStatusBrushConverter Instance { get; } = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value switch
        {
            LogMessageStatus.Good => Brush("#7DCEA0"),
            LogMessageStatus.Info => Brush("#85C1E9"),
            LogMessageStatus.Warning => Brush("#F0B27A"),
            LogMessageStatus.Error => Brush("#F1948A"),
            _ => Brush("#AAAAAA"),
        };

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();

    private static IBrush Brush(string hex) => SolidColorBrush.Parse(hex);
}
