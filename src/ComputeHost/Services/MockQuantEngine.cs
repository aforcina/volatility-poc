using VolatilityPoc.Grpc;

namespace ComputeHost.Services;

public class MockQuantEngine : IQuantEngine
{
    private readonly Dictionary<string, List<VolPointModel>> _store = new();

    public async Task ComputeSurfaceAsync(ComputeRequest request, Func<double, IEnumerable<VolPointModel>?, Task> progressCallback, CancellationToken token)
    {
        var maturities = request.Quotes.Select(q => q.Maturity).Distinct().Take(5).ToArray();
        var strikes = request.Quotes.Select(q => q.Strike).Distinct().OrderBy(s => s).ToArray();
        var rnd = new Random(request.InstrumentId.GetHashCode());

        int steps = 5;
        for (int step = 1; step <= steps; step++)
        {
            token.ThrowIfCancellationRequested();
            double progress = (double)step / steps;

            var snapshot = new List<VolPointModel>();
            foreach (var m in maturities)
            {
                foreach (var k in strikes)
                {
                    double baseVol = 0.2;
                    double skew = 0.0008 * (k - strikes.Average());
                    double matFactor = 0.02 * Array.IndexOf(maturities, m);
                    double noise = (rnd.NextDouble() - 0.5) * 0.002;
                    double vol = Math.Max(0.01, baseVol + skew + matFactor + noise);
                    snapshot.Add(new VolPointModel(k, m, vol));
                }
            }

            _store[request.RequestId] = snapshot.ToList();
            await progressCallback(progress, snapshot);
            await Task.Delay(400, token);
        }
    }

    public IEnumerable<VolPointModel> GetLastSurface(string requestId)
    {
        if (_store.TryGetValue(requestId, out var s)) return s;
        return Array.Empty<VolPointModel>();
    }
}
