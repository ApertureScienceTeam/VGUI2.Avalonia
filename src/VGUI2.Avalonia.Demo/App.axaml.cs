using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;

namespace VGUI2.Avalonia.Demo;

public class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var main = new MainWindow { Position = new global::Avalonia.PixelPoint(40, 460) };
            var table = new TableWindow { Position = new global::Avalonia.PixelPoint(620, 60) };

            desktop.MainWindow = main;
            table.Show();
        }

        base.OnFrameworkInitializationCompleted();
    }
}

// build seed 1792847421
