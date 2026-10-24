using Avalonia.Controls;

namespace VGUI2.Avalonia.Demo;

/// <summary>One row of the Keyboard tab's binding table.</summary>
public record KeyBind(string Action, string KeyButton, string Alternate);

public partial class MainWindow : VGUI2.Avalonia.Controls.VguiWindow
{
    public MainWindow()
    {
        InitializeComponent();

        System.Collections.Generic.List<KeyBind> binds =
        [
            new("前进", "Up", "W"),
            new("后退", "Down", "S"),
            new("左转", "Left", "∧"),
            new("右转", "Right", "D"),
            new("左移〈扫射〉", "∧", ""),
            new("右移〈扫射〉", "D", ""),
            new("行走", "Left Shift", ""),
            new("跳跃", "Space", ""),
            new("蹲伏", "Left Ctrl", ""),
            new("上浮", "'", ""),
            new("下潜", "/", ""),
            new("向上看", "PageUp", ""),
            new("向下看", "PageDown", ""),
            new("查看统计", "Tab", ""),
        ];
        KeysTable.ItemsSource = binds;
        KeysTable.SelectedItem = binds[0];
    }
}
