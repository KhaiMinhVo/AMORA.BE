using Amora.Application.Abstractions;
using Amora.Application.Iap;
using Amora.Application.Pets;
using Amora.Domain.Interfaces;
using MediatR;

namespace Amora.Application.Features.Pets.Commands;

public sealed class ProcessCoPresenceCommandHandler : IRequestHandler<ProcessCoPresenceCommand, int>
{
    private readonly IPetRepository _petRepository;
    private readonly IMatchConnectionRepository _matchRepository;
    private readonly IPetRealtimeNotifier _petNotifier;
    private readonly PetCoinRewardService _petCoins;

    public ProcessCoPresenceCommandHandler(
        IPetRepository petRepository,
        IMatchConnectionRepository matchRepository,
        IPetRealtimeNotifier petNotifier,
        PetCoinRewardService petCoins)
    {
        _petRepository = petRepository;
        _matchRepository = matchRepository;
        _petNotifier = petNotifier;
        _petCoins = petCoins;
    }

    public async Task<int> Handle(ProcessCoPresenceCommand request, CancellationToken cancellationToken)
    {
        var pet = await _petRepository.GetByMatchIdAsync(request.MatchId, cancellationToken);
        if (pet is null) return 0;

        var gained = PetEngine.AwardOnlineRp(pet);
        if (gained <= 0) return 0;

        pet.Stage = PetEngine.EvaluateStage(pet);
        pet.UpdatedAt = DateTimeOffset.UtcNow;
        await _petRepository.SaveChangesAsync(cancellationToken);

        var match = await _matchRepository.GetByIdAsync(request.MatchId, cancellationToken);
        if (match is not null)
        {
            await _petCoins.TryGrantCoPresenceCoinsAsync(match.UserAId, cancellationToken);
            await _petCoins.TryGrantCoPresenceCoinsAsync(match.UserBId, cancellationToken);
            await _petNotifier.NotifyPetStatusUpdatedAsync(pet, match, cancellationToken);
        }

        return gained;
    }
}
