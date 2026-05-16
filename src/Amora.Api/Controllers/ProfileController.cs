using Amora.Application.Dtos.Profile;
using Amora.Application.Services;
using Microsoft.AspNetCore.Mvc;

namespace Amora.Api.Controllers;

[ApiController]
[Route("api/profile")]
public sealed class ProfileController : ControllerBase
{
    private readonly ProfileService _profileService;

    public ProfileController(ProfileService profileService)
    {
        _profileService = profileService;
    }

    /// <summary>Lấy profile của chính mình.</summary>
    [HttpGet("me")]
    public async Task<IActionResult> GetMyProfile(CancellationToken cancellationToken)
    {
        var result = await _profileService.GetMyProfileAsync(cancellationToken);
        return Ok(new { success = true, data = result });
    }

    /// <summary>Cập nhật profile của chính mình.</summary>
    [HttpPut("me")]
    public async Task<IActionResult> UpdateMyProfile(
        [FromBody] UpdateProfileRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _profileService.UpdateMyProfileAsync(request, cancellationToken);
        return Ok(new { success = true, data = result });
    }

    /// <summary>Xem profile công khai của user khác (avatar blur nếu chưa match).</summary>
    [HttpGet("{userId:guid}")]
    public async Task<IActionResult> GetPublicProfile(Guid userId, CancellationToken cancellationToken)
    {
        var result = await _profileService.GetPublicProfileAsync(userId, cancellationToken);
        return Ok(new { success = true, data = result });
    }
}
