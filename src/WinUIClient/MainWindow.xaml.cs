using Microsoft.UI.Xaml;
using Microsoft.Web.WebView2.Core;
using WinUIClient.ViewModels;
using Microsoft.UI.Dispatching;
using System.Linq;

namespace WinUIClient;

public sealed partial class MainWindow : Window
{
    public MainViewModel ViewModel { get; } = new();

    private IEnumerable<WinUIClient.ViewModels.ChartPointDto> _nativePoints = Array.Empty<WinUIClient.ViewModels.ChartPointDto>();
    private string? _selectedMaturityForNative;

    public MainWindow()
    {
        this.InitializeComponent();
        this.DataContext = ViewModel;
        _ = InitializeWebViewAsync();

        // Register native chart updater
        ViewModel.RegisterNativeChartUpdater(UpdateNativeChartPoints);
    }

    private async Task InitializeWebViewAsync()
    {
        await ChartView.EnsureCoreWebView2Async();
        var webFolder = Path.Combine(AppContext.BaseDirectory, "WebContent");
        ChartView.CoreWebView2.SetVirtualHostNameToFolderMapping("appassets", webFolder, CoreWebView2HostResourceAccessKind.Allow);
        ChartView.Source = new Uri("http://appassets/chart.html");
        WebViewBridge.WebViewInstance = ChartView;
    }

    private async void OnSettingsClick(object sender, RoutedEventArgs e)
    {
        var dlg = new SettingsDialog(ViewModel);
        await dlg.ShowAsync();
    }

    private async void OnComputeClick(object sender, RoutedEventArgs e) => await ViewModel.SubmitComputeAsync();
    private void OnCancelClick(object sender, RoutedEventArgs e) => ViewModel.Cancel();
    private async void OnExportClick(object sender, RoutedEventArgs e) => await ViewModel.ExportAsync();

    private void UpdateNativeChartPoints(IEnumerable<WinUIClient.ViewModels.ChartPointDto> pts)
    {
        var points = pts.ToList();
        if (!points.Any()) return;
        _selectedMaturityForNative = points[0].Maturity;
        _nativePoints = points.Where(p => p.Maturity == _selectedMaturityForNative).OrderBy(p => p.Strike).ToArray();

        // schedule UI redraw
        _ = DispatcherQueue.TryEnqueue(() => NativeCanvas.Invalidate());
    }

    private void NativeCanvas_Draw(Microsoft.Graphics.Canvas.UI.Xaml.CanvasControl sender, Microsoft.Graphics.Canvas.UI.Xaml.CanvasDrawEventArgs args)
    {
        var ds = args.DrawingSession;
        var width = (float)sender.ActualWidth;
        var height = (float)sender.ActualHeight;
        ds.Clear(Windows.UI.Colors.White);

        var pts = _nativePoints.ToArray();
        if (pts.Length == 0) return;

        var strikes = pts.Select(p => p.Strike).ToArray();
        var vols = pts.Select(p => p.Vol).ToArray();
        var minStrike = strikes.Min();
        var maxStrike = strikes.Max();
        var minVol = vols.Min();
        var maxVol = vols.Max();

        float left = 40, right = 20, top = 20, bottom = 40;
        float plotW = width - left - right;
        float plotH = height - top - bottom;

        ds.DrawLine(left, top + plotH, left + plotW, top + plotH, Microsoft.UI.Colors.Black, 1);
        ds.DrawLine(left, top, left, top + plotH, Microsoft.UI.Colors.Black, 1);

        float MapX(double strike) => left + (float)((strike - minStrike) / (maxStrike - minStrike) * plotW);
        float MapY(double vol) => top + (float)((maxVol - vol) / (maxVol - minVol) * plotH);

        for (int i = 1; i < pts.Length; i++)
        {
            var p0 = pts[i - 1];
            var p1 = pts[i];
            ds.DrawLine(MapX(p0.Strike), MapY(p0.Vol), MapX(p1.Strike), MapY(p1.Vol), Microsoft.UI.Colors.CornflowerBlue, 2);
        }

        foreach (var p in pts)
        {
            ds.FillCircle(MapX(p.Strike), MapY(p.Vol), 3, Microsoft.UI.Colors.DarkBlue);
        }

        ds.DrawText($"Maturity: {_selectedMaturityForNative}", left, 2, Microsoft.UI.Colors.Black);
    }
}
