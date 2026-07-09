namespace Oryxen.Application.Common.Interfaces;

/// <summary>
/// Text-only conversational AI port (plant-care assistant). Implementations call an
/// external LLM provider server-side so no API key ever reaches a client application.
/// </summary>
public interface IChatAiService
{
    /// <summary>Generates an assistant reply for the given user message and optional context.</summary>
    Task<string> GenerateReplyAsync(string message, string? context, CancellationToken cancellationToken = default);
}
