using Amora.Domain.Entities;

namespace Amora.Domain.Results;

public sealed record MatchCreationResult(MatchConnection MatchConnection, bool PostClosed);