using Oryxen.Application.AI.Contracts;

namespace Oryxen.Application.AI;

/// <summary>Application service for the conversational plant-care assistant.</summary>
public interface IChatService
{
    Task<ChatResponse> ChatAsync(ChatRequest request, CancellationToken cancellationToken = default);
}
