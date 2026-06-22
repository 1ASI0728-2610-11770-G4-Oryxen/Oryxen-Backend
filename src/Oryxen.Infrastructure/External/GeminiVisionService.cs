using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;
using Oryxen.Application.Common.Exceptions;
using Oryxen.Application.Common.Interfaces;
using Oryxen.Application.Common.Models;

namespace Oryxen.Infrastructure.External;

/// <summary>
/// <see cref="IMultimodalAiService"/> backed by Google's Gemini 2.0 Flash Vision API.
/// Sends a plant photograph (inline base64) together with the latest Sensor Lite telemetry
/// as a textual prompt, asking the model to act as a Multimodal Phytopathology and Plant
/// Health Assistant that diagnoses the general health status of any plant, identifying
/// anomalies, nutrient deficiencies, diseases, pests or environmental stress. The response
/// is parsed into a structured <see cref="AiDiagnosisResult"/>. Network errors, missing
/// configuration or unparseable payloads are surfaced as <see cref="ExternalServiceException"/>
/// (HTTP 502) by the API middleware.
/// </summary>
public sealed class GeminiVisionService : IMultimodalAiService
{
    private const string PromptTemplate = """
        You are a Multimodal Phytopathology and Agricultural Health Assistant.
        Your task is to analyze a plant photograph together with environmental
        sensor telemetry to diagnose the overall health of the plant.

        Examine the image for visual signs on leaves, stems, flowers and fruits:
        discoloration, spots, wilting, chewed edges, holes, larvae, fungal growth,
        nutrient deficiency symptoms, or any other anomaly.

        Recent Sensor Lite telemetry for this plant:
          - Soil moisture: {SoilMoisture}%
          - Air humidity: {Humidity}%
          - Temperature: {Temperature}°C

        Correlate the visual signs with the environmental conditions to produce
        a comprehensive diagnosis.

        Respond ONLY with a compact JSON object, no markdown fences, using exactly
        this schema:
        {
          "detectedPest": "<pest or anomaly name, or 'None' if healthy>",
          "confidenceScore": 0.0 to 1.0,
          "recommendation": "<specific care or mitigation recommendation>"
        }

        If the plant appears healthy, set detectedPest to "None", confidenceScore
        to the confidence of that negative finding, and recommendation to a
        preventive care tip based on the current telemetry.
        """;

    private readonly HttpClient _http;
    private readonly GeminiVisionSettings _settings;

    public GeminiVisionService(HttpClient http, IOptions<GeminiVisionSettings> settings)
    {
        _http = http;
        _settings = settings.Value;
    }

