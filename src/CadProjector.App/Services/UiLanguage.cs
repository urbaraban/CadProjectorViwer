using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace CadProjector.App.Services;

public static class UiLanguage
{
    private static ResourceDictionary? _current;

    public static string Current { get; private set; } = "en-US";

    public static void Apply(string culture)
    {
        culture = culture is "ru-RU" or "ru" ? "ru-RU" : "en-US";
        var app = Application.Current;
        if (app is null) return;

        var uri = new Uri($"avares://2Cut/Assets/Lang/{culture}.axaml");
        if (AvaloniaXamlLoader.Load(uri) is not ResourceDictionary dict)
            return;

        if (app.Resources is not ResourceDictionary root)
        {
            root = new ResourceDictionary();
            app.Resources = root;
        }

        if (_current is not null)
            root.MergedDictionaries.Remove(_current);

        root.MergedDictionaries.Add(dict);
        _current = dict;
        Current = culture;
    }

    /// <summary>Looks up a localized string outside XAML, for text built in view models.</summary>
    public static string Text(string key, string fallback)
    {
        if (Application.Current?.Resources.TryGetResource(key, null, out var value) == true
            && value is string s)
            return s;
        return fallback;
    }

    public static string Toggle()
    {
        var next = Current.StartsWith("ru", StringComparison.OrdinalIgnoreCase) ? "en-US" : "ru-RU";
        Apply(next);
        return Current;
    }
}
