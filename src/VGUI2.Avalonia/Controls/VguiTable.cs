using System.Collections;
using System.Collections.Specialized;
using Avalonia;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using HAlignment = Avalonia.Layout.HorizontalAlignment;
using VAlignment = Avalonia.Layout.VerticalAlignment;

namespace VGUI2.Avalonia.Controls;

/// <summary>
/// One grid column of <see cref="VguiTable"/>, mirroring a
/// <c>table.setColumn</c> entry: <c>key</c> names the row value,
/// <c>header</c> is the button label, <c>width</c> the column width.
/// The last column stretches automatically.
/// </summary>
public class VguiTableColumn : AvaloniaObject
{
    public static readonly StyledProperty<string?> KeyProperty =
        AvaloniaProperty.Register<VguiTableColumn, string?>(nameof(Key));

    public static readonly StyledProperty<string?> HeaderProperty =
        AvaloniaProperty.Register<VguiTableColumn, string?>(nameof(Header));

    public static readonly StyledProperty<double> WidthProperty =
        AvaloniaProperty.Register<VguiTableColumn, double>(nameof(Width), 62.0);

    /// <summary>Minimum column width for header and resize clamping (62px in the reference).</summary>
    public static readonly StyledProperty<double> MinWidthProperty =
        AvaloniaProperty.Register<VguiTableColumn, double>(nameof(MinWidth), 62.0);

    public string? Key
    {
        get => GetValue(KeyProperty);
        set => SetValue(KeyProperty, value);
    }

    public string? Header
    {
        get => GetValue(HeaderProperty);
        set => SetValue(HeaderProperty, value);
    }

    public double Width
    {
        get => GetValue(WidthProperty);
        set => SetValue(WidthProperty, value);
    }

    public double MinWidth
    {
        get => GetValue(MinWidthProperty);
        set => SetValue(MinWidthProperty, value);
    }
}

/// <summary>Arguments for <see cref="VguiTable.RowClick"/> and <see cref="VguiTable.RowDoubleClicked"/>.</summary>
public class VguiTableRowEventArgs : EventArgs
{
    public VguiTableRowEventArgs(object? item, int index)
    {
        Item = item;
        Index = index;
    }

    public object? Item { get; }

    public int Index { get; }
}

/// <summary>
/// Visual row container of <see cref="VguiTable"/>. Gets the
/// <c>:selected</c> pseudo-class while its item is the table selection.
/// </summary>
public class VguiTableRow : Border
{
    /// <summary>The source item this row displays.</summary>
    public object? Item { get; internal set; }

    /// <summary>Row index within the table body.</summary>
    public int Index { get; internal set; }

    internal bool Selected
    {
        set => PseudoClasses.Set(":selected", value);
    }
}

/// <summary>
/// Port of the reference <c>VGUITable</c>: button header row with column
/// sort + drag-to-resize, and an 18px-row body with single row selection.
/// The last column always stretches to fill the table.
/// </summary>
public class VguiTable : TemplatedControl
{
    public static readonly StyledProperty<VguiTableColumnCollection> ColumnsProperty =
        AvaloniaProperty.Register<VguiTable, VguiTableColumnCollection>(
            nameof(Columns), defaultValue: new VguiTableColumnCollection());

    public static readonly StyledProperty<IEnumerable?> ItemsSourceProperty =
        AvaloniaProperty.Register<VguiTable, IEnumerable?>(nameof(ItemsSource));

    public static readonly StyledProperty<object?> SelectedItemProperty =
        AvaloniaProperty.Register<VguiTable, object?>(nameof(SelectedItem));

    public static readonly StyledProperty<double> HeadHeightProperty =
        AvaloniaProperty.Register<VguiTable, double>(nameof(HeadHeight), 18.0);

    public static readonly StyledProperty<double> RowHeightProperty =
        AvaloniaProperty.Register<VguiTable, double>(nameof(RowHeight), 18.0);

    private Grid? _headerGrid;
    private StackPanel? _rowsHost;
    private Grid? _content;

    private readonly List<(object Item, VguiTableRow Row)> _rows = new();
    private readonly List<ColumnDefinitions> _rowDefinitions = new();
    private ColumnDefinitions? _headerDefinitions;

    private VguiTableColumn? _sortColumn;
    private bool _sortDescending;

    static VguiTable()
    {
        ItemsSourceProperty.Changed.AddClassHandler<VguiTable>((x, e) => x.OnItemsSourceChanged());
        SelectedItemProperty.Changed.AddClassHandler<VguiTable>((x, _) => x.ApplySelection());
    }

