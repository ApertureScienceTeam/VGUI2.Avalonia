using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Controls.Primitives;
using Avalonia.Headless;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;
using VGUI2.Avalonia.Controls;
using VGUI2.Avalonia.Demo;

namespace VGUI2.Avalonia.RenderTool;

/// <summary>
/// Headless verification tool: captures screenshots ("shots") and runs
/// interaction self-tests ("selftest") against the demo windows.
/// </summary>
public static class Program
{
    [STAThread]
    public static int Main(string[] args)
    {
        var mode = args.Length > 0 ? args[0] : "shots";
        var outDir = args.Length > 1 ? args[1] : AppContext.BaseDirectory;
        Directory.CreateDirectory(outDir);

        var builder = AppBuilder.Configure<App>()
            .UseHeadless(new AvaloniaHeadlessPlatformOptions
            {
                UseHeadlessDrawing = false,
            })
            .UseSkia()
            // Match the demo: route CJK glyphs to SimSun (see Demo/Program.cs).
            .With(new FontManagerOptions
            {
                FontFallbacks = new[]
                {
                    new FontFallback { FontFamily = new FontFamily("SimSun"), UnicodeRange = UnicodeRange.Parse("U+3000-U+303F") },
                    new FontFallback { FontFamily = new FontFamily("SimSun"), UnicodeRange = UnicodeRange.Parse("U+4E00-U+9FFF") },
                    new FontFallback { FontFamily = new FontFamily("SimSun"), UnicodeRange = UnicodeRange.Parse("U+F900-U+FAFF") },
                    new FontFallback { FontFamily = new FontFamily("SimSun"), UnicodeRange = UnicodeRange.Parse("U+FF00-U+FFEF") },
                },
            });

        var lifetime = new ClassicDesktopStyleApplicationLifetime
        {
            Args = args,
            ShutdownMode = ShutdownMode.OnExplicitShutdown,
        };
        builder.SetupWithLifetime(lifetime);

        var main = new MainWindow();
        var table = new TableWindow();

        main.Show();
        table.Show();

        Window? fontProbe = null;
        if (mode == "fontprobe")
        {
            fontProbe = BuildFontProbe();
            fontProbe.Show();
        }

        _ = Dispatcher.UIThread.InvokeAsync(async () =>
        {
            await Task.Delay(300);

            // Diagnostic: which typeface actually serves CJK glyphs for the
            // theme font stack (SimSun bitmap look vs YaHei vector look).
            if (FontManager.Current.TryMatchCharacter('键', FontStyle.Normal, FontWeight.Normal,
                    FontStretch.Normal, new FontFamily("Verdana, SimSun, Microsoft YaHei"), null, out var cjk))
            {
                Console.WriteLine($"[font] CJK fallback for 键: {cjk.FontFamily.Name}");
            }
            else
            {
                Console.WriteLine("[font] TryMatchCharacter found no fallback");
            }

            if (mode == "selftest")
            {
                RunSelfTest(table, main);

                // Verify the segmented fill closes the track at 100%.
                var progress = main.GetVisualDescendants().OfType<VguiProgressBar>().First();
                progress.SetCurrentValue(RangeBase.ValueProperty, 100.0);
                Dispatcher.UIThread.RunJobs();
                Capture(main, Path.Combine(outDir, "progress100.png"));
            }
            else if (mode == "shrink")
            {
                // Verify the dialog body scrolls when the window is smaller
                // than its content (CSS .vgui-dialog-body overflow:auto).
                main.Width = 430;
                main.Height = 280;
                table.Width = 400;
                table.Height = 170;
                await Task.Delay(100);
                Capture(main, Path.Combine(outDir, "main_shrunk.png"));
                Capture(table, Path.Combine(outDir, "table_shrunk.png"));
            }
            else if (mode == "dpi")
            {
                // Simulate high-DPI machines: fractional render scaling rounds
                // layout rects independently and can expose 1px seams.
                foreach (var scaling in new[] { 1.25, 1.5, 1.75, 2.0 })
                {
                    main.SetRenderScaling(scaling);
                    table.SetRenderScaling(scaling);
                    await Task.Delay(100);
                    Capture(main, Path.Combine(outDir, $"main_{scaling:0.00}.png"));
                    Capture(table, Path.Combine(outDir, $"table_{scaling:0.00}.png"));
                }
            }
            else if (mode == "fakewindow")
            {
                // Verify the in-canvas fake window: dragging the title bar
                // moves Canvas.Left/Top, the SE grip resizes via Width/Height.
                var host = new Window { Width = 560, Height = 420 };
                var canvas = new Canvas();
                var fake = new VguiFakeWindow
                {
                    Title = "Fake window",
                    Width = 320,
                    Height = 220,
                };
                Canvas.SetLeft(fake, 30);
                Canvas.SetTop(fake, 20);
                canvas.Children.Add(fake);

                // Second window, added later so it wins on child order —
                // clicking the first one must raise it above anyway.
                var fake2 = new VguiFakeWindow { Title = "Console", Width = 250, Height = 150 };
                Canvas.SetLeft(fake2, 240);
                Canvas.SetTop(fake2, 190);
                canvas.Children.Add(fake2);

                host.Content = canvas;
                host.Show();
                await Task.Delay(200);

                var title = fake.GetVisualDescendants().OfType<Border>()
                    .First(b => b.Name == "PART_TitleBar");
                var from = title.TranslatePoint(new Point(title.Bounds.Width / 2, 15), host) ?? default;
                host.MouseDown(from, MouseButton.Left);
                host.MouseMove(new Point(from.X + 60, from.Y + 40));
                host.MouseUp(new Point(from.X + 60, from.Y + 40), MouseButton.Left);
                await Task.Delay(100);
                Dispatcher.UIThread.RunJobs();
                Console.WriteLine($"[fake] drag: Left = {Canvas.GetLeft(fake)} (expected 90), Top = {Canvas.GetTop(fake)} (expected 60)");
                Console.WriteLine($"[fake] raise: fake Z = {fake.GetValue(Panel.ZIndexProperty)}, fake2 Z = {fake2.GetValue(Panel.ZIndexProperty)} (expected fake > fake2)");

                var grip = fake.GetVisualDescendants().OfType<Border>()
                    .First(b => b.Name == "PART_ResizeRb");
                var g = grip.TranslatePoint(new Point(grip.Bounds.Width / 2, grip.Bounds.Height / 2), host) ?? default;
                host.MouseDown(g, MouseButton.Left);
                host.MouseMove(new Point(g.X + 40, g.Y + 30));
                host.MouseUp(new Point(g.X + 40, g.Y + 30), MouseButton.Left);
                await Task.Delay(100);
                Dispatcher.UIThread.RunJobs();
                Console.WriteLine($"[fake] resize: {fake.Width}x{fake.Height} (expected 360x250)");

                Capture(host, Path.Combine(outDir, "fakewindow.png"));
            }
            else
            {
                Capture(main, Path.Combine(outDir, "main.png"));
                Capture(table, Path.Combine(outDir, "table.png"));
                if (fontProbe is not null)
                {
                    Capture(fontProbe, Path.Combine(outDir, "fontprobe.png"));
                }
            }

            lifetime.Shutdown();
        });

        lifetime.Start(args);
        return 0;
    }

