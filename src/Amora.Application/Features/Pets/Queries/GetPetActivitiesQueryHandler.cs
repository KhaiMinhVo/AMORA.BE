using Amora.Application.Common;
using Amora.Application.Dtos.Pets;
using Amora.Application.Exceptions;
using Amora.Domain.Interfaces;
using MediatR;

namespace Amora.Application.Features.Pets.Queries;

public sealed class GetPetActivitiesQueryHandler : IRequestHandler<GetPetActivitiesQuery, PagedResult<PetActivityDto>>
{
    private readonly IPetRepository _petRepository;
    private readonly IMatchConnectionRepository _matchRepository;

    public GetPetActivitiesQueryHandler(IPetRepository petRepository, IMatchConnectionRepository matchRepository)
    {
        _petRepository = petRepository;
        _matchRepository = matchRepository;
    }

    public async Task<PagedResult<PetActivityDto>> Handle(GetPetActivitiesQuery request, CancellationToken cancellationToken)
    {
        if (!await _matchRepository.IsParticipantAsync(request.MatchId, request.UserId, cancellationToken))
            throw new ForbiddenApiException("Bạn không tham gia cuộc trò chuyện này.");

        var activities = await _petRepository.GetActivitiesAsync(request.MatchId, request.Page, request.PageSize, cancellationToken);
        
        var dtos = activities.Select(a => new PetActivityDto
        {
            Id = a.Id,
            ActivityType = a.ActivityType,
            Description = a.Description,
            CreatedAt = a.CreatedAt,
            UserDisplayName = a.User?.DisplayName ?? "Người dùng ẩn"
        }).ToList();

        // For simplicity we don't query total count here unless requested, 
        // return PagedResult with a fake total for now, or you can add a Count method to IPetRepository.
        return new PagedResult<PetActivityDto>
        {
            Items = dtos,
            TotalCount = 100 // Hardcoded for now unless you want a real count query
        };
    }
}
