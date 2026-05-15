using System.Security.Claims;
using Amora.Application.Abstractions;

namespace Amora.Api.Infrastructure;

public sealed class HttpContextCurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public HttpContextCurrentUserService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public Guid UserId
    {
        get
        {
            var context = _httpContextAccessor.HttpContext ?? throw new InvalidOperationException("HttpContext is unavailable.");
            var value = context.User.FindFirst("id")?.Value
                ?? context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (!Guid.TryParse(value, out var userId))
            {
                throw new InvalidOperationException("User id claim is missing or invalid.");
            }

            return userId;
        }
    }
}