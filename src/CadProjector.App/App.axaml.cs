using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using CadProjector.App.Services;
using CadProjector.App.ViewModels;
using CadProjector.App.Views;

namespace CadProjector.App;

public partial class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
        var prefs = AppPrefs.Load();
        UiLanguage.Apply(string.IsNullOrWhiteSpace(prefs.Language) ? "en-US" : prefs.Language!);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = new MainWindow
            {
                DataContext = new MainViewModel(),
            };
        }

        base.OnFrameworkInitializationCompleted();
    }
}
