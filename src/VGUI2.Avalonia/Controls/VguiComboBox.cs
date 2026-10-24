using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Selection;
using Avalonia.Controls.Templates;
using Avalonia.Input;
using Avalonia.VisualTree;

namespace VGUI2.Avalonia.Controls;

/// <summary>
/// Port of the reference <c>VGUISelect</c>: an inset header showing the
/// selected item text with a <c>›</c> arrow, and a raised dropdown list.
/// The dropdown keeps a single highlight: it starts on the selected item,
/// then tracks the pointer while it moves inside the list, and is cleared
/// once the pointer leaves the list.
/// </summary>
public class VguiComboBox : SelectingItemsControl
{
    public static readonly StyledProperty<string?> PlaceholderTextProperty =
        AvaloniaProperty.Register<VguiComboBox, string?>(nameof(PlaceholderText));

    public static readonly StyledProperty<bool> IsDropDownOpenProperty =
        AvaloniaProperty.Register<VguiComboBox, bool>(nameof(IsDropDownOpen));

    public static readonly StyledProperty<double> MaxDropDownHeightProperty =
        AvaloniaProperty.Register<VguiComboBox, double>(nameof(MaxDropDownHeight), 200.0);

    private Control? _header;
    private ContentPresenter? _selectedItemPresenter;
    private TextBlock? _placeholder;
    private Popup? _popup;
    private VguiComboBoxItem? _highlightedItem;

    static VguiComboBox()
    {
        SelectionModeProperty.OverrideDefaultValue<VguiComboBox>(SelectionMode.Single);
    }

    public VguiComboBox()
    {
        SelectionChanged += OnSelectionChanged;
    }

    /// <inheritdoc />
    // SelectingItemsControl's default container is a bare ContentPresenter;
    // create themed list items instead (the ItemContainerTheme is then
    // applied by ItemsControl.PrepareItemContainer).
    protected override bool NeedsContainerOverride(object? item, int index, out object? recycleKey)
    {
        if (item is VguiComboBoxItem)
        {
            recycleKey = null;
            return false;
        }

        return NeedsContainer<VguiComboBoxItem>(item, out recycleKey);
    }

    /// <inheritdoc />
    protected override Control CreateContainerForItemOverride(object? item, int index, object? recycleKey)
    {
        return new VguiComboBoxItem();
    }

    /// <summary>Text displayed while nothing is selected.</summary>
    public string? PlaceholderText
    {
        get => GetValue(PlaceholderTextProperty);
        set => SetValue(PlaceholderTextProperty, value);
    }

    /// <summary>Gets or sets whether the dropdown list is visible.</summary>
    public bool IsDropDownOpen
    {
        get => GetValue(IsDropDownOpenProperty);
        set => SetValue(IsDropDownOpenProperty, value);
    }

