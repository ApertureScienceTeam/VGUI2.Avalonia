using System.Runtime.CompilerServices;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Data.Converters;

namespace VGUI2.Avalonia.Controls;

/// <summary>Value converters used by the VGUI2 control templates.</summary>
public static class VguiConverters
{
    /// <summary>
    /// Window.Icon (<see cref="WindowIcon"/>) to an <see cref="IImage"/> for template images.
    /// Falls back to the default Steam title icon when no icon is set,
    /// like the reference dialogs (<c>setIcon('vgui-icon-steam')</c>).
    /// </summary>
    public static readonly IValueConverter WindowIconToBitmap =
        new FuncValueConverter<object?, IImage?>(value => value switch
        {
            Bitmap bitmap => bitmap,
            WindowIcon icon => GetBitmap(icon),
            _ => VguiImages.SteamIcon,
        });

    private static readonly ConditionalWeakTable<WindowIcon, Bitmap> Cache = new();

    private static Bitmap GetBitmap(WindowIcon icon)
    {
        if (Cache.TryGetValue(icon, out var cached))
        {
            return cached;
        }

        using var stream = new MemoryStream();
        icon.Save(stream);
        stream.Position = 0;
        var bitmap = new Bitmap(stream);
        Cache.Add(icon, bitmap);
        return bitmap;
    }
}
