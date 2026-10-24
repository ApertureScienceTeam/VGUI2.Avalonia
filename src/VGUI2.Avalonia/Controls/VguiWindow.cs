using System;
using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.Immutable;

namespace VGUI2.Avalonia.Controls;

/// <summary>
/// Port of the reference <c>VGUIDialog</c>: a borderless window with custom
/// VGUI2 chrome — 30px title bar with icon, title and a close button, an
/// optional 30px bottom strip, plus the 5px resize grips around all edges.
/// </summary>
public class VguiWindow : Window
{
    public static readonly StyledProperty<object?> BottomContentProperty =
        AvaloniaProperty.Register<VguiWindow, object?>(nameof(BottomContent));

    public static readonly StyledProperty<bool> ShowCloseButtonProperty =
        AvaloniaProperty.Register<VguiWindow, bool>(nameof(ShowCloseButton), true);

    static VguiWindow()
    {
        WindowDecorationsProperty.OverrideDefaultValue<VguiWindow>(WindowDecorations.None);
        ExtendClientAreaToDecorationsHintProperty.OverrideDefaultValue<VguiWindow>(true);
        BackgroundProperty.OverrideDefaultValue<VguiWindow>(new ImmutableSolidColorBrush(0xFF4C5844u));
        Layoutable.MinWidthProperty.OverrideDefaultValue<VguiWindow>(250.0);
        Layoutable.MinHeightProperty.OverrideDefaultValue<VguiWindow>(114.0);
    }

    // Window pins the implicit style key to typeof(Window); point it back at
    // VguiWindow so the custom chrome ControlTheme is picked up.
    protected override Type StyleKeyOverride => typeof(VguiWindow);

    /// <inheritdoc />
    protected override void OnOpened(EventArgs e)
    {
        base.OnOpened(e);

        // Windows 11 rounds the corners of every top-level window by default;
        // VGUI2 dialogs are square (DWMWA_WINDOW_CORNER_PREFERENCE = 33,
        // DWMWCP_DONOTROUND = 1).
        if (OperatingSystem.IsWindows() &&
            TryGetPlatformHandle()?.Handle is { } hwnd && hwnd != IntPtr.Zero)
        {
            var preference = 1;
            _ = DwmSetWindowAttribute(hwnd, 33, ref preference, sizeof(int));
        }
    }

    [DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attribute, ref int value, int size);

    /// <summary>Content of the optional bottom strip.</summary>
    public object? BottomContent
    {
        get => GetValue(BottomContentProperty);
        set => SetValue(BottomContentProperty, value);
    }

    /// <summary>Whether the title bar shows a close button.</summary>
    public bool ShowCloseButton
    {
        get => GetValue(ShowCloseButtonProperty);
        set => SetValue(ShowCloseButtonProperty, value);
    }

    /// <inheritdoc />
    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);

        if (e.NameScope.Find<Border>("PART_TitleBar") is { } titleBar)
        {
            titleBar.PointerPressed += OnTitleBarPointerPressed;
        }

        if (e.NameScope.Find<Button>("PART_CloseButton") is { } closeButton)
        {
            closeButton.Click += OnCloseButtonClick;
        }

        BindResizeGrip(e, "PART_ResizeLt", WindowEdge.NorthWest);
        BindResizeGrip(e, "PART_ResizeT", WindowEdge.North);
        BindResizeGrip(e, "PART_ResizeRt", WindowEdge.NorthEast);
        BindResizeGrip(e, "PART_ResizeL", WindowEdge.West);
        BindResizeGrip(e, "PART_ResizeR", WindowEdge.East);
        BindResizeGrip(e, "PART_ResizeLb", WindowEdge.SouthWest);
        BindResizeGrip(e, "PART_ResizeB", WindowEdge.South);
        BindResizeGrip(e, "PART_ResizeRb", WindowEdge.SouthEast);
    }

    /// <summary>Clicking anywhere in the dialog raises it, like <c>VGUI2.setFocus</c>.</summary>
    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);
        Activate();
    }

    private void OnTitleBarPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed && e.Source is not Button)
        {
            BeginMoveDrag(e);
            e.Handled = true;
        }
    }

    private void OnCloseButtonClick(object? sender, RoutedEventArgs e) => Close();

    private void BindResizeGrip(TemplateAppliedEventArgs e, string name, WindowEdge edge)
    {
        if (e.NameScope.Find<Control>(name) is { } grip)
        {
            grip.PointerPressed += (_, args) =>
            {
                if (args.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
                {
                    BeginResizeDrag(edge, args);
                    args.Handled = true;
                }
            };
        }
    }
}
