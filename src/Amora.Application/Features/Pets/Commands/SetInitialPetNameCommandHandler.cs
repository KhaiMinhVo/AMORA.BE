using Amora.Application.Abstractions;
using Amora.Application.Exceptions;
using Amora.Domain.Entities;
using Amora.Domain.Enums;
using Amora.Domain.Interfaces;
using MediatR;

namespace Amora.Application.Features.Pets.Commands;

public sealed class SetInitialPetNameCommandHandler : IRequestHandler<SetInitialPetNameCommand, SetInitialPetNameResult>
{
    private readonly IPetRepository _petRepository;
    private readonly IMatchConnectionRepository _matchRepository;
    private readonly IPetRealtimeNotifier _petNotifier;

    public SetInitialPetNameCommandHandler(
        IPetRepository petRepository,
        IMatchConnectionRepository matchRepository,
        IPetRealtimeNotifier petNotifier)
    {
        _petRepository = petRepository;
        _matchRepository = matchRepository;
        _petNotifier = petNotifier;
    }

    public async Task<SetInitialPetNameResult> Handle(SetInitialPetNameCommand request, CancellationToken cancellationToken)
    {
        if (!await _matchRepository.IsParticipantAsync(request.MatchId, request.UserId, cancellationToken))
            throw new ForbiddenApiException("MATCH_ACCESS_DENIED", "MATCH_ACCESS_DENIED"); // The user mentioned errorCode so we pass it.

        var pet = await _petRepository.GetByMatchIdAsync(request.MatchId, cancellationToken)
            ?? throw new NotFoundApiException("PET_NOT_FOUND", "PET_NOT_FOUND");

        if (pet.Stage != GrowthStage.ResonanceSeed)
            throw new ValidationApiException("PET_NOT_IN_EGG_STAGE", "PET_NOT_IN_EGG_STAGE");

        if (!string.IsNullOrWhiteSpace(pet.Name))
            throw new ValidationApiException("PET_ALREADY_NAMED", "PET_ALREADY_NAMED");

        var newName = request.Name?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(newName) || newName.Length > 30)
            throw new ValidationApiException("PET_NAME_INVALID", "PET_NAME_INVALID");

        pet.Name = newName;
        pet.UpdatedAt = DateTimeOffset.UtcNow;

        await _petRepository.AddActivityAsync(new PetActivity
        {
            Id = Guid.NewGuid(),
            MatchId = request.MatchId,
            PetId = pet.Id,
            UserId = request.UserId,
            ActivityType = "initial_name",
            Description = $"Đã đặt tên Thú cưng là {newName}",
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        }, cancellationToken);

        await _petRepository.SaveChangesAsync(cancellationToken);

        var match = await _matchRepository.GetByIdAsync(request.MatchId, cancellationToken);
        if (match is not null)
            await _petNotifier.NotifyPetStatusUpdatedAsync(pet, match, cancellationToken);

        return new SetInitialPetNameResult(request.MatchId, newName);
    }
}
