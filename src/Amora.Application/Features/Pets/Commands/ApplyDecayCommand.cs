using MediatR;

namespace Amora.Application.Features.Pets.Commands;

public sealed record ApplyDecayCommand : IRequest<int>;
