using CommunityToolkit.Mvvm.ComponentModel;
using Grpc.Net.Client;
using VolatilityPoc.Grpc;
using System.Collections.ObjectModel;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.UI.Xaml;

namespace WinUIClient.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private const string ChartModeKey = "ChartMode";

    public ObservableCollection<string> Instruments { get; } = new() { "SPX", "AAPL", "MSFT" };

    [ObservableProperty]
    private string _selectedInstrument = "SPX";

    [ObservableProperty]
    private double _progress;

    [ObservableProperty]
    private string _statusMessage = "Idle";

    [ObservableProperty]
    private int _selectedChartModeIndex;

    public Visibility WebViewVisibility => SelectedChartModeIndex == 0 ? Visibility.Visible : Visibility.Collapsed;
    public Visibility NativeVisibility => SelectedChartModeIndex == 1 ? Visibility.Visible : Visibility.Collapsed;

    private CancellationTokenSource? _cts;
    private readonly string _grpcAddress = "http://127.0.0.1:5001";
    private readonly string _marketQuoteApi = "http://127.0.0.1:5001/api/marketquote";

    private Action<IEnumerable<ChartPointDto>>? _nativeChartUpdater;

    public MainViewModel()
    {
        // read saved setting; if not present default to 0 (WebView)
        try
        {
            var settings = Windows.Storage.ApplicationData.Current.LocalSettings;
            if (settings.Values.TryGetValue(ChartModeKey, out var val) && val is int idx)
            {
                SelectedChartModeIndex = idx;
            }
            else
            {
                SelectedChartModeIndex = 0;
            }
        }
        catch
        {
            SelectedChartModeIndex = 0;
        }
    }

    partial void OnSelectedChartModeIndexChanged(int oldValue, int newValue)
    {
        // persist
        try
        {
            var settings = Windows.Storage.ApplicationData.Current.LocalSettings;
            settings.Values[ChartModeKey] = newValue;
        }
        catch { /* ignore for POC */ }

        // notify visibility bindings
        OnPropertyChanged(nameof(WebViewVisibility));
        OnPropertyChanged(nameof(NativeVisibility));
    }

    public void RegisterNativeChartUpdater(Action<IEnumerable<ChartPointDto>> updater)
    {
        _nativeChartUpdater = updater;
    }

    public async Task SubmitComputeAsync()
    {
        _cts?.Cancel();
        _cts = new CancellationTokenSource();

        Progress = 0;
        StatusMessage = "Fetching market quotes...";

        // Call REST market quote stub
        using var http = new HttpClient();
        var quotes = await http.GetFromJsonAsync<List<MarketQuoteDto>>($"{_marketQuoteApi}/{SelectedInstrument}", _cts.Token);

        if (quotes == null || !quotes.Any())
        {
            StatusMessage = "No quotes";
            return;
        }

        StatusMessage = "Submitting compute request...";

        var channel = GrpcChannel.ForAddress(_grpcAddress);
        var client = new ComputeService.ComputeServiceClient(channel);

        var request = new ComputeRequest
        {
            RequestId = Guid.NewGuid().ToString(),
            InstrumentId = SelectedInstrument
        };

        foreach (var q in quotes)
        {
            request.Quotes.Add(new OptionQuote
            {
                Side = q.Side,
                Strike = q.Strike,
                Bid = q.Bid,
                Ask = q.Ask,
                Maturity = q.Maturity
            });
        }

        using var call = client.Compute(request, cancellationToken: _cts.Token);

        await foreach (var update in call.ResponseStream.ReadAllAsync(_cts.Token))
        {
            Progress = update.Progress;
            StatusMessage = update.Message;

            if (update.Snapshot.Count > 0)
            {
                var snapshot = update.Snapshot.Select(p => new { strike = p.Strike, maturity = p.Maturity, vol = p.ImpliedVol }).ToArray();
                PublishSnapshot(snapshot);
            }

            if (update.IsFinal)
            {
                StatusMessage = "Computation finished";
                break;
            }
        }
    }

    private void PublishSnapshot(IEnumerable<object> snapshot)
    {
        // Convert snapshot to simplified DTO
        var points = snapshot.Select(p =>
        {
            // snapshot element is anonymous with strike,maturity,vol
            var dict = p.GetType().GetProperties().ToDictionary(pi => pi.Name, pi => pi.GetValue(p));
            return new ChartPointDto(
                Strike: Convert.ToDouble(dict["strike"]),
                Maturity: dict["maturity"]?.ToString() ?? "",
                Vol: Convert.ToDouble(dict["vol"])
            );
        }).ToList();

        if (SelectedChartModeIndex == 0)
        {
            var json = JsonSerializer.Serialize(points);
            WebViewBridge.PostMessageToChart(json);
        }
        else
        {
            _nativeChartUpdater?.Invoke(points);
        }
    }

    public void Cancel()
    {
        _cts?.Cancel();
        StatusMessage = "Cancelled";
    }

    public async Task ExportAsync()
    {
        StatusMessage = "Exporting...";
        await Task.Delay(400);
        StatusMessage = "Export complete";
    }

    private record MarketQuoteDto(string Side, double Strike, double Bid, double Ask, string Maturity);

    public record ChartPointDto(double Strike, string Maturity, double Vol);
}
