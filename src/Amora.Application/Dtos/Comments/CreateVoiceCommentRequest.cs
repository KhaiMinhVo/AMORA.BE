namespace Amora.Application.Dtos.Comments;

public sealed class CreateVoiceCommentRequest
{
    public string AudioUrl { get; set; } = string.Empty;

    public int Duration { get; set; }
}