using Amora.Application.Dtos.Pets;
using MediatR;

namespace Amora.Application.Features.Pets.Commands;

public sealed record BuyItemCommand(Guid UserId, BuyItemRequest Request) : IRequest;
