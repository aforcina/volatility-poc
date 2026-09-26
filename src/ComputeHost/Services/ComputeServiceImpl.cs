using Grpc.Core;
using VolatilityPoc.Grpc;
using Microsoft.Extensions.Logging;

namespace ComputeHost.Services;

public class ComputeServiceImpl : ComputeService.ComputeServiceBase
{
    private readonly IQuantEngine _engine;
    private readonly ILogger<ComputeServiceImpl> _logger;

    public ComputeServiceImpl(IQuantEngine engine, ILogger<ComputeServiceImpl> logger)
    {
        _engine = engine;
        _logger = logger;
    }

    public override async Task Compute(ComputeRequest request, IServerStreamWriter<ComputeProgress> responseStream, ServerCallContext context)
    {
        _logger.LogInformation("Compute request {RequestId} for instrument {Instrument}", request.RequestId, request.InstrumentId);

        await _engine.ComputeSurfaceAsync(request, async (progress, snapshot) =>
        {
            if (context.CancellationToken.IsCancellationRequested) throw new OperationCanceledException();

            var msg = new ComputeProgress
            {
                RequestId = request.RequestId,
                Progress = progress,
                IsFinal = false,
                Message = $"Progress {progress:P0}"
            };
            if (snapshot != null)
            {
                msg.Snapshot.AddRange(snapshot.Select(p => new VolPoint
                {
                    Strike = p.Strike,
                    Maturity = p.Maturity,
                    ImpliedVol = p.ImpliedVol
                }));
            }

            await responseStream.WriteAsync(msg);
        }, context.CancellationToken);

        // Send final result
        var finalSurface = _engine.GetLastSurface(request.RequestId);
        var finalMsg = new ComputeProgress
        {
            RequestId = request.RequestId,
            Progress = 1.0,
            IsFinal = true,
            Message = "Complete"
        };
        finalMsg.Snapshot.AddRange(finalSurface.Select(p => new VolPoint {
            Strike = p.Strike,
            Maturity = p.Maturity,
            ImpliedVol = p.ImpliedVol
        }));
        await responseStream.WriteAsync(finalMsg);
    }
}
