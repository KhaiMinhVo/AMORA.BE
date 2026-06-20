using Amora.Api.Infrastructure;
using Amora.Application.Pets;
using Amora.Domain.Entities;
using Amora.Domain.Enums;
using Amora.Domain.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Caching.Memory;
using Amora.Application.Abstractions;

namespace Amora.Api.Hubs;

[Authorize]
public sealed class CallHub : Hub
{
    private readonly IMatchConnectionRepository _matches;
    private readonly PetFeatureGateService _featureGate;
    private readonly IUserPresenceTracker _presenceTracker;
    private readonly ILogger<CallHub> _logger;
    private readonly IMemoryCache _cache;
    private readonly IPetRepository _petRepository;
    private readonly IPetRealtimeNotifier _petNotifier;

    public CallHub(
        IMatchConnectionRepository matches, 
        PetFeatureGateService featureGate, 
        IUserPresenceTracker presenceTracker, 
        ILogger<CallHub> logger,
        IMemoryCache cache,
        IPetRepository petRepository,
        IPetRealtimeNotifier petNotifier)
    {
        _matches = matches;
        _featureGate = featureGate;
        _presenceTracker = presenceTracker;
        _logger = logger;
        _cache = cache;
        _petRepository = petRepository;
        _petNotifier = petNotifier;
    }

    public override async Task OnConnectedAsync()
    {
        _logger.LogInformation(
            "CallHub connected: UserId={UserId}, ConnectionId={ConnectionId}",
            Context.UserIdentifier,
            Context.ConnectionId);

        if (Guid.TryParse(GetUserId(), out var userId))
        {
            await _presenceTracker.UserConnectedAsync(userId, Context.ConnectionId);
        }
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        _logger.LogInformation(
            exception,
            "CallHub disconnected: UserId={UserId}, ConnectionId={ConnectionId}",
            Context.UserIdentifier,
            Context.ConnectionId);

        if (Guid.TryParse(GetUserId(), out var userId))
        {
            await _presenceTracker.UserDisconnectedAsync(userId, Context.ConnectionId);
        }
        await base.OnDisconnectedAsync(exception);
    }

    public async Task JoinMatchCall(string matchId)
    {
        var matchGuid = await EnsureParticipantAsync(matchId, Context.ConnectionAborted);
        await Groups.AddToGroupAsync(Context.ConnectionId, RealtimeGroupNames.Match(matchGuid.ToString()), Context.ConnectionAborted);
    }

    public Task LeaveMatchCall(string matchId)
    {
        return Groups.RemoveFromGroupAsync(Context.ConnectionId, RealtimeGroupNames.Match(matchId), Context.ConnectionAborted);
    }

    public async Task<CallStartResponse> StartCall(string matchId, string callType)
    {
        var matchGuid = await EnsureActiveMatchAsync(matchId, Context.ConnectionAborted);
        var maxDurationSeconds = await _featureGate.ValidateCallAsync(matchGuid, callType, Context.ConnectionAborted);
        var callId = Guid.NewGuid().ToString("N");
        var normalizedType = NormalizeCallType(callType);
        var now = DateTimeOffset.UtcNow;

        await _matches.CompleteHandshakeAsync(matchGuid, Context.ConnectionAborted);

        var payload = new
        {
            callId,
            matchId = matchGuid,
            type = normalizedType,
            fromUserId = GetUserId(),
            maxDurationSeconds,
            startedAt = now,
            expiresAt = now.AddSeconds(maxDurationSeconds)
        };

        await Clients.Group(RealtimeGroupNames.Match(matchGuid.ToString()))
            .SendAsync("CallStarted", payload, Context.ConnectionAborted);

        // Store call start time in cache
        _cache.Set($"CallStart_{callId}", now, TimeSpan.FromHours(12));

        return new CallStartResponse
        {
            CallId = callId,
            MaxDurationSeconds = maxDurationSeconds
        };
    }

