using Oryxen.Application.AI.Contracts;
using Oryxen.Application.Common.Interfaces;

namespace Oryxen.Application.AI;

/// <summary>
/// Conversational assistant use case. Delegates text generation to the server-side
/// <see cref="IChatAiService"/> (Gemini) so client applications never hold an AI API key.
/// </summary>
public sealed class ChatService : IChatService
{
    private const string ProviderName = "gemini";

    private readonly IChatAiService _chatAi;

    public ChatService(IChatAiService chatAi) => _chatAi = chatAi;

    public async Task<ChatResponse> ChatAsync(ChatRequest request, CancellationToken cancellationToken = default)
    {
        var reply = await _chatAi.GenerateReplyAsync(request.Message, request.Context, cancellationToken);

        return new ChatResponse(reply, ProviderName, DateTime.UtcNow);
    }
}
