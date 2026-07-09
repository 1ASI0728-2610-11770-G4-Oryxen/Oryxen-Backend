namespace Oryxen.Application.AI.Contracts;

/// <summary>The assistant's reply to a <see cref="ChatRequest"/>.</summary>
public sealed record ChatResponse(
    string Reply,
    string Provider,
    DateTime GeneratedAt);
