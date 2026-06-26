using Amora.Domain.Enums;

namespace Amora.Application.Dtos.Posts;

public sealed class ReactToPostRequest
{
    public ReactionType ReactionType { get; init; }
}

public sealed class ReactToPostResponse
{
    public int NewReactionCount { get; init; }
    
    public string? CurrentReactionType { get; init; }
}
