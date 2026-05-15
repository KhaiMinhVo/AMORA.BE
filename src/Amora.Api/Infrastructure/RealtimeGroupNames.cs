namespace Amora.Api.Infrastructure;

public static class RealtimeGroupNames
{
    public static string User(string userId) => $"user:{userId}";

    public static string Match(string matchId) => $"match:{matchId}";
}