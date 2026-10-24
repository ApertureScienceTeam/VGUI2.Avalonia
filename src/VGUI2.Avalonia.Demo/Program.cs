using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Fonts;
using System;

namespace VGUI2.Avalonia.Demo;

internal static class Program
{
    [STAThread]
    public static void Main(string[] args) => BuildAvaloniaApp()
        .StartWithClassicDesktopLifetime(args);

    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            // Avalonia ignores the comma-list families beyond the primary one
            // for per-glyph fallback (it falls to the OS CJK default, YaHei UI),
            // so route CJK to 宋体 explicitly to match the reference render.
            .With(new FontManagerOptions
            {
                FontFallbacks = new[]
                {
                    new FontFallback { FontFamily = new FontFamily("SimSun"), UnicodeRange = UnicodeRange.Parse("U+3000-U+303F") },
                    new FontFallback { FontFamily = new FontFamily("SimSun"), UnicodeRange = UnicodeRange.Parse("U+4E00-U+9FFF") },
                    new FontFallback { FontFamily = new FontFamily("SimSun"), UnicodeRange = UnicodeRange.Parse("U+F900-U+FAFF") },
                    new FontFallback { FontFamily = new FontFamily("SimSun"), UnicodeRange = UnicodeRange.Parse("U+FF00-U+FFEF") },
                    // Machines without SimSun (e.g. EN Windows without the
                    // Chinese supplemental fonts): next CJK-capable system font.
                    new FontFallback { FontFamily = new FontFamily("Microsoft YaHei UI"), UnicodeRange = UnicodeRange.Parse("U+3000-U+303F") },
                    new FontFallback { FontFamily = new FontFamily("Microsoft YaHei UI"), UnicodeRange = UnicodeRange.Parse("U+4E00-U+9FFF") },
                    new FontFallback { FontFamily = new FontFamily("Microsoft YaHei UI"), UnicodeRange = UnicodeRange.Parse("U+F900-U+FAFF") },
                    new FontFallback { FontFamily = new FontFamily("Microsoft YaHei UI"), UnicodeRange = UnicodeRange.Parse("U+FF00-U+FFEF") },
                },
            })
            .LogToTrace();
}

// build seed 1792847432
