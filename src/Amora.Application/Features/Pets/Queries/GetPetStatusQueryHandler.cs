using Amora.Application.Dtos.Pets;
using Amora.Application.Exceptions;
using Amora.Application.Pets;
using Amora.Domain.Interfaces;
using MediatR;

namespace Amora.Application.Features.Pets.Queries;

public sealed class GetPetStatusQueryHandler : IRequestHandler<GetPetStatusQuery, PetStatusDto>
{
    private readonly IPetRepository _petRepository;
    private readonly IMatchConnectionRepository _matchRepository;

    public GetPetStatusQueryHandler(IPetRepository petRepository, IMatchConnectionRepository matchRepository)
    {
        _petRepository = petRepository;
        _matchRepository = matchRepository;
    }

    public async Task<PetStatusDto> Handle(GetPetStatusQuery request, CancellationToken cancellationToken)
    {
        if (!await _matchRepository.IsParticipantAsync(request.MatchId, request.UserId, cancellationToken))
            throw new ForbiddenApiException("Not a participant of this match.");

        var pet = await _petRepository.GetByMatchIdAsync(request.MatchId, cancellationToken)
            ?? throw new NotFoundApiException("Pet not found.");

        return PetCoordinator.ToDto(pet);
    }
}