    public VguiTable()
    {
        // The StyledProperty default is a shared instance; give each table its own.
        Columns = new VguiTableColumnCollection();
    }

    /// <inheritdoc />
    protected override Size MeasureOverride(Size availableSize)
    {
        // The dialog body hosts a ScrollViewer (CSS .vgui-dialog-body
        // overflow:auto), which measures with unbounded height. The table is
        // a viewport-filling control (CSS width/height calc(100% - 2px)):
        // report a bounded desired size instead of growing to the full row
        // count, so the body never scrolls and stretch sizing fills it.
        var available = availableSize;
        if (double.IsInfinity(available.Height))
        {
            available = new Size(available.Width, HeadHeight + RowHeight * 3);
        }

        return base.MeasureOverride(available);
    }

    /// <summary>Fired when the user clicks a row.</summary>
    public event EventHandler<VguiTableRowEventArgs>? RowClick;

    /// <summary>Fired when the user double-clicks a row.</summary>
    public event EventHandler<VguiTableRowEventArgs>? RowDoubleClicked;

    /// <summary>The grid columns. The last column always stretches.</summary>
    public VguiTableColumnCollection Columns
    {
        get => GetValue(ColumnsProperty);
        set => SetValue(ColumnsProperty, value);
    }

    /// <summary>The row items. Values are read by column <see cref="VguiTableColumn.Key"/>
    /// from properties, fields or <c>IDictionary</c> entries.</summary>
    public IEnumerable? ItemsSource
    {
        get => GetValue(ItemsSourceProperty);
        set => SetValue(ItemsSourceProperty, value);
    }

    /// <summary>The currently selected row item.</summary>
    public object? SelectedItem
    {
        get => GetValue(SelectedItemProperty);
        set => SetValue(SelectedItemProperty, value);
    }

    /// <summary>Header row height in device independent pixels (default 18px, like the reference).</summary>
    public double HeadHeight
    {
        get => GetValue(HeadHeightProperty);
        set => SetValue(HeadHeightProperty, value);
    }

    /// <summary>Body row height in device independent pixels (default 18px, like the reference).</summary>
    public double RowHeight
    {
        get => GetValue(RowHeightProperty);
        set => SetValue(RowHeightProperty, value);
    }

    /// <summary>Removes all rows.</summary>
    public void ClearRows()
    {
        _rows.Clear();
        _rowDefinitions.Clear();
        _rowsHost?.Children.Clear();
        SetSelectedItem(null, updateSelection: false);
    }

