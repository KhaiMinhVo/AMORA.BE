using Amora.Application.Common;
using Amora.Application.Dtos.Pets;
using Amora.Application.Features.Pets.Queries;
using Amora.Application.Abstractions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Amora.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/transactions")]
public sealed class TransactionsController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ICurrentUserService _currentUser;

    public TransactionsController(IMediator mediator, ICurrentUserService currentUser)
    {
        _mediator = mediator;
        _currentUser = currentUser;
    }

    /// <summary>
    /// Lay lich su giao dich cua user, gioi han so luong.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<TransactionDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<TransactionDto>>>> GetTransactions(
        [FromQuery] int limit = 50,
        CancellationToken cancellationToken = default)
    {
        var rows = await _mediator.Send(new GetTransactionsQuery(_currentUser.UserId, limit), cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<TransactionDto>>.Ok(rows));
    }
}
