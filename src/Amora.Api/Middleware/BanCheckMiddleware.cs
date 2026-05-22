using System.Security.Claims;
using System.Text.Json;
using Amora.Application.Common;
using Amora.Domain.Interfaces;
using Microsoft.AspNetCore.Http;

namespace Amora.Api.Middleware;

public sealed class BanCheckMiddleware
{
    private readonly RequestDelegate _next;

    public BanCheckMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, IUserRepository userRepository)
    {
        if (context.User.Identity?.IsAuthenticated == true)
        {
            var userIdClaim = context.User.FindFirst("id")?.Value ?? context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (Guid.TryParse(userIdClaim, out var userId))
            {
                var user = await userRepository.GetByIdAsync(userId);
                if (user != null)
                {
                    // Check permanent ban or temporary ban
                    if (user.IsBanned)
                    {
                        bool stillBanned = true;
                        
                        // If it's a temporary ban, check if it has expired
                        if (user.BannedUntil.HasValue)
                        {
                            if (DateTimeOffset.UtcNow > user.BannedUntil.Value)
                            {
                                // Ban has expired
                                stillBanned = false;
                                
                                // Optionally auto-unban them in db
                                user.IsBanned = false;
                                user.BannedUntil = null;
                                user.BanReason = null;
                                await userRepository.UpdateAsync(user);
                            }
                        }

                        if (stillBanned)
                        {
                            context.Response.StatusCode = StatusCodes.Status403Forbidden;
                            context.Response.ContentType = "application/json";

                            var reason = user.BanReason ?? "Your account has been suspended.";
                            if (user.BannedUntil.HasValue)
                            {
                                reason += $" Ban expires at {user.BannedUntil.Value:yyyy-MM-dd HH:mm} UTC.";
                            }

                            var payload = ApiResponse<object>.Fail(reason, "account_banned");
                            await context.Response.WriteAsync(JsonSerializer.Serialize(payload));
                            return;
                        }
                    }
                }
            }
        }

        await _next(context);
    }
}