    public async Task EndCall(string matchId, string callId, string? reason = null)
    {
        var matchGuid = await EnsureParticipantAsync(matchId, Context.ConnectionAborted);

        var payload = new
        {
            callId,
            matchId = matchGuid,
            fromUserId = GetUserId(),
            reason
        };

        await Clients.Group(RealtimeGroupNames.Match(matchGuid.ToString()))
            .SendAsync("CallEnded", payload, Context.ConnectionAborted);

        // Compute Voice EXP
        if (_cache.TryGetValue($"CallStart_{callId}", out DateTimeOffset startedAt))
        {
            _cache.Remove($"CallStart_{callId}");
            var durationSeconds = (DateTimeOffset.UtcNow - startedAt).TotalSeconds;

            if (durationSeconds > 0) // Any call duration
            {
                var pet = await _petRepository.GetByMatchIdAsync(matchGuid, Context.ConnectionAborted);
                if (pet != null)
                {
                    var gain = PetEngine.AwardVoiceRp(pet, durationSeconds);
                    if (gain > 0)
                    {
                        pet.Stage = PetEngine.EvaluateStage(pet);
                        pet.UpdatedAt = DateTimeOffset.UtcNow;

                        var minutes = (int)Math.Floor(durationSeconds / 60.0);
                        if (minutes == 0) minutes = 1; // display 1 min if <60s

                        await _petRepository.AddActivityAsync(new PetActivity
                        {
                            Id = Guid.NewGuid(),
                            MatchId = matchGuid,
                            PetId = pet.Id,
                            UserId = Guid.Parse(GetUserId()),
                            ActivityType = "voice_chat",
                            Description = $"Hoàn thành cuộc gọi ({minutes} phút), nhận +{gain} RP",
                            CreatedAt = DateTimeOffset.UtcNow,
                            UpdatedAt = DateTimeOffset.UtcNow
                        }, Context.ConnectionAborted);

                        await _petRepository.SaveChangesAsync(Context.ConnectionAborted);

                        var match = await _matches.GetByIdAsync(matchGuid, Context.ConnectionAborted);
                        if (match != null)
                        {
                            await _petNotifier.NotifyPetStatusUpdatedAsync(pet, match, Context.ConnectionAborted);
                        }
                    }
                }
            }
        }
    }

    public async Task SendOffer(string matchId, string callId, string sdp)
    {
        var matchGuid = await EnsureParticipantAsync(matchId, Context.ConnectionAborted);

        var payload = new
        {
            callId,
            matchId = matchGuid,
            fromUserId = GetUserId(),
            sdp
        };

        await Clients.OthersInGroup(RealtimeGroupNames.Match(matchGuid.ToString()))
            .SendAsync("CallOffer", payload, Context.ConnectionAborted);
    }

    public async Task SendAnswer(string matchId, string callId, string sdp)
    {
        var matchGuid = await EnsureParticipantAsync(matchId, Context.ConnectionAborted);

        var payload = new
        {
            callId,
            matchId = matchGuid,
            fromUserId = GetUserId(),
            sdp
        };

        await Clients.OthersInGroup(RealtimeGroupNames.Match(matchGuid.ToString()))
            .SendAsync("CallAnswer", payload, Context.ConnectionAborted);
    }

    public async Task SendIceCandidate(string matchId, string callId, string candidate, string? sdpMid, int? sdpMLineIndex)
    {
        var matchGuid = await EnsureParticipantAsync(matchId, Context.ConnectionAborted);

        var payload = new
        {
            callId,
            matchId = matchGuid,
            fromUserId = GetUserId(),
            candidate,
            sdpMid,
            sdpMLineIndex
        };

        await Clients.OthersInGroup(RealtimeGroupNames.Match(matchGuid.ToString()))
            .SendAsync("CallIceCandidate", payload, Context.ConnectionAborted);
    }

    private async Task<Guid> EnsureParticipantAsync(string matchId, CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(matchId, out var matchGuid))
            throw new HubException("Invalid matchId.");

        var userId = GetUserId();
        if (!Guid.TryParse(userId, out var userGuid))
            throw new HubException("Invalid userId.");

        if (!await _matches.IsParticipantAsync(matchGuid, userGuid, cancellationToken))
            throw new HubException("You cannot access this call.");

        return matchGuid;
    }

    private async Task<Guid> EnsureActiveMatchAsync(string matchId, CancellationToken cancellationToken)
    {
        var matchGuid = await EnsureParticipantAsync(matchId, cancellationToken);
        var match = await _matches.GetByIdAsync(matchGuid, cancellationToken);
        if (match is null || match.Status != MatchStatus.Active)
            throw new HubException("Match expired or inactive.");

        return matchGuid;
    }

    private static string NormalizeCallType(string callType)
        => callType.Trim().ToLowerInvariant();

    private string GetUserId()
        => Context.User?.FindFirst("id")?.Value
           ?? Context.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
           ?? Context.ConnectionId;

    public sealed class CallStartResponse
    {
        public string CallId { get; init; } = string.Empty;

        public int MaxDurationSeconds { get; init; }
    }
 }
