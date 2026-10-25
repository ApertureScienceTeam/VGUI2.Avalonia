using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.VisualTree;

namespace VGUI2.Avalonia.Controls;

/// <summary>
/// A window-less stand-in for <see cref="VguiWindow"/> that lives inside a
/// <c>Canvas</c>: the same VGUI2 chrome (title bar, close button, optional
/// bottom strip, 5px resize grips), but moving and resizing are done by hand
/// against the <c>Canvas.Left</c> / <c>Canvas.Top</c> attached properties and
/// <see cref="Layoutable.Width"/> / <see cref="Layoutable.Height"/> instead of
/// the window manager. Clicking anywhere raises it above its siblings, like
/// VGUI2's <c>setFocus</c>.
/// </summary>
public class VguiFakeWindow : ContentControl
{
    public static readonly StyledProperty<string?> TitleProperty =
        AvaloniaProperty.Register<VguiFakeWindow, string?>(nameof(Title));

    public static readonly StyledProperty<IImage?> IconProperty =
        AvaloniaProperty.Register<VguiFakeWindow, IImage?>(nameof(Icon));

    public static readonly StyledProperty<object?> BottomContentProperty =
        AvaloniaProperty.Register<VguiFakeWindow, object?>(nameof(BottomContent));

    public static readonly StyledProperty<bool> ShowCloseButtonProperty =
        AvaloniaProperty.Register<VguiFakeWindow, bool>(nameof(ShowCloseButton), true);

    protected override Type StyleKeyOverride => typeof(VguiFakeWindow);

    [Flags]
    private enum DragMode
    {
        None = 0,
        Move = 1,
        North = 2,
        South = 4,
        East = 8,
        West = 16,
    }

    private DragMode _mode = DragMode.None;
    private Visual? _reference;
    private Point _startPointer;
    private Rect _startBounds;

    /// <summary>Raised after the close button hides the window.</summary>
    public event EventHandler? Closed;

    /// <summary>Window title shown in the title bar.</summary>
    public string? Title
    {
        get => GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    /// <summary>Optional 16x16 icon in the title bar.</summary>
    public IImage? Icon
    {
        get => GetValue(IconProperty);
        set => SetValue(IconProperty, value);
    }

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

    public VguiFakeWindow()
    {
        // Title-bar drags and inner controls (TextBox…) mark PointerPressed
        // handled, which stops it bubbling to the class handler — listen with
        // handledEventsToo so every press inside the window raises it.
        AddHandler(PointerPressedEvent, (_, _) => RaiseToFront(),
            RoutingStrategies.Bubble, handledEventsToo: true);
    }

    /// <summary>Hides the window (the counterpart of <c>Window.Close</c> for a Canvas child).</summary>
    public void Close()
    {
        IsVisible = false;
        Closed?.Invoke(this, EventArgs.Empty);
    }

    /// <inheritdoc />
    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);

        if (e.NameScope.Find<Border>("PART_TitleBar") is { } titleBar)
        {
            titleBar.PointerPressed += (_, args) =>
            {
                if (args.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
                {
                    BeginDrag(DragMode.Move, args);
                }
            };
        }

        if (e.NameScope.Find<Button>("PART_CloseButton") is { } closeButton)
        {
            closeButton.Click += (_, _) => Close();
        }

        BindResizeGrip(e, "PART_ResizeLt", DragMode.North | DragMode.West);
        BindResizeGrip(e, "PART_ResizeT", DragMode.North);
        BindResizeGrip(e, "PART_ResizeRt", DragMode.North | DragMode.East);
        BindResizeGrip(e, "PART_ResizeL", DragMode.West);
        BindResizeGrip(e, "PART_ResizeR", DragMode.East);
        BindResizeGrip(e, "PART_ResizeLb", DragMode.South | DragMode.West);
        BindResizeGrip(e, "PART_ResizeB", DragMode.South);
        BindResizeGrip(e, "PART_ResizeRb", DragMode.South | DragMode.East);
    }

