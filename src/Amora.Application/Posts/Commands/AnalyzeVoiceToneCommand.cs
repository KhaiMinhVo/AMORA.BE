using Amora.Application.Services;
using Amora.Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Amora.Application.Posts.Commands;

public record AnalyzeVoiceToneCommand(Guid PostId) : IRequest;

public sealed class AnalyzeVoiceToneCommandHandler : IRequestHandler<AnalyzeVoiceToneCommand>
{
    private readonly IVoicePostRepository _voicePostRepository;
    private readonly AiVoiceAnalysisService _aiVoiceAnalysisService;
    private readonly ILogger<AnalyzeVoiceToneCommandHandler> _logger;

    public AnalyzeVoiceToneCommandHandler(
        IVoicePostRepository voicePostRepository,
        AiVoiceAnalysisService aiVoiceAnalysisService,
        ILogger<AnalyzeVoiceToneCommandHandler> logger)
    {
        _voicePostRepository = voicePostRepository;
        _aiVoiceAnalysisService = aiVoiceAnalysisService;
        _logger = logger;
    }

    public async Task Handle(AnalyzeVoiceToneCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var post = await _voicePostRepository.GetByIdAsync(request.PostId, cancellationToken);
            if (post == null || string.IsNullOrWhiteSpace(post.AudioUrl))
            {
                return;
            }

            var tone = await _aiVoiceAnalysisService.AnalyzeVoiceToneAsync(post.AudioUrl, cancellationToken);
            
            if (tone.HasValue)
            {
                post.Tone = tone.Value;
                await _voicePostRepository.UpdateAsync(post, cancellationToken);
                _logger.LogInformation("Successfully analyzed tone {Tone} for post {PostId}", tone.Value, request.PostId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to analyze voice tone for post {PostId}", request.PostId);
        }
    }
}