    private static Window BuildFontProbe()
    {
        var stack = new StackPanel { Margin = new Thickness(8, 6) };
        foreach (var family in new[]
                 {
                     "Verdana, SimSun, Microsoft YaHei",
                     "SimSun",
                     "Microsoft YaHei",
                     "Verdana",
                 })
        {
            stack.Children.Add(new TextBlock
            {
                FontFamily = new FontFamily(family),
                FontSize = 14,
                Foreground = Brushes.White,
                Text = $"键盘测试 Multiplayer 10/10  [{family}]",
                Margin = new Thickness(0, 3),
            });
        }

        return new Window
        {
            Width = 420,
            Height = 120,
            Background = new SolidColorBrush(0xFF4C5844u),
            Content = stack,
        };
    }

    private static void RunSelfTest(TableWindow table, Window main)
    {
        var tableControl = table.GetVisualDescendants().OfType<VguiTable>().First();
        var rows = tableControl.GetVisualDescendants().OfType<VguiTableRow>().ToList();
        Console.WriteLine($"rows found: {rows.Count}");

        if (rows.Count < 3)
        {
            return;
        }

        // --- visual path: set SelectedItem programmatically ---
        tableControl.SetCurrentValue(VguiTable.SelectedItemProperty, rows[1].Item);
        var bgAfterSet = rows[1].Background;
        Console.WriteLine($"[visual] SelectedItem = {tableControl.SelectedItem}");
        Console.WriteLine($"[visual] row1 background = {(bgAfterSet is global::Avalonia.Media.ISolidColorBrush sc ? sc.Color.ToString() : bgAfterSet?.GetType().Name ?? "null")}");

        // --- event path: click row 2 (coords relative to the window) ---
        var row = rows[2];
        var point = row.TranslatePoint(new Point(row.Bounds.Width / 2, row.Bounds.Height / 2), table) ?? default;
        Console.WriteLine($"[event] clicking row2 at {point}");
        table.MouseDown(point, MouseButton.Left);
        table.MouseUp(point, MouseButton.Left);
        Thread.Sleep(100);
        Dispatcher.UIThread.RunJobs();

        Console.WriteLine($"[event] SelectedItem = {tableControl.SelectedItem} (expected {row.Item})");
        var bgRow2 = row.Background;
        Console.WriteLine($"[event] row2 background = {(bgRow2 is global::Avalonia.Media.ISolidColorBrush sc2 ? sc2.Color.ToString() : bgRow2?.GetType().Name ?? "null")}");

    }

    private static bool Contains(this string? s, byte r, byte g, byte b) =>
        s is not null && s.Contains($"#{r:X2}{g:X2}{b:X2}", StringComparison.OrdinalIgnoreCase);

    private static void Capture(Window window, string path)
    {
        try
        {
            var frame = window.CaptureRenderedFrame();
            if (frame is null)
            {
                Console.Error.WriteLine($"capture failed: {path}");
                return;
            }

            using (frame)
            {
                frame.Save(path);
            }
            Console.WriteLine($"captured: {path}");
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"capture error for {path}: {ex}");
        }
    }
}

// build seed 1792847407
