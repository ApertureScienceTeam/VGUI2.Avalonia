using Avalonia.Media.Imaging;
using Avalonia.Platform;

namespace VGUI2.Avalonia.Controls;

/// <summary>
/// Bitmap assets extracted from the reference implementation's CSS
/// (steam title icon and the red-X close button).
/// </summary>
public static class VguiImages
{
    private static Bitmap? _steamIcon;
    private static Bitmap? _closeButton;

    /// <summary>The classic 16x16 Steam logo (<c>.vgui-icon-steam</c>), used as the default window title icon.</summary>
    public static Bitmap? SteamIcon => _steamIcon ??= Load("avares://VGUI2.Avalonia/Assets/steam-icon.png");

    /// <summary>The 20x20 dark square with the red X (<c>.vgui-dialog-close-button</c>).</summary>
    public static Bitmap? CloseButton => _closeButton ??= Load("avares://VGUI2.Avalonia/Assets/close-button.png");

    private static Bitmap? Load(string uri)
    {
        try
        {
            using var stream = AssetLoader.Open(new Uri(uri));
            return new Bitmap(stream);
        }
        catch
        {
            return null;
        }
    }
}
