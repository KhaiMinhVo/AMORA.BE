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

    /// <summary>
    /// Lay profile cua chinh minh.
    /// Tra ve thong tin ho so hien tai.
    /// </summary>
    [HttpGet("me")]
    public async Task<IActionResult> GetMyProfile(CancellationToken cancellationToken)
    {
        var result = await _profileService.GetMyProfileAsync(cancellationToken);
        return Ok(new { success = true, data = result });
    }

    /// <summary>
    /// Cap nhat profile cua chinh minh.
    /// Tra ve thong tin sau khi cap nhat.
    /// </summary>
    [HttpPut("me")]
    public async Task<IActionResult> UpdateMyProfile(
        [FromBody] UpdateProfileRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _profileService.UpdateMyProfileAsync(request, cancellationToken);
        return Ok(new { success = true, data = result });
    }

    /// <summary>
    /// Xem profile cong khai cua user khac.
    /// Avatar co the bi blur neu chua match.
    /// </summary>
    [HttpGet("{userId:guid}")]
    public async Task<IActionResult> GetPublicProfile(Guid userId, CancellationToken cancellationToken)
    {
        var result = await _profileService.GetPublicProfileAsync(userId, cancellationToken);
        return Ok(new { success = true, data = result });
    }

    /// <summary>
    /// Diem danh hang ngay de nhan Kim Cuong va Trust Score.
    /// </summary>
    [HttpPost("attendance")]
    public async Task<IActionResult> ClaimAttendance(CancellationToken cancellationToken)
    {
        var result = await _profileService.ClaimAttendanceAsync(cancellationToken);
        return Ok(new { success = true, data = result, message = "Điểm danh thành công!" });
    }
}
