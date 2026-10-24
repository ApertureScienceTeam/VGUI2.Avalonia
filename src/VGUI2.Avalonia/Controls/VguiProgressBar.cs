using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Media;

namespace VGUI2.Avalonia.Controls;

/// <summary>
/// Port of the reference <c>VGUIProgressBar</c>: a row of 8px gold blocks
/// every 12px; a block becomes visible once the percent value reaches it.
/// </summary>
public class VguiProgressBar : RangeBase
{
    static VguiProgressBar()
    {
        MinimumProperty.OverrideDefaultValue<VguiProgressBar>(0.0);
        MaximumProperty.OverrideDefaultValue<VguiProgressBar>(100.0);
    }
}

/// <summary>
/// Template part that draws the segmented fill of <see cref="VguiProgressBar"/>.
/// </summary>
public class VguiProgressFill : Control
{
    public static readonly StyledProperty<double> PercentProperty =
        AvaloniaProperty.Register<VguiProgressFill, double>(nameof(Percent), 0.0);

    public static readonly StyledProperty<IBrush?> FillProperty =
        AvaloniaProperty.Register<VguiProgressFill, IBrush?>(nameof(Fill));

    static VguiProgressFill()
    {
        AffectsRender<VguiProgressFill>(PercentProperty, FillProperty);
    }

    public VguiProgressFill()
    {
        // The closing block may overrun the track's right edge; clip it.
        ClipToBounds = true;
    }

    /// <summary>Fill value from 0 to 100.</summary>
    public double Percent
    {
        get => GetValue(PercentProperty);
        set => SetValue(PercentProperty, value);
    }

    public IBrush? Fill
    {
        get => GetValue(FillProperty);
        set => SetValue(FillProperty, value);
    }

    public override void Render(DrawingContext context)
    {
        var fill = Fill;
        if (fill is null)
        {
            return;
        }

        var width = Bounds.Width;
        var height = Bounds.Height;
        if (width <= 0 || height <= 0)
        {
            return;
        }

        // VGUI2.css/.js: blocks are 8px wide on a 12px period. When the
        // track width leaves a sub-block remainder, one extra block is
        // added and clipped at the track's right edge, so 100% closes the
        // bar without ever stretching a block.
        var count = (int)Math.Ceiling(width / 12.0);
        var percent = Math.Clamp(Percent, 0.0, 100.0);

        for (var i = 0; i < count; i++)
        {
            var blockPercent = Math.Round((i + 1) * 100.0 / count);
            if (blockPercent > percent)
            {
                continue;
            }

            var blockWidth = Math.Min(8.0, width - i * 12.0);
            if (blockWidth <= 0)
            {
                break;
            }

            context.FillRectangle(fill, new Rect(i * 12, 0, blockWidth, height));
        }
    }
}
