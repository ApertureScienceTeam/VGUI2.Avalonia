using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Controls.Primitives;

namespace VGUI2.Avalonia.Demo;

public record PlayerItem(string Label);

public partial class MainWindow : VGUI2.Avalonia.Controls.VguiWindow
{
    public MainWindow()
    {
        InitializeComponent();

        var players = new System.Collections.Generic.List<PlayerItem>
        {
            new("请选择……"),
        };
        for (var i = 1; i < 33; i++)
        {
            players.Add(new PlayerItem("玩家" + i));
        }
        PlayerSelect.ItemsSource = players;
        PlayerSelect.SelectedIndex = 0;
    }

    private void OnProgressMinus(object? sender, RoutedEventArgs e) =>
        Progress1.SetCurrentValue(RangeBase.ValueProperty, Progress1.Value - 10);

    private void OnProgressPlus(object? sender, RoutedEventArgs e) =>
        Progress1.SetCurrentValue(RangeBase.ValueProperty, Progress1.Value + 10);
}
