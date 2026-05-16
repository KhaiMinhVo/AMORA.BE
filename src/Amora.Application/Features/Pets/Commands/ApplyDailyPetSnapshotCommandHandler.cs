using Amora.Application.Pets;
using Amora.Domain.Interfaces;
using MediatR;

namespace Amora.Application.Features.Pets.Commands;

public sealed class ApplyDailyPetSnapshotCommandHandler : IRequestHandler<ApplyDailyPetSnapshotCommand, int>
{
    private readonly IPetRepository _petRepository;

    public ApplyDailyPetSnapshotCommandHandler(IPetRepository petRepository) => _petRepository = petRepository;

    public async Task<int> Handle(ApplyDailyPetSnapshotCommand request, CancellationToken cancellationToken)
    {
        var pets = await _petRepository.GetAllForDailySnapshotAsync(cancellationToken);
        var count = 0;

        foreach (var pet in pets)
        {
            PetEngine.RecordDailyHpSnapshot(pet);
            pet.Stage = PetEngine.EvaluateStage(pet);
            pet.UpdatedAt = DateTimeOffset.UtcNow;
            count++;
        }

        if (count > 0)
            await _petRepository.SaveChangesAsync(cancellationToken);

        return count;
    }
}
