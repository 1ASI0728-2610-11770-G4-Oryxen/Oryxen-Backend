using Oryxen.Application.Telemetry.Contracts;

namespace Oryxen.Application.Telemetry;

public interface ITelemetryService
{
    Task<TelemetryResponse> IngestAsync(TelemetryIngestRequest request, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<TelemetryResponse>> GetByPlantAsync(
        Guid plantId,
        DateTime? from,
        DateTime? to,
        CancellationToken cancellationToken = default);
}
