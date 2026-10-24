using Avalonia.Controls;

namespace VGUI2.Avalonia.Controls;

/// <summary>
/// Port of the reference <c>VGUITab</c>: a tab strip of raised buttons over
/// a raised content pane; the selected tab grows taller and merges with the
/// pane. Use <see cref="VguiTabItem"/> children, or <see cref="AddTab"/> to
/// mirror the reference <c>tab.addTab(label)</c> API.
/// </summary>
public class VguiTabControl : TabControl
{
    /// <summary>Mirror of the reference <c>addTab(label, buttonWidth)</c>: creates a tab
    /// (optionally with a fixed width, like the reference's per-tab button width) and
    /// returns the item so the caller can fill <c>item.Content</c>.</summary>
    public VguiTabItem AddTab(object? header, double? width = null)
    {
        var item = new VguiTabItem { Header = header };
        if (width is { } w)
        {
            item.Width = w;
        }
        Items.Add(item);
        return item;
    }
}

/// <summary>
/// A tab of <see cref="VguiTabControl"/> — a raised button whose bottom
/// edge is suppressed so the selected tab merges with the content pane.
/// </summary>
public class VguiTabItem : TabItem
{
}
