namespace Amora.Application.Common;

public sealed class PagedResult<T>
{
    public int TotalCount { get; init; }

    public IReadOnlyList<T> Items { get; init; } = Array.Empty<T>();
}