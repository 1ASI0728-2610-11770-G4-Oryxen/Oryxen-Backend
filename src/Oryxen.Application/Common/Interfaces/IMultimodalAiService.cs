using Oryxen.Application.Common.Models;

namespace Oryxen.Application.Common.Interfaces;

/// <summary>
/// Multimodal AI analysis port: sends a plant photograph together with the latest
/// Sensor Lite telemetry to a vision-capable model (Gemini 2.0 Flash) that returns a
/// structured plant health diagnosis result.
/// Implemented by <c>GeminiVisionService</c> in the Infrastructure layer.
/// </summary>
public interface IMultimodalAiService
{
    Task<AiDiagnosisResult> AnalyzeAsync(
        byte[] imageBytes,
        string mimeType,
        double? soilMoisture,
        double? humidity,
        double? temperature,
        CancellationToken cancellationToken = default);
}