    /// <inheritdoc />
    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);

        _headerGrid = e.NameScope.Find<Grid>("PART_HeaderGrid");
        _rowsHost = e.NameScope.Find<StackPanel>("PART_RowsHost");
        _content = e.NameScope.Find<Grid>("PART_Content");

        Rebuild();
    }

    /// <inheritdoc />
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == ColumnsProperty)
        {
            if (change.GetOldValue<VguiTableColumnCollection>() is { } old)
            {
                old.CollectionChanged -= OnColumnsChanged;
            }

            if (Columns is { } current)
            {
                current.CollectionChanged += OnColumnsChanged;
            }

            Rebuild();
        }
        else if (change.Property == RowHeightProperty)
        {
            foreach (var (_, row) in _rows)
            {
                row.Height = RowHeight;
            }
        }
    }

    private void OnColumnsChanged(object? sender, NotifyCollectionChangedEventArgs e) => Rebuild();

    private void OnItemsSourceChanged() => Dispatcher.UIThread.Post(Rebuild);

    // ------------------------------------------------------------------
    // Layout
    // ------------------------------------------------------------------

    private double RowsMinWidth
    {
        get
        {
            var columns = Columns;
            if (columns is null || columns.Count == 0)
            {
                return 0;
            }

            // Fixed columns keep max(min, width); the stretching last column
            // still reserves its minimum.
            var sum = 0.0;
            for (var i = 0; i < columns.Count; i++)
            {
                sum += i == columns.Count - 1
                    ? columns[i].MinWidth
                    : Math.Max(columns[i].MinWidth, columns[i].Width);
            }
            return sum;
        }
    }

    private void Rebuild()
    {
        if (_headerGrid is null || _rowsHost is null || _content is null)
        {
            return;
        }

        RebuildHeader();
        RebuildRows();
        UpdateContentMinWidth();
    }

    private void UpdateContentMinWidth()
    {
        // The 18px vertical scrollbar of the body is always reserved, exactly
        // like the reference layout comment (min-width: cols * 62px + 18px).
        if (_content is not null)
        {
            _content.MinWidth = RowsMinWidth + 18;
        }

        if (_rowsHost is not null)
        {
            _rowsHost.MinWidth = RowsMinWidth;
        }
    }

    private void RebuildHeader()
    {
        _headerGrid!.Children.Clear();
        _headerGrid.ColumnDefinitions.Clear();
        _headerGrid.RowDefinitions.Clear();
        _headerDefinitions = null;

        var columns = Columns;
        if (columns is null || columns.Count == 0)
        {
            return;
        }

        _headerGrid.Height = HeadHeight;

        var defs = new ColumnDefinitions();
        for (var i = 0; i < columns.Count; i++)
        {
            defs.Add(i == columns.Count - 1
                ? new ColumnDefinition(1, GridUnitType.Star)
                : new ColumnDefinition(Math.Max(columns[i].MinWidth, columns[i].Width), GridUnitType.Pixel));
        }

        _headerGrid.ColumnDefinitions = defs;
        _headerDefinitions = defs;

        for (var i = 0; i < columns.Count; i++)
        {
            var column = columns[i];

            var button = new VguiButton
            {
                Content = column.Header,
                FontSize = 12,
                HorizontalContentAlignment = HAlignment.Stretch,
                Margin = new Thickness(0, 1, 0, 1),
            };
            button.Click += (_, _) => OnColumnHeaderClicked(column);
            Grid.SetColumn(button, i);
            _headerGrid.Children.Add(button);

            if (i < columns.Count - 1)
            {
                var resizer = new Border
                {
                    Width = 16,
                    Background = Brushes.Transparent,
                    Cursor = new Cursor(StandardCursorType.SizeWestEast),
                };
                resizer.PointerPressed += (_, e) => BeginColumnResize(column, resizer, e);
                Grid.SetColumn(resizer, i);
                resizer.HorizontalAlignment = HAlignment.Right;
                _headerGrid.Children.Add(resizer);
            }
        }
    }

    private void RebuildRows()
    {
        _rowsHost!.Children.Clear();
        _rows.Clear();
        _rowDefinitions.Clear();

        var columns = Columns;
        if (columns is null || columns.Count == 0 || ItemsSource is null)
        {
            return;
        }

        var index = 0;
        foreach (var item in ItemsSource)
        {
            if (item is null)
            {
                continue;
            }

            var row = BuildRow(item, index, columns);
            _rows.Add((item, row));
            _rowsHost.Children.Add(row);
            index++;
        }

        ApplySelection();
    }

    private VguiTableRow BuildRow(object item, int index, VguiTableColumnCollection columns)
    {
        var defs = new ColumnDefinitions();
        for (var i = 0; i < columns.Count; i++)
        {
            defs.Add(i == columns.Count - 1
                ? new ColumnDefinition(1, GridUnitType.Star)
                : new ColumnDefinition(Math.Max(columns[i].MinWidth, columns[i].Width), GridUnitType.Pixel));
        }

        var grid = new Grid { ColumnDefinitions = defs };
        _rowDefinitions.Add(defs);

        for (var i = 0; i < columns.Count; i++)
        {
            var cell = new TextBlock
            {
                Text = GetCellValue(item, columns[i].Key)?.ToString() ?? string.Empty,
                FontSize = 12,
                TextTrimming = TextTrimming.CharacterEllipsis,
                VerticalAlignment = VAlignment.Center,
                Margin = new Thickness(4, 0, 0, 0),
            };
            Grid.SetColumn(cell, i);
            grid.Children.Add(cell);
        }

        var row = new VguiTableRow
        {
            Item = item,
            Index = index,
            Height = RowHeight,
            Background = Brushes.Transparent,
            Child = grid,
        };

        row.PointerPressed += (_, e) =>
        {
            if (e.GetCurrentPoint(row).Properties.IsLeftButtonPressed)
            {
                SetSelectedItem(item);
                RowClick?.Invoke(this, new VguiTableRowEventArgs(item, row.Index));
            }
        };
        row.DoubleTapped += (_, _) => RowDoubleClicked?.Invoke(this, new VguiTableRowEventArgs(item, row.Index));

        return row;
    }

    // ------------------------------------------------------------------
    // Interaction
    // ------------------------------------------------------------------

    private void SetSelectedItem(object? item, bool updateSelection = true)
    {
        SetCurrentValue(SelectedItemProperty, item);
        if (updateSelection)
        {
            ApplySelection();
        }
    }

    private void ApplySelection()
    {
        var selected = SelectedItem;
        var accent = ResolveAccentBrush();
        foreach (var (item, row) in _rows)
        {
            var isRowSelected = ReferenceEquals(item, selected);
            row.Selected = isRowSelected;

            // .vgui-table-tr-selected — gold row highlight; the transparent
            // background keeps unselected rows hit-testable for clicks. The
            // brush is resolved on the table itself: rows created in code may
            // not reach the theme resources through their own chain.
            row.Background = isRowSelected ? accent : Brushes.Transparent;
        }
    }

    private IBrush? _accentBrush;

    private IBrush? ResolveAccentBrush()
    {
        if (_accentBrush is null &&
            this.TryFindResource("VguiBrushAccent", ActualThemeVariant, out var resource) &&
            resource is IBrush brush)
        {
            _accentBrush = brush;
        }
        return _accentBrush;
    }

    private void OnColumnHeaderClicked(VguiTableColumn column)
    {
        if (_sortColumn == column)
        {
            _sortDescending = !_sortDescending;
        }
        else
        {
            _sortColumn = column;
            _sortDescending = false;
        }

        SortRows();
    }

    private void SortRows()
    {
        var key = _sortColumn?.Key;
        if (key is null || _rows.Count == 0)
        {
            return;
        }

        var numeric = _rows.All(r => double.TryParse(GetCellValue(r.Item, key)?.ToString(), out _));

        var sorted = (numeric
            ? _rows.OrderBy(r => double.TryParse(GetCellValue(r.Item, key)?.ToString(), out var v) ? v : 0)
            : _rows.OrderBy(r => GetCellValue(r.Item, key)?.ToString() ?? string.Empty, StringComparer.Ordinal))
            .ToList();

        if (_sortDescending)
        {
            sorted.Reverse();
        }

        _rows.Clear();
        _rowsHost!.Children.Clear();

        var index = 0;
        foreach (var (item, row) in sorted)
        {
            row.Index = index++;
            _rows.Add((item, row));
            _rowsHost.Children.Add(row);
        }

        ApplySelection();
    }

    private void BeginColumnResize(VguiTableColumn column, Border resizer, PointerPressedEventArgs e)
    {
        var point = e.GetCurrentPoint(resizer);
        if (!point.Properties.IsLeftButtonPressed)
        {
            return;
        }

        var startWidth = column.Width;
        var startPos = e.GetCurrentPoint(null).Position;
        var columnIndex = Columns.IndexOf(column);
        if (columnIndex < 0)
        {
            return;
        }

        e.Pointer.Capture(resizer);
        e.Handled = true;

        void Move(object? s, PointerEventArgs ee)
        {
            var pos = ee.GetCurrentPoint(null).Position;
            var dx = pos.X - startPos.X;
            column.Width = Math.Max(column.MinWidth, Math.Round(startWidth + dx));
            UpdateColumnWidth(columnIndex, column);
        }

        void Release(object? s, PointerReleasedEventArgs ee)
        {
            resizer.PointerMoved -= Move;
            resizer.PointerReleased -= Release;
            ee.Pointer.Capture(null);
        }

        resizer.PointerMoved += Move;
        resizer.PointerReleased += Release;
    }

    private void UpdateColumnWidth(int columnIndex, VguiTableColumn column)
    {
        var width = Math.Max(column.MinWidth, column.Width);

        if (_headerDefinitions is not null && columnIndex < _headerDefinitions.Count - 1)
        {
            _headerDefinitions[columnIndex].Width = new GridLength(width, GridUnitType.Pixel);
        }

        foreach (var defs in _rowDefinitions)
        {
            if (columnIndex < defs.Count - 1)
            {
                defs[columnIndex].Width = new GridLength(width, GridUnitType.Pixel);
            }
        }
    }

    // ------------------------------------------------------------------
    // Value resolution
    // ------------------------------------------------------------------

    private static object? GetCellValue(object item, string? key)
    {
        if (key is null)
        {
            return null;
        }

        if (item is IReadOnlyDictionary<string, object?> readOnly &&
            readOnly.TryGetValue(key, out var value))
        {
            return value;
        }

        if (item is IDictionary dictionary && dictionary.Contains(key))
        {
            return dictionary[key];
        }

        var type = item.GetType();

        if (type.GetProperty(key) is { } property)
        {
            return property.GetValue(item);
        }

        if (type.GetField(key) is { } field)
        {
            return field.GetValue(item);
        }

        return null;
    }
}

/// <summary>Collection of <see cref="VguiTableColumn"/> definitions.</summary>
public class VguiTableColumnCollection : AvaloniaList<VguiTableColumn>
{
}
