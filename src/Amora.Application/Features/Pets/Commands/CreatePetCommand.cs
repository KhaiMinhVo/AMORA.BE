using Amora.Domain.Entities;
using MediatR;

namespace Amora.Application.Features.Pets.Commands;

public sealed record CreatePetCommand(Guid MatchId) : IRequest<Pet>;
