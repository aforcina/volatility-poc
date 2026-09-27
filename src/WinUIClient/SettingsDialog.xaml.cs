using Microsoft.UI.Xaml.Controls;
using WinUIClient.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using Windows.UI;

namespace WinUIClient;

public sealed partial class SettingsDialog : ContentDialog
{
    private readonly MainViewModel _vm;

    private readonly (string Name, string Hex)[] _accents = new[] {
        ("Default Blue", "#4CC2FF"),
        ("Deep Blue", "#0078D4"),
        ("Teal", "#20C997"),
        ("Purple", "#8A2BE2"),
        ("Orange", "#FF8C00"),
        ("Green", "#36C98F")
    };

    public SettingsDialog(MainViewModel vm)
    {
        this.InitializeComponent();
        _vm = vm;

        // Chart mode
        ChartModeCombo.Items.Add("Web (WebView2)");
        ChartModeCombo.Items.Add("Native (Win2D)");
        ChartModeCombo.SelectedIndex = _vm.SelectedChartModeIndex;

        // Theme
        ThemeCombo.Items.Add("System");
        ThemeCombo.Items.Add("Light");
        ThemeCombo.Items.Add("Dark");

        // load persisted theme
        var settings = Windows.Storage.ApplicationData.Current.LocalSettings;
        if (settings.Values.TryGetValue("AppTheme", out var themeVal) && themeVal is string themeStr)
        {
            if (themeStr == "Light") ThemeCombo.SelectedIndex = 1;
            else if (themeStr == "Dark") ThemeCombo.SelectedIndex = 2;
            else ThemeCombo.SelectedIndex = 0;
        }
        else
        {
            ThemeCombo.SelectedIndex = 0; // system
        }

        ThemeCombo.SelectionChanged += ThemeCombo_SelectionChanged;

        // Accent colors
        foreach (var a in _accents)
        {
            var tb = new TextBlock { Text = a.Name };
            AccentCombo.Items.Add(tb);
        }

        // load persisted accent
        if (settings.Values.TryGetValue("AppAccentHex", out var accentVal) && accentVal is string hex)
        {
            int idx = Array.FindIndex(_accents, x => string.Equals(x.Hex, hex, StringComparison.OrdinalIgnoreCase));
            AccentCombo.SelectedIndex = idx >= 0 ? idx : 0;
        }
        else
        {
            AccentCombo.SelectedIndex = 0;
        }

        AccentCombo.SelectionChanged += AccentCombo_SelectionChanged;

        // preview initial accent
        ApplyAccent(_accents[Math.Max(0, AccentCombo.SelectedIndex)].Hex);
    }

    private void ThemeCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        var idx = ThemeCombo.SelectedIndex;
        switch (idx)
        {
            case 1: // Light
                ApplyTheme("Light");
                break;
            case 2: // Dark
                ApplyTheme("Dark");
                break;
            default:
                ApplyTheme("System");
                break;
        }
    }

    private void AccentCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        var idx = AccentCombo.SelectedIndex;
        if (idx < 0 || idx >= _accents.Length) return;
        var hex = _accents[idx].Hex;
        ApplyAccent(hex);
    }

    private void ApplyTheme(string theme)
    {
        // theme: "System" | "Light" | "Dark"
        try
        {
            var app = Microsoft.UI.Xaml.Application.Current;
            if (app != null)
            {
                if (theme == "Light") app.RequestedTheme = ApplicationTheme.Light;
                else if (theme == "Dark") app.RequestedTheme = ApplicationTheme.Dark;
                else app.RequestedTheme = ApplicationTheme.Default; // system
            }

            // persist selection in settings for next runs
            var settings = Windows.Storage.ApplicationData.Current.LocalSettings;
            settings.Values["AppTheme"] = theme;
        }
        catch
        {
            // ignore for POC
        }
    }

    private void ApplyAccent(string hex)
    {
        try
        {
            // parse hex (#RRGGBB)
            var color = HexToColor(hex);
            var brush = new SolidColorBrush(color);

            var resources = Microsoft.UI.Xaml.Application.Current.Resources;
            // update accent brushes used in theme
            if (resources.ContainsKey("AppAccentBrush")) resources["AppAccentBrush"] = brush;
            else resources.Add("AppAccentBrush", brush);

            if (resources.ContainsKey("AppAccentStrongBrush"))
            {
                // darken slightly for strong accent
                var strong = Darken(color, 0.25f);
                resources["AppAccentStrongBrush"] = new SolidColorBrush(strong);
            }
            else
            {
                resources.Add("AppAccentStrongBrush", new SolidColorBrush(Darken(color, 0.25f)));
            }

            AccentPreview.Text = $"Accent: {hex}";

            // persist
            var settings = Windows.Storage.ApplicationData.Current.LocalSettings;
            settings.Values["AppAccentHex"] = hex;
        }
        catch
        {
            // ignore for POC
        }
    }

    private static Color HexToColor(string hex)
    {
        if (string.IsNullOrWhiteSpace(hex)) return Colors.Transparent;
        hex = hex.TrimStart('#');
        if (hex.Length == 6)
        {
            byte r = Convert.ToByte(hex.Substring(0, 2), 16);
            byte g = Convert.ToByte(hex.Substring(2, 2), 16);
            byte b = Convert.ToByte(hex.Substring(4, 2), 16);
            return Color.FromArgb(255, r, g, b);
        }
        if (hex.Length == 8)
        {
            byte a = Convert.ToByte(hex.Substring(0, 2), 16);
            byte r = Convert.ToByte(hex.Substring(2, 2), 16);
            byte g = Convert.ToByte(hex.Substring(4, 2), 16);
            byte b = Convert.ToByte(hex.Substring(6, 2), 16);
            return Color.FromArgb(a, r, g, b);
        }
        return Colors.Transparent;
    }

    private static Color Darken(Color c, float amount)
    {
        float f = 1 - amount;
        return Color.FromArgb(c.A,
            (byte)(c.R * f),
            (byte)(c.G * f),
            (byte)(c.B * f));
    }

    private void ContentDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
    {
        // Save chart mode selection back to ViewModel
        _vm.SelectedChartModeIndex = ChartModeCombo.SelectedIndex;

        // Theme & accent already persisted in ApplyTheme/ApplyAccent
    }

    private void ContentDialog_SecondaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
    {
        // Revert preview changes by re-applying persisted values
        var settings = Windows.Storage.ApplicationData.Current.LocalSettings;
        if (settings.Values.TryGetValue("AppTheme", out var themeVal) && themeVal is string themeStr)
        {
            ApplyTheme(themeStr);
        }
        else
        {
            ApplyTheme("System");
        }

        if (settings.Values.TryGetValue("AppAccentHex", out var accentVal) && accentVal is string hex)
        {
            ApplyAccent(hex);
        }
    }
}
