using Avalonia.Controls;
using Avalonia.Controls.Primitives;

namespace VGUI2.Avalonia.Controls;

/// <summary>
/// Port of the reference <c>VGUIInput</c> checkbox variant
/// (<c>vgui.CheckBox()</c>): a 14px inset square that shows a gold check
/// mark when checked. Inherits the Avalonia <see cref="CheckBox"/> API
/// (three-state <see cref="ToggleButton.IsChecked"/>, <c>Command</c>).
/// </summary>
public class VguiCheckBox : CheckBox
{
}

/// <summary>
/// Port of the reference <c>VGUIInput</c> radio variant
/// (<c>vgui.Radio()</c>): a round inset well with a gold bullet when
/// checked. Group radios with <see cref="RadioButton.GroupName"/>.
/// </summary>
public class VguiRadioButton : RadioButton
{
}
