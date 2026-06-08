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

    public async Task InvokeAsync(HttpContext context, IUserRepository userRepository, IUserBanRepository userBanRepository)
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
                        var activeBan = await userBanRepository.GetActiveBanByUserIdAsync(user.Id);
                        if (activeBan != null)
                        {
                            bool stillBanned = true;
                            
                            // If it's a temporary ban, check if it has expired
                            if (activeBan.BannedUntil.HasValue)
                            {
                                if (DateTimeOffset.UtcNow > activeBan.BannedUntil.Value)
                                {
                                    // Ban has expired
                                    stillBanned = false;
                                    
                                    // Auto-unban them in db
                                    user.IsBanned = false;
                                    await userRepository.UpdateAsync(user);
                                    
                                    activeBan.IsActive = false;
                                    await userBanRepository.UpdateAsync(activeBan);
                                }
                            }

                            if (stillBanned)
                            {
                                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                                context.Response.ContentType = "application/json";

                                var reason = activeBan.BanReason ?? "Your account has been suspended.";
                                if (activeBan.BannedUntil.HasValue)
                                {
                                    reason += $" Ban expires at {activeBan.BannedUntil.Value:yyyy-MM-dd HH:mm} UTC.";
                                }

                                var payload = ApiResponse<object>.Fail(reason, "account_banned");
                                await context.Response.WriteAsync(JsonSerializer.Serialize(payload));
                                return;
                            }
                        }
                        else
                        {
                            // Missing active ban record, auto fix
                            user.IsBanned = false;
                            await userRepository.UpdateAsync(user);
                        }
                    }
                }
            }
        }

        await _next(context);
    }
}