    /// <summary>Clicking anywhere in the window raises it, like <c>VGUI2.setFocus</c>.</summary>
    protected override void OnPointerMoved(PointerEventArgs e)
    {
        base.OnPointerMoved(e);

        if (_mode == DragMode.None || e.Pointer.Captured != this || _reference is null)
        {
            return;
        }

        var p = e.GetPosition(_reference);
        var dx = p.X - _startPointer.X;
        var dy = p.Y - _startPointer.Y;

        if (_mode == DragMode.Move)
        {
            Canvas.SetLeft(this, _startBounds.X + dx);
            Canvas.SetTop(this, _startBounds.Y + dy);
            return;
        }

        double left = _startBounds.X;
        double top = _startBounds.Y;
        double width = _startBounds.Width;
        double height = _startBounds.Height;

        // MinWidth/MinHeight may be 0 (unset); never collapse past 1px.
        var minW = Math.Max(MinWidth, 1.0);
        var minH = Math.Max(MinHeight, 1.0);

        if (_mode.HasFlag(DragMode.East))
        {
            width = Math.Max(minW, _startBounds.Width + dx);
        }

        if (_mode.HasFlag(DragMode.West))
        {
            width = Math.Max(minW, _startBounds.Width - dx);
            left = _startBounds.Right - width;
        }

        if (_mode.HasFlag(DragMode.South))
        {
            height = Math.Max(minH, _startBounds.Height + dy);
        }

        if (_mode.HasFlag(DragMode.North))
        {
            height = Math.Max(minH, _startBounds.Height - dy);
            top = _startBounds.Bottom - height;
        }

        Width = width;
        Height = height;
        Canvas.SetLeft(this, left);
        Canvas.SetTop(this, top);
    }

    /// <inheritdoc />
    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        base.OnPointerReleased(e);

        if (e.Pointer.Captured == this)
        {
            e.Pointer.Capture(null);
        }

        _mode = DragMode.None;
    }

    /// <inheritdoc />
    protected override void OnPointerCaptureLost(PointerCaptureLostEventArgs e)
    {
        base.OnPointerCaptureLost(e);
        _mode = DragMode.None;
    }

    private void BeginDrag(DragMode mode, PointerPressedEventArgs e)
    {
        // Reference the parent, not the window itself: moving a Canvas child
        // shifts its own coordinate space, the parent's stays fixed.
        if (this.GetVisualParent() is not Visual parent ||
            this.TranslatePoint(default, parent) is not { } topLeft)
        {
            return;
        }

        _mode = mode;
        _reference = parent;
        _startPointer = e.GetPosition(parent);
        _startBounds = new Rect(topLeft, Bounds.Size);
        e.Pointer.Capture(this);
        e.Handled = true;
    }

    private void BindResizeGrip(TemplateAppliedEventArgs e, string name, DragMode mode)
    {
        if (e.NameScope.Find<Control>(name) is { } grip)
        {
            grip.PointerPressed += (_, args) =>
            {
                if (args.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
                {
                    BeginDrag(mode, args);
                }
            };
        }
    }

    private void RaiseToFront()
    {
        if (this.GetVisualParent() is Panel panel)
        {
            // Max over the SIBLINGS: siblings are also at ZIndex 0 initially,
            // and child order beats ZIndex ties, so ">= max" must still raise.
            var max = 0;
            foreach (var child in panel.Children)
            {
                if (child != this)
                {
                    max = Math.Max(max, child.GetValue(Panel.ZIndexProperty));
                }
            }

            if (this.GetValue(Panel.ZIndexProperty) <= max)
            {
                this.SetValue(Panel.ZIndexProperty, max + 1);
                // ZIndex alone only queues a renderer child-recalc that does
                // not reliably flush on the next frame (raise was invisible
                // until something else dirtied the tree); dirtying the panel
                // runs SynchronizeCompositionChildVisuals in the same render
                // pass, so the raise is visible immediately.
                panel.InvalidateVisual();
            }
        }
    }
}
