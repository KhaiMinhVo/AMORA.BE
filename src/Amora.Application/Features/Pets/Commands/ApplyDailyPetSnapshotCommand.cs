using MediatR;

namespace Amora.Application.Features.Pets.Commands;

public sealed record ApplyDailyPetSnapshotCommand : IRequest<int>;