    public async Task<AiDiagnosisResult> AnalyzeAsync(
        byte[] imageBytes,
        string mimeType,
        double? soilMoisture,
        double? humidity,
        double? temperature,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_settings.ApiKey))
        {
            throw new ExternalServiceException(
                "Gemini Vision API key is not configured (set GeminiVision__ApiKey).");
        }

        var prompt = PromptTemplate
            .Replace("{SoilMoisture}", soilMoisture?.ToString("0.0") ?? "N/A")
            .Replace("{Humidity}", humidity?.ToString("0.0") ?? "N/A")
            .Replace("{Temperature}", temperature?.ToString("0.0") ?? "N/A");

        var base64 = Convert.ToBase64String(imageBytes);
        var url = $"/v1beta/models/{_settings.Model}:generateContent?key={_settings.ApiKey}";

        var payload = new GeminiRequest
        {
            Contents = new[]
            {
                new GeminiContent
                {
                    Parts = new object[]
                    {
                        new GeminiTextPart { Text = prompt },
                        new GeminiInlinePart
                        {
                            InlineData = new GeminiInlineData { MimeType = mimeType, Data = base64 }
                        }
                    }
                }
            },
            GenerationConfig = new GeminiGenerationConfig
            {
                Temperature = 0.2,
                MaxOutputTokens = 512,
                ResponseMimeType = "application/json"
            }
        };

        GeminiResponse? response;
        try
        {
            var httpResponse = await _http.PostAsJsonAsync(url, payload, cancellationToken);
            if (!httpResponse.IsSuccessStatusCode)
            {
                throw new ExternalServiceException(
                    $"Gemini Vision API returned HTTP {(int)httpResponse.StatusCode}.");
            }

            response = await httpResponse.Content.ReadFromJsonAsync<GeminiResponse>(
                cancellationToken: cancellationToken);
        }
        catch (HttpRequestException ex)
        {
            throw new ExternalServiceException("Gemini Vision API is unreachable.", ex);
        }
        catch (TaskCanceledException ex) when (!cancellationToken.IsCancellationRequested)
        {
            throw new ExternalServiceException("Gemini Vision API request timed out.", ex);
        }

        var text = response?.Candidates?.FirstOrDefault()?.Content?.Parts?.FirstOrDefault()?.Text;
        if (string.IsNullOrWhiteSpace(text))
        {
            throw new ExternalServiceException("Gemini Vision API returned an empty response.");
        }

        return ParseStructuredResult(text);
    }

    /// <summary>
    /// Parses the JSON returned by Gemini into the normalized <see cref="AiDiagnosisResult"/>.
    /// Tolerates leading/trailing whitespace and markdown fences if the model adds them.
    /// </summary>
    private static AiDiagnosisResult ParseStructuredResult(string text)
    {
        var json = text.Trim();
        if (json.StartsWith("```"))
        {
            var firstNewline = json.IndexOf('\n');
            if (firstNewline >= 0) json = json[(firstNewline + 1)..];
            var lastFence = json.LastIndexOf("```", StringComparison.Ordinal);
            if (lastFence >= 0) json = json[..lastFence];
            json = json.Trim();
        }

        try
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            return new AiDiagnosisResult(
                DetectedPest: root.TryGetProperty("detectedPest", out var pest) ? pest.GetString() ?? "None" : "None",
                ConfidenceScore: root.TryGetProperty("confidenceScore", out var conf) ? conf.GetDouble() : 0d,
                Recommendation: root.TryGetProperty("recommendation", out var rec) ? rec.GetString() ?? string.Empty : string.Empty);
        }
        catch (JsonException ex)
        {
            throw new ExternalServiceException("Gemini Vision API returned an unparseable payload.", ex);
        }
    }

    // ---- Gemini wire DTOs (internal) ------------------------------------------

    private sealed record GeminiRequest
    {
        [JsonPropertyName("contents")] public GeminiContent[] Contents { get; init; } = Array.Empty<GeminiContent>();
        [JsonPropertyName("generationConfig")] public GeminiGenerationConfig? GenerationConfig { get; init; }
    }

    private sealed record GeminiContent
    {
        [JsonPropertyName("parts")] public object[] Parts { get; init; } = Array.Empty<object>();
    }

    private sealed record GeminiTextPart
    {
        [JsonPropertyName("text")] public string Text { get; init; } = string.Empty;
    }

    private sealed record GeminiInlinePart
    {
        [JsonPropertyName("inline_data")] public GeminiInlineData InlineData { get; init; } = new();
    }

    private sealed record GeminiInlineData
    {
        [JsonPropertyName("mime_type")] public string MimeType { get; init; } = string.Empty;
        [JsonPropertyName("data")] public string Data { get; init; } = string.Empty;
    }

    private sealed record GeminiGenerationConfig
    {
        [JsonPropertyName("temperature")] public double Temperature { get; init; }
        [JsonPropertyName("max_output_tokens")] public int MaxOutputTokens { get; init; }
        [JsonPropertyName("response_mime_type")] public string ResponseMimeType { get; init; } = "application/json";
    }

    private sealed record GeminiResponse
    {
        [JsonPropertyName("candidates")] public List<GeminiCandidate>? Candidates { get; init; }
    }

    private sealed record GeminiCandidate
    {
        [JsonPropertyName("content")] public GeminiCandidateContent? Content { get; init; }
    }

    private sealed record GeminiCandidateContent
    {
        [JsonPropertyName("parts")] public List<GeminiResponsePart>? Parts { get; init; }
    }

    private sealed record GeminiResponsePart
    {
        [JsonPropertyName("text")] public string? Text { get; init; }
    }
}
