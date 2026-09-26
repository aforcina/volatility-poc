using VolatilityPoc.Grpc;

namespace ComputeHost.Services;

public record VolPointModel(double Strike, string Maturity, double ImpliedVol);

public interface IQuantEngine
{
    Task ComputeSurfaceAsync(ComputeRequest request, Func<double, IEnumerable<VolPointModel>?, Task> progressCallback, CancellationToken token);
    IEnumerable<VolPointModel> GetLastSurface(string requestId);
}
