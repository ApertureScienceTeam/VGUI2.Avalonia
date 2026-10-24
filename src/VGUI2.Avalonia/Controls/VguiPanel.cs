using Avalonia.Controls;

namespace VGUI2.Avalonia.Controls;

/// <summary>
/// Port of the reference <c>VGUIPanel</c>: the base widget of the VGUI2
/// family — a positioned container that can hold text or a child control.
/// Use inside a <c>Canvas</c> (or any layout panel) and position it with
/// <c>Canvas.Left</c>/<c>Canvas.Top</c>, like <c>setPosition</c> in the
/// reference implementation.
/// </summary>
public class VguiPanel : ContentControl
{
    static VguiPanel()
    {
        FocusableProperty.OverrideDefaultValue<VguiPanel>(false);
    }
}
