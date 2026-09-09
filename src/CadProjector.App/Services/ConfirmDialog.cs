using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;

namespace CadProjector.App.Services;

public enum ConfirmUnsavedResult
{
    Cancel,
    Discard,
    Save
}

/// <summary>Minimal Yes/No/Cancel style dialog without extra NuGet packages.</summary>
public static class ConfirmDialog
{
    public static async Task<ConfirmUnsavedResult> UnsavedChangesAsync(Window owner, string message)
    {
        var result = ConfirmUnsavedResult.Cancel;
        var dialog = new Window
        {
            Title = UiLanguage.Text("Ui.UnsavedTitle", "Unsaved changes"),
            Width = 420,
            Height = 180,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            CanResize = false,
            Background = new SolidColorBrush(Color.Parse("#1E1E1E"))
        };

        var save = new Button
        {
            Content = UiLanguage.Text("Ui.Save", "Save"),
            MinWidth = 88,
            Margin = new Avalonia.Thickness(0, 0, 8, 0)
        };
        var discard = new Button
        {
            Content = UiLanguage.Text("Ui.Discard", "Don't save"),
            MinWidth = 88,
            Margin = new Avalonia.Thickness(0, 0, 8, 0)
        };
        var cancel = new Button
        {
            Content = UiLanguage.Text("Ui.Cancel", "Cancel"),
            MinWidth = 88
        };

        save.Click += (_, _) =>
        {
            result = ConfirmUnsavedResult.Save;
            dialog.Close();
        };
        discard.Click += (_, _) =>
        {
            result = ConfirmUnsavedResult.Discard;
            dialog.Close();
        };
        cancel.Click += (_, _) =>
        {
            result = ConfirmUnsavedResult.Cancel;
            dialog.Close();
        };

        dialog.Content = new StackPanel
        {
            Margin = new Avalonia.Thickness(16),
            Spacing = 16,
            Children =
            {
                new TextBlock
                {
                    Text = message,
                    TextWrapping = TextWrapping.Wrap,
                    Foreground = Brushes.WhiteSmoke
                },
                new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    HorizontalAlignment = HorizontalAlignment.Right,
                    Children = { save, discard, cancel }
                }
            }
        };

        await dialog.ShowDialog(owner);
        return result;
    }

    public static async Task<bool> OkCancelAsync(Window owner, string title, string message)
    {
        var ok = false;
        var dialog = new Window
        {
            Title = title,
            Width = 440,
            Height = 200,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            CanResize = false,
            Background = new SolidColorBrush(Color.Parse("#1E1E1E"))
        };

        var yes = new Button
        {
            Content = UiLanguage.Text("Ui.ConfirmPlay", "Start laser"),
            MinWidth = 110,
            Margin = new Avalonia.Thickness(0, 0, 8, 0),
            FontWeight = FontWeight.Bold
        };
        var cancel = new Button
        {
            Content = UiLanguage.Text("Ui.Cancel", "Cancel"),
            MinWidth = 88
        };

        yes.Click += (_, _) =>
        {
            ok = true;
            dialog.Close();
        };
        cancel.Click += (_, _) => dialog.Close();

        dialog.Content = new StackPanel
        {
            Margin = new Avalonia.Thickness(16),
            Spacing = 16,
            Children =
            {
                new TextBlock
                {
                    Text = message,
                    TextWrapping = TextWrapping.Wrap,
                    Foreground = Brushes.WhiteSmoke
                },
                new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    HorizontalAlignment = HorizontalAlignment.Right,
                    Children = { yes, cancel }
                }
            }
        };

        await dialog.ShowDialog(owner);
        return ok;
    }
}
