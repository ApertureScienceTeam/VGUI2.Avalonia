namespace VGUI2.Avalonia.Demo;

public partial class TableWindow : VGUI2.Avalonia.Controls.VguiWindow
{
    private sealed record ServerRow(string Server, string Game, string Players, string Map, int Ping);

    public TableWindow()
    {
        InitializeComponent();

        ServerTable.ItemsSource = new[]
        {
            new ServerRow("Counter-Strike 1.6 3266 Server", "Counter-Strike", "10 / 10", "de_dust2", 104),
            new ServerRow("duote", "Condition Zero", "16 / 32", "cs_assault", 221),
            new ServerRow("duote", "Condition Zero", "16 / 32", "!!!!!!!!17_iceword", 174),
            new ServerRow("lai wan cs a", "Condition Zero", "5 / 20", "fy_iceword", 311),
            new ServerRow("DcOo Counter-Strike 3382", "Counter-Strike", "14 / 32", "de_dust2", 325),
            new ServerRow("1111111111111111", "Condition Zero", "10 / 10", "de_dust2", 320),
            new ServerRow("www.Xtreme-Jumps.eu", "Condition Zero", "3 / 32", "bkz_goldbhop", 278),
            new ServerRow("duote", "Condition Zero", "7 / 20", "-=911=-_dao", 117),
            new ServerRow("洛阳僵尸服#6", "Zombie Plague 4.3", "18 / 32", "zm_abyss3", 255),
            new ServerRow("CT", "Counter-Strike", "16 / 32", "cs_bloodstrike", 341),
        };
    }
}
