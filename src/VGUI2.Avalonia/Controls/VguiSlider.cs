using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;

namespace VGUI2.Avalonia.Controls;

/// <summary>
/// Port of the reference <c>VGUISlider</c>: an inset track with a slim
/// raised thumb, a tick scale below it and min/max captions.
/// </summary>
public class VguiSlider : RangeBase
{
    private Thumb? _thumb;
    private Panel? _trackArea;

    static VguiSlider()
    {
        MinimumProperty.OverrideDefaultValue<VguiSlider>(0.0);
        MaximumProperty.OverrideDefaultValue<VguiSlider>(10.0);
        AffectsArrange<VguiSlider>(MinimumProperty, MaximumProperty, ValueProperty);
        AffectsRender<VguiSlider>(ValueProperty);
    }

    /// <inheritdoc />
    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);

        if (_thumb is not null)
        {
            _thumb.DragDelta -= OnThumbDragDelta;
        }

        if (_trackArea is not null)
        {
            _trackArea.PointerPressed -= OnTrackPointerPressed;
            _trackArea.SizeChanged -= OnSizeChanged;
        }

        _thumb = e.NameScope.Find<Thumb>("PART_Thumb");
        _trackArea = e.NameScope.Find<Panel>("PART_TrackArea");

        if (_thumb is not null)
        {
            _thumb.DragDelta += OnThumbDragDelta;
        }

        if (_trackArea is not null)
        {
            _trackArea.PointerPressed += OnTrackPointerPressed;
            _trackArea.SizeChanged += OnSizeChanged;
        }

        UpdateThumbPosition();
    }

    /// <inheritdoc />
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == ValueProperty ||
            change.Property == MinimumProperty ||
            change.Property == MaximumProperty)
        {
            UpdateThumbPosition();
        }
    }

    private void OnSizeChanged(object? sender, SizeChangedEventArgs e) => UpdateThumbPosition();

    private double UsableTrackWidth => Math.Max(0, (_trackArea?.Bounds.Width ?? 0) - 14 - 6);

    private void UpdateThumbPosition()
    {
        if (_thumb is null || _trackArea is null)
        {
            return;
        }

        var span = Maximum - Minimum;
        var t = span <= 0 ? 0 : Math.Clamp((Value - Minimum) / span, 0.0, 1.0);
        var x = 7 + t * UsableTrackWidth;
        _thumb.Margin = new Thickness(x, 1, 0, 0);
    }

    private void OnThumbDragDelta(object? sender, VectorEventArgs e)
    {
        var usable = UsableTrackWidth;
        if (usable <= 0)
        {
            return;
        }

        var span = Maximum - Minimum;
        var t = span <= 0 ? 0 : Math.Clamp((Value - Minimum) / span + e.Vector.X / usable, 0.0, 1.0);
        SetCurrentValue(ValueProperty, Minimum + t * span);
    }

    private void OnTrackPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (_trackArea is null || _thumb is null || !e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
        {
            return;
        }

        var usable = UsableTrackWidth;
        if (usable <= 0)
        {
            return;
        }

        var x = e.GetPosition(_trackArea).X;
        var t = Math.Clamp((x - 10) / usable, 0.0, 1.0);
        SetCurrentValue(ValueProperty, Minimum + t * (Maximum - Minimum));
        e.Handled = true;
    }
}
