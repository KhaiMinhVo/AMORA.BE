using Amora.Application.Messaging;
using MediatR;

namespace Amora.Application.Features.Pets.Commands;

public sealed record UpdatePetAfterVoiceCommand(ChatVibeResultMessage Result) : IRequest;
