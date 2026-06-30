using Amora.Domain.Enums;
using Amora.Domain.Interfaces;
using Amora.Application.Services;
using Amora.Application.Abstractions;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Amora.Application.Pets.Commands;

public sealed record AnalyzeConversationEmotionCommand(Guid MatchId) : IRequest;

public sealed class AnalyzeConversationEmotionCommandHandler : IRequestHandler<AnalyzeConversationEmotionCommand>
{
    private readonly IPetRepository _petRepository;
    private readonly IChatMessageRepository _chatMessageRepository;
    private readonly AiEmotionAnalysisService _emotionService;
    private readonly AiModerationService _moderationService;
    private readonly IPetRealtimeNotifier _petNotifier;
    private readonly IMatchConnectionRepository _matchRepository;
    private readonly ILogger<AnalyzeConversationEmotionCommandHandler> _logger;

    public AnalyzeConversationEmotionCommandHandler(
        IPetRepository petRepository,
        IChatMessageRepository chatMessageRepository,
        AiEmotionAnalysisService emotionService,
        AiModerationService moderationService,
        IPetRealtimeNotifier petNotifier,
        IMatchConnectionRepository matchRepository,
        ILogger<AnalyzeConversationEmotionCommandHandler> logger)
    {
        _petRepository = petRepository;
        _chatMessageRepository = chatMessageRepository;
        _emotionService = emotionService;
        _moderationService = moderationService;
        _petNotifier = petNotifier;
        _matchRepository = matchRepository;
        _logger = logger;
    }

    public async Task Handle(AnalyzeConversationEmotionCommand request, CancellationToken cancellationToken)
    {
        var pet = await _petRepository.GetByMatchIdAsync(request.MatchId, cancellationToken);
        if (pet == null) return;

        pet.UnanalyzedMessageCount++;

        if (pet.UnanalyzedMessageCount < 5)
        {
            pet.UpdatedAt = DateTimeOffset.UtcNow;
            await _petRepository.SaveChangesAsync(cancellationToken);
            return;
        }

        // Reset the counter
        pet.UnanalyzedMessageCount = 0;

        // Get last 5 messages
        var messagesResult = await _chatMessageRepository.GetByMatchAsync(request.MatchId, null, 5, cancellationToken);
        var messages = messagesResult.Items.OrderBy(m => m.CreatedAt).ToList();

        if (messages.Count == 0) return;

        var conversationLines = new List<string>();

        foreach (var msg in messages)
        {
            var senderName = msg.SenderId.HasValue ? msg.SenderId.Value.ToString() : "System";
            var text = msg.Content;

            // If voice message has no text, try to transcribe it
            if (msg.MessageType == MessageType.Voice && string.IsNullOrWhiteSpace(text) && !string.IsNullOrWhiteSpace(msg.ContentUrl))
            {
                text = await _moderationService.TranscribeAudioAsync(msg.ContentUrl, cancellationToken);
                // We won't save it back to DB based on user preference (Dịch ngầm)
            }

            if (!string.IsNullOrWhiteSpace(text))
            {
                conversationLines.Add($"[{msg.CreatedAt:HH:mm}] User {senderName}: {text}");
            }
            else if (msg.MessageType == MessageType.Image)
            {
                conversationLines.Add($"[{msg.CreatedAt:HH:mm}] User {senderName}: [Sent an Image]");
            }
        }

        var fullConversation = string.Join("\n", conversationLines);
        if (string.IsNullOrWhiteSpace(fullConversation)) return;

        var emotion = await _emotionService.AnalyzeConversationEmotionAsync(fullConversation, cancellationToken);

        if (emotion != PetEmotion.Neutral && pet.CurrentEmotion != emotion)
        {
            pet.CurrentEmotion = emotion;
            pet.UpdatedAt = DateTimeOffset.UtcNow;
            
            await _petRepository.SaveChangesAsync(cancellationToken);

            var match = await _matchRepository.GetByIdAsync(request.MatchId, cancellationToken);
            if (match != null)
            {
                await _petNotifier.NotifyPetStatusUpdatedAsync(pet, match, cancellationToken);
            }
            
            _logger.LogInformation("Pet emotion updated to {Emotion} for Match {MatchId}", emotion, request.MatchId);
        }
    }
}
