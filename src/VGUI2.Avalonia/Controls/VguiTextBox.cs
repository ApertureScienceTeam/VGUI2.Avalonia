using Avalonia.Controls;

namespace VGUI2.Avalonia.Controls;

/// <summary>
/// Port of the reference <c>VGUIInput</c> text variants (<c>vgui.Text()</c>,
/// <c>vgui.Password()</c>, <c>vgui.Number()</c>): a single-line text input
/// sunk into an inset well. Set <see cref="TextBox.PasswordChar"/> for
/// password mode, or wire <see cref="TextBox.Text"/> validation for numeric
/// input.
/// </summary>
public class VguiTextBox : TextBox
{
    static VguiTextBox()
    {
        MinHeightProperty.OverrideDefaultValue<VguiTextBox>(20);
        WatermarkProperty.OverrideDefaultValue<VguiTextBox>(null);
    }
}
