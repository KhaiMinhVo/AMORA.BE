using System;
using System.Threading;
using System.Threading.Tasks;
using Amora.Application.Common;
using Amora.Domain.Entities;
using Amora.Domain.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Amora.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/ads")]
public sealed class AdMonetizationController : ControllerBase
{
    private readonly IUserRepository _userRepository;
    private readonly IPetTransactionRepository _transactionRepository;

    public AdMonetizationController(IUserRepository userRepository, IPetTransactionRepository transactionRepository)
    {
        _userRepository = userRepository;
        _transactionRepository = transactionRepository;
    }

    /// <summary>
    /// Nhan thuong sau khi xem quang cao
    /// </summary>
    [HttpPost("claim-reward")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<object>>> ClaimAdReward(CancellationToken cancellationToken)
    {
        var userIdStr = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
            ?? User.FindFirst("id")?.Value;

        if (string.IsNullOrEmpty(userIdStr) || !Guid.TryParse(userIdStr, out var userId))
        {
            return Unauthorized();
        }

        var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
        if (user == null)
            return Unauthorized();

        // 5 Diamonds per ad view
        int rewardDiamonds = 5;
        
        user.Diamonds += rewardDiamonds;
        await _userRepository.UpdateAsync(user, cancellationToken);

        await _transactionRepository.AddAsync(new PetTransaction
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            ShopItemId = null,
            TransactionType = "Ad Reward",
            DiamondsDelta = rewardDiamonds,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        }, cancellationToken);

        await _transactionRepository.SaveChangesAsync(cancellationToken);

        return Ok(ApiResponse<object>.Ok(new { rewardDiamonds, currentDiamonds = user.Diamonds }, "Successfully claimed ad reward."));
    }
}
