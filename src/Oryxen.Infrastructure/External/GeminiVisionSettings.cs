namespace Oryxen.Infrastructure.External;

/// <summary>
/// Strongly-typed binding for the "GeminiVision" configuration section. The API key is
/// injected via the <c>GeminiVision__ApiKey</c> environment variable in production and
/// is never committed to source control.
/// </summary>
public sealed class GeminiVisionSettings
{
    public const string SectionName = "GeminiVision";

    public string ApiKey { get; set; } = string.Empty;

    public string BaseUrl { get; set; } = "https://generativelanguage.googleapis.com";

    /// <summary>Gemini multimodal model used for image + text analysis.</summary>
    public string Model { get; set; } = "gemini-2.0-flash";
}