    /// <summary>Maximum height of the popup list.</summary>
    public double MaxDropDownHeight
    {
        get => GetValue(MaxDropDownHeightProperty);
        set => SetValue(MaxDropDownHeightProperty, value);
    }

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);

        if (_header is not null)
        {
            _header.PointerPressed -= OnHeaderPointerPressed;
        }

        _header = e.NameScope.Find<Control>("PART_Header");
        _selectedItemPresenter = e.NameScope.Find<ContentPresenter>("PART_SelectedItemPresenter");
        _placeholder = e.NameScope.Find<TextBlock>("PART_Placeholder");
        _popup = e.NameScope.Find<Popup>("PART_Popup");

        if (_header is not null)
        {
            _header.PointerPressed += OnHeaderPointerPressed;
        }

        UpdateHeaderText();
    }

    private void OnHeaderPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (!e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
        {
            return;
        }

        IsDropDownOpen = !IsDropDownOpen;
        e.Handled = true;
    }

    private void OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        IsDropDownOpen = false;
        UpdateHeaderText();
    }

    /// <inheritdoc />
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == SelectedItemProperty ||
            change.Property == DisplayMemberBindingProperty)
        {
            UpdateHeaderText();
        }

        // Rule 1: when the dropdown opens with the pointer outside the list,
        // the highlight sits on the selected item.
        if (change.Property == IsDropDownOpenProperty && change.GetNewValue<bool>())
        {
            global::Avalonia.Threading.Dispatcher.UIThread.Post(HighlightSelectedItem);
        }
    }

    /// <summary>Highlights the container of <see cref="SelectedItem"/>, clearing any other.</summary>
    private void HighlightSelectedItem()
    {
        if (!IsDropDownOpen)
        {
            return;
        }

        SetHighlight(null);
        if (SelectedIndex < 0 || _popup?.Child is null)
        {
            return;
        }

        var container = _popup.Child.GetVisualDescendants()
            .OfType<VguiComboBoxItem>()
            .FirstOrDefault(c => IndexFromContainer(c) == SelectedIndex);
        SetHighlight(container);
    }

    /// <summary>Moves the (single) highlight to the given container, or clears it with null.</summary>
    private void SetHighlight(VguiComboBoxItem? item)
    {
        if (ReferenceEquals(_highlightedItem, item))
        {
            return;
        }

        _highlightedItem?.SetHighlighted(false);
        _highlightedItem = item;
        _highlightedItem?.SetHighlighted(true);
    }

    internal void HighlightItem(VguiComboBoxItem item) => SetHighlight(item);

    internal void UnhighlightItem(VguiComboBoxItem item)
    {
        if (ReferenceEquals(_highlightedItem, item))
        {
            SetHighlight(null);
        }
    }

    private void UpdateHeaderText()
    {
        if (_selectedItemPresenter is null || _placeholder is null)
        {
            return;
        }

        var selected = SelectedItem;
        _selectedItemPresenter.Content = selected;
        _selectedItemPresenter.IsVisible = selected is not null;
        _placeholder.IsVisible = selected is null;
        _placeholder.Text = PlaceholderText;

        if (DisplayMemberBinding is { } binding)
        {
            _selectedItemPresenter.ContentTemplate = new FuncDataTemplate<object>(
                (_, _) => new TextBlock { [!TextBlock.TextProperty] = binding },
                supportsRecycling: true);
        }
        else
        {
            _selectedItemPresenter.ContentTemplate = null;
        }
    }
}

/// <summary>
/// Item container of <see cref="VguiComboBox"/>: a 24px row. The gold
/// highlight is driven by the combo's single-highlight state (the
/// <c>:highlighted</c> pseudo-class), not by pointer-over or selection, so
/// only one row is ever highlighted.
/// </summary>
public class VguiComboBoxItem : ListBoxItem
{
    static VguiComboBoxItem()
    {
        FocusableProperty.OverrideDefaultValue<VguiComboBoxItem>(false);
    }

    internal void SetHighlighted(bool value) => PseudoClasses.Set(":highlighted", value);

    /// <inheritdoc />
    protected override void OnPointerEntered(PointerEventArgs e)
    {
        base.OnPointerEntered(e);

        // Rule 2: while the pointer is inside the list, the highlight tracks it.
        FindOwner()?.HighlightItem(this);
    }

    /// <inheritdoc />
    protected override void OnPointerExited(PointerEventArgs e)
    {
        base.OnPointerExited(e);

        // Rule 3: leaving the list (or moving onto the scrollbar) clears the
        // highlight entirely.
        FindOwner()?.UnhighlightItem(this);
    }

    /// <inheritdoc />
    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);

        if (!e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
        {
            return;
        }

        // Select explicitly through the logical tree: containers generated
        // inside the dropdown popup may not be reachable through the base
        // ItemsControlFromItemContainer walk.
        var owner = FindOwner();
        if (owner is null)
        {
            return;
        }

        var index = owner.IndexFromContainer(this);
        if (index >= 0)
        {
            owner.SetCurrentValue(SelectingItemsControl.SelectedIndexProperty, index);
            owner.IsDropDownOpen = false;
            e.Handled = true;
        }
    }

    private VguiComboBox? FindOwner()
    {
        for (var node = (global::Avalonia.StyledElement?)this; node is not null; node = node.Parent)
        {
            if (node is VguiComboBox combo)
            {
                return combo;
            }
        }

        return null;
    }
}
