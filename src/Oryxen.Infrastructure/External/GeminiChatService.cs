using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;
using Oryxen.Application.Common.Exceptions;
using Oryxen.Application.Common.Interfaces;

namespace Oryxen.Infrastructure.External;

/// <summary>
/// <see cref="IChatAiService"/> backed by Google's Gemini text generation API (same model
/// and settings as the vision diagnosis flow, plain-text output). Keeps the API key on the
/// server: clients talk to <c>POST /api/v1/ai/chat</c>, never to Google directly. Missing
/// configuration or provider failures surface as <see cref="ExternalServiceException"/>
/// (HTTP 502) via the API middleware.
/// </summary>
public sealed class GeminiChatService : IChatAiService
{
    private const string SystemPreamble = """
        You are Oryx, the Oryxen plant-care assistant. Oryxen is a smart plant-care
        platform combining Sensor Lite IoT telemetry (soil moisture, air humidity,
        temperature, light) with automated watering. Answer the user's question in a
        friendly, practical tone, focused on plant care. Keep replies under 180 words.
        If the question is unrelated to plants or Oryxen, politely steer back to plant care.
        """;

    private readonly HttpClient _http;
    private readonly GeminiVisionSettings _settings;

    public GeminiChatService(HttpClient http, IOptions<GeminiVisionSettings> settings)
    {
        _http = http;
        _settings = settings.Value;
    }

    public async Task<string> GenerateReplyAsync(string message, string? context, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_settings.ApiKey))
        {
            throw new ExternalServiceException(
                "Gemini API key is not configured (set GeminiVision__ApiKey).");
        }

        var prompt = string.IsNullOrWhiteSpace(context)
            ? $"{SystemPreamble}\n\nUser: {message}"
            : $"{SystemPreamble}\n\nConversation context:\n{context}\n\nUser: {message}";

        var url = $"/v1beta/models/{_settings.Model}:generateContent?key={_settings.ApiKey}";

        var payload = new ChatGeminiRequest
        {
            Contents = new[]
            {
                new ChatGeminiContent
                {
                    Parts = new[] { new ChatGeminiTextPart { Text = prompt } }
                }
            },
            GenerationConfig = new ChatGeminiGenerationConfig
            {
                Temperature = 0.7,
                MaxOutputTokens = 512
            }
        };

        ChatGeminiResponse? response;
        try
        {
            var httpResponse = await _http.PostAsJsonAsync(url, payload, cancellationToken);
            if (!httpResponse.IsSuccessStatusCode)
            {
                throw new ExternalServiceException(
                    $"Gemini API returned HTTP {(int)httpResponse.StatusCode}.");
            }

            response = await httpResponse.Content.ReadFromJsonAsync<ChatGeminiResponse>(
                cancellationToken: cancellationToken);
        }
        catch (HttpRequestException ex)
        {
            throw new ExternalServiceException("Gemini API is unreachable.", ex);
        }
        catch (TaskCanceledException ex) when (!cancellationToken.IsCancellationRequested)
        {
            throw new ExternalServiceException("Gemini API request timed out.", ex);
        }

        var text = response?.Candidates?.FirstOrDefault()?.Content?.Parts?.FirstOrDefault()?.Text;
        if (string.IsNullOrWhiteSpace(text))
        {
            throw new ExternalServiceException("Gemini API returned an empty response.");
        }

        return text.Trim();
    }

    // ---- Gemini wire DTOs (internal) ------------------------------------------

    private sealed record ChatGeminiRequest
    {
        [JsonPropertyName("contents")] public ChatGeminiContent[] Contents { get; init; } = Array.Empty<ChatGeminiContent>();
        [JsonPropertyName("generationConfig")] public ChatGeminiGenerationConfig? GenerationConfig { get; init; }
    }

    private sealed record ChatGeminiContent
    {
        [JsonPropertyName("parts")] public ChatGeminiTextPart[] Parts { get; init; } = Array.Empty<ChatGeminiTextPart>();
    }

    private sealed record ChatGeminiTextPart
    {
        [JsonPropertyName("text")] public string Text { get; init; } = string.Empty;
    }

    private sealed record ChatGeminiGenerationConfig
    {
        [JsonPropertyName("temperature")] public double Temperature { get; init; }
        [JsonPropertyName("max_output_tokens")] public int MaxOutputTokens { get; init; }
    }

    private sealed record ChatGeminiResponse
    {
        [JsonPropertyName("candidates")] public List<ChatGeminiCandidate>? Candidates { get; init; }
    }

    private sealed record ChatGeminiCandidate
    {
        [JsonPropertyName("content")] public ChatGeminiCandidateContent? Content { get; init; }
    }

    private sealed record ChatGeminiCandidateContent
    {
        [JsonPropertyName("parts")] public List<ChatGeminiResponsePart>? Parts { get; init; }
    }

    private sealed record ChatGeminiResponsePart
    {
        [JsonPropertyName("text")] public string? Text { get; init; }
    }
}
