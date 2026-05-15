namespace Amora.Application.Abstractions;

public interface ICurrentUserService
{
    Guid UserId { get; }
}