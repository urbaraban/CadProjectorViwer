using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace CadProjector.App.Converters;

/// <summary>True when the bound string equals the converter parameter (for dock RadioButtons).</summary>
public sealed class StringEqualsConverter : IValueConverter
{
    public static readonly StringEqualsConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => string.Equals(value?.ToString(), parameter?.ToString(), StringComparison.Ordinal);

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is true ? parameter?.ToString() : Avalonia.Data.BindingOperations.DoNothing;
}
