using Avalonia.Controls;

namespace CadProjector.App.Views;

public partial class HotkeysWindow : Window
{
    public HotkeysWindow()
    {
        InitializeComponent();
    }

    private void Close_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e) => Close();
}
