using Avalonia;
using Avalonia.Media;
using Avalonia.Controls;

namespace VGUI2.Avalonia.Controls;

/// <summary>
/// Raised / inset bevel style, mirroring the <c>.vgui-theme</c> and
/// <c>.vgui-theme-inner</c> CSS classes of the reference implementation.
/// </summary>
public enum VguiFrameStyle
{
    /// <summary>Draws no border at all.</summary>
    None,

    /// <summary>Light top/left edges and dark bottom/right edges (<c>.vgui-theme</c>).</summary>
    Raised,

    /// <summary>Dark top/left edges and light bottom/right edges (<c>.vgui-theme-inner</c>).</summary>
    Inset,
}

/// <summary>
/// A decorator that reproduces the classic VGUI2 beveled border:
/// one pixel edges painted from two brushes whose sides are chosen by
/// <see cref="FrameStyle"/>. Replaces Avalonia's plain <see cref="Border"/>
/// border which can only use a single brush for all four sides.
/// </summary>
public class VguiFrame : Decorator
{
    public static readonly StyledProperty<VguiFrameStyle> FrameStyleProperty =
        AvaloniaProperty.Register<VguiFrame, VguiFrameStyle>(nameof(FrameStyle), VguiFrameStyle.Raised);

    public static readonly StyledProperty<IBrush?> BackgroundProperty =
        AvaloniaProperty.Register<VguiFrame, IBrush?>(nameof(Background));

    public static readonly StyledProperty<IBrush?> EdgeLightBrushProperty =
        AvaloniaProperty.Register<VguiFrame, IBrush?>(nameof(EdgeLightBrush));

    public static readonly StyledProperty<IBrush?> EdgeDarkBrushProperty =
        AvaloniaProperty.Register<VguiFrame, IBrush?>(nameof(EdgeDarkBrush));

    /// <summary>
    /// Tab buttons suppress their bottom edge so the active tab visually
    /// merges with the content pane below it (CSS: <c>.vgui-tab-button</c>
    /// has <c>border-bottom: none</c>).
    /// </summary>
    public static readonly StyledProperty<bool> ShowBottomEdgeProperty =
        AvaloniaProperty.Register<VguiFrame, bool>(nameof(ShowBottomEdge), true);

    static VguiFrame()
    {
        AffectsRender<VguiFrame>(FrameStyleProperty, BackgroundProperty,
            EdgeLightBrushProperty, EdgeDarkBrushProperty, ShowBottomEdgeProperty);
        AffectsArrange<VguiFrame>(FrameStyleProperty);
    }

    public VguiFrameStyle FrameStyle
    {
        get => GetValue(FrameStyleProperty);
        set => SetValue(FrameStyleProperty, value);
    }

    public IBrush? Background
    {
        get => GetValue(BackgroundProperty);
        set => SetValue(BackgroundProperty, value);
    }

    public IBrush? EdgeLightBrush
    {
        get => GetValue(EdgeLightBrushProperty);
        set => SetValue(EdgeLightBrushProperty, value);
    }

    public IBrush? EdgeDarkBrush
    {
        get => GetValue(EdgeDarkBrushProperty);
        set => SetValue(EdgeDarkBrushProperty, value);
    }

    public bool ShowBottomEdge
    {
        get => GetValue(ShowBottomEdgeProperty);
        set => SetValue(ShowBottomEdgeProperty, value);
    }

    /// <summary>
    /// Height of the top and left edges; also used as the child inset so
    /// content aligns like the CSS box model of the reference.
    /// </summary>
    protected override Size ArrangeOverride(Size finalSize)
    {
        var inset = FrameStyle == VguiFrameStyle.None ? default : new Thickness(1);
        Child?.Arrange(new Rect(inset.Left, inset.Top,
            Math.Max(0, finalSize.Width - inset.Left - inset.Right),
            Math.Max(0, finalSize.Height - inset.Top - inset.Bottom)));
        return finalSize;
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        if (Child is null)
        {
            return default;
        }

        var inset = FrameStyle == VguiFrameStyle.None ? default : new Thickness(1);
        var childAvailable = new Size(
            Math.Max(0, availableSize.Width - inset.Left - inset.Right),
            double.IsInfinity(availableSize.Height)
                ? double.PositiveInfinity
                : Math.Max(0, availableSize.Height - inset.Top - inset.Bottom));

        Child.Measure(childAvailable);
        var childSize = Child.DesiredSize;
        return new Size(childSize.Width + inset.Left + inset.Right,
            childSize.Height + inset.Top + inset.Bottom);
    }

    public override void Render(DrawingContext context)
    {
        var background = Background;
        var width = Bounds.Width;
        var height = Bounds.Height;

        if (background is not null && width > 0 && height > 0)
        {
            context.FillRectangle(background, new Rect(0, 0, width, height));
        }

        if (FrameStyle == VguiFrameStyle.None || width <= 0 || height <= 0)
        {
            return;
        }

        var light = EdgeLightBrush;
        var dark = EdgeDarkBrush;
        IBrush? top, left, bottom, right;

        if (FrameStyle == VguiFrameStyle.Raised)
        {
            top = left = light;
            bottom = right = dark;
        }
        else
        {
            top = left = dark;
            bottom = right = light;
        }

        if (top is not null)
        {
            context.FillRectangle(top, new Rect(0, 0, width, 1));
        }

        if (left is not null)
        {
            context.FillRectangle(left, new Rect(0, 0, 1, height));
        }

        if (right is not null)
        {
            context.FillRectangle(right, new Rect(width - 1, 0, 1, height));
        }

        if (bottom is not null && ShowBottomEdge)
        {
            context.FillRectangle(bottom, new Rect(0, height - 1, width, 1));
        }
    }
}
