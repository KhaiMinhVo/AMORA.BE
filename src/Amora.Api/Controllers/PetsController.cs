using Amora.Application.Common;
using Amora.Application.Dtos.Pets;
using Amora.Application.Features.Pets.Commands;
using Amora.Application.Features.Pets.Queries;
using Amora.Application.Abstractions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Amora.Api.Controllers;

/// <summary>
/// Thu cung ao theo match (HP, Mood, RP, tien hoa).
/// Cung cap API xem trang thai va dung item.
/// </summary>
[ApiController]
[Authorize]
[Route("api/pets")]
public sealed class PetsController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ICurrentUserService _currentUser;

    public PetsController(IMediator mediator, ICurrentUserService currentUser)
    {
        _mediator = mediator;
        _currentUser = currentUser;
    }

    /// <summary>
    /// Lay trang thai Pet cua match.
    /// Tra ve cac chi so hien tai cua Pet.
    /// </summary>
    [HttpGet("{matchId:guid}/status")]
    [ProducesResponseType(typeof(ApiResponse<PetStatusDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<PetStatusDto>>> GetStatus(Guid matchId, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetPetStatusQuery(matchId, _currentUser.UserId), cancellationToken);
        return Ok(ApiResponse<PetStatusDto>.Ok(result));
    }

    /// <summary>
    /// Lấy lịch sử chăm sóc thú cưng của match.
    /// </summary>
    [HttpGet("{matchId:guid}/activities")]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<PetActivityDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<PagedResult<PetActivityDto>>>> GetActivities(
        Guid matchId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(new GetPetActivitiesQuery(matchId, _currentUser.UserId, page, pageSize), cancellationToken);
        return Ok(ApiResponse<PagedResult<PetActivityDto>>.Ok(result));
    }

    /// <summary>
    /// Su dung item tu inventory len Pet cua match.
    /// Cap nhat trang thai Pet sau khi ap dung.
    /// </summary>
    [HttpPost("{matchId:guid}/use-item")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<object>>> UseItem(
        Guid matchId,
        [FromBody] UseItemRequest request,
        CancellationToken cancellationToken)
    {
        await _mediator.Send(new UseItemCommand(_currentUser.UserId, matchId, request.ItemId), cancellationToken);
        return Ok(ApiResponse<object>.Ok(null, "Item used successfully."));
    }
    /// <summary>
    /// Nhận nước miễn phí (Tối đa 3 lần/ngày, cách nhau 1 giờ).
    /// </summary>
    [HttpPost("{matchId:guid}/claim-water")]
    [ProducesResponseType(typeof(ApiResponse<WaterClaimResultDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<WaterClaimResultDto>>> ClaimWater(
        Guid matchId,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new ClaimWaterCommand(_currentUser.UserId, matchId), cancellationToken);
        return Ok(ApiResponse<WaterClaimResultDto>.Ok(result, "Nhận nước thành công. Pet nhận thêm 10 EXP."));
    }

    /// <summary>
    /// Sử dụng Thẻ Đổi Tên.
    /// </summary>
    [HttpPost("{matchId:guid}/rename")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<object>>> RenamePet(
        Guid matchId,
        [FromBody] RenamePetRequest request,
        CancellationToken cancellationToken)
    {
        await _mediator.Send(new RenamePetCommand(_currentUser.UserId, matchId, request.ItemId, request.NewName), cancellationToken);
        return Ok(ApiResponse<object>.Ok(null, "Đổi tên Thú cưng thành công."));
    }

    /// <summary>
    /// Mặc phụ kiện cho Thú cưng.
    /// </summary>
    [HttpPost("{matchId:guid}/equip-cosmetic")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<object>>> EquipCosmetic(
        Guid matchId,
        [FromBody] EquipCosmeticRequest request,
        CancellationToken cancellationToken)
    {
        await _mediator.Send(new EquipCosmeticCommand(_currentUser.UserId, matchId, request.ItemId), cancellationToken);
        return Ok(ApiResponse<object>.Ok(null, "Mặc phụ kiện thành công."));
    }

    /// <summary>
    /// Đặt tên ban đầu cho Pet (Chỉ dùng khi Pet đang ở dạng Trứng và chưa có tên).
    /// </summary>
    [HttpPost("{matchId:guid}/initial-name")]
    [ProducesResponseType(typeof(ApiResponse<SetInitialPetNameResult>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<SetInitialPetNameResult>>> SetInitialName(
        Guid matchId,
        [FromBody] SetInitialPetNameRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new SetInitialPetNameCommand(_currentUser.UserId, matchId, request.Name), cancellationToken);
        return Ok(ApiResponse<SetInitialPetNameResult>.Ok(result, "Initial pet name set successfully."));
    }
}

public sealed class RenamePetRequest
{
    public Guid ItemId { get; set; }
    public string NewName { get; set; } = string.Empty;
}

public sealed class EquipCosmeticRequest
{
    public Guid ItemId { get; set; }
}
