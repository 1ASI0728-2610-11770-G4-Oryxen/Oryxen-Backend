using System.Threading;
using System.Threading.Tasks;
using Oryxen.Application.Telemetry.Contracts;

namespace Oryxen.Application.Telemetry;

public interface IIoTSimulationService
{
    Task<SeedResultResponse> SeedHistoricalDataAsync(int days = 30, CancellationToken cancellationToken = default);
    
    Task GenerateRealtimeReadingsAsync(CancellationToken cancellationToken = default);
}
