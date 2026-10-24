using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;

namespace VGUI2.Avalonia.Controls;

/// <summary>
/// A flat separator line with the VGUI2 inset look: the top pixel is the
/// dark edge and the bottom pixel the light edge — the same one-pixel
/// pairing as the inset frame — so the line reads as a groove pressed
/// into the surface. The middle stays transparent when the control is
/// given more than 2px of thickness. Renders itself; give it a Width
/// (horizontal) or Height (vertical) and let it stretch along the line.
/// </summary>
public class VguiDivider : Control
{
    public static readonly StyledProperty<Orientation> OrientationProperty =
        AvaloniaProperty.Register<VguiDivider, Orientation>(nameof(Orientation), Orientation.Horizontal);

    public static readonly StyledProperty<IBrush?> EdgeDarkBrushProperty =
        AvaloniaProperty.Register<VguiDivider, IBrush?>(nameof(EdgeDarkBrush));

    public static readonly StyledProperty<IBrush?> EdgeLightBrushProperty =
        AvaloniaProperty.Register<VguiDivider, IBrush?>(nameof(EdgeLightBrush));

    static VguiDivider()
    {
        AffectsRender<VguiDivider>(OrientationProperty, EdgeDarkBrushProperty, EdgeLightBrushProperty);
        AffectsMeasure<VguiDivider>(OrientationProperty);
    }

    public Orientation Orientation
    {
        get => GetValue(OrientationProperty);
        set => SetValue(OrientationProperty, value);
    }

    public IBrush? EdgeDarkBrush
    {
        get => GetValue(EdgeDarkBrushProperty);
        set => SetValue(EdgeDarkBrushProperty, value);
    }

    public IBrush? EdgeLightBrush
    {
        get => GetValue(EdgeLightBrushProperty);
        set => SetValue(EdgeLightBrushProperty, value);
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        return Orientation == Orientation.Horizontal ? new Size(0, 2) : new Size(2, 0);
    }

    public override void Render(DrawingContext context)
    {
        var dark = EdgeDarkBrush;
        var light = EdgeLightBrush;
        var width = Bounds.Width;
        var height = Bounds.Height;

        if (Orientation == Orientation.Horizontal)
        {
            if (width <= 0)
            {
                return;
            }

            if (dark is not null)
            {
                context.FillRectangle(dark, new Rect(0, 0, width, 1));
            }

            if (light is not null && height >= 2)
            {
                context.FillRectangle(light, new Rect(0, height - 1, width, 1));
            }
        }
        else
        {
            if (height <= 0)
            {
                return;
            }

            if (dark is not null)
            {
                context.FillRectangle(dark, new Rect(0, 0, 1, height));
            }

            if (light is not null && width >= 2)
            {
                context.FillRectangle(light, new Rect(width - 1, 0, 1, height));
            }
        }
    }
}
