using Amora.Application.Dtos.Admin;
using Amora.Application.Dtos.Support;
using Amora.Application.Exceptions;
using Amora.Application.Payment.PayOs;
using Amora.Application.Services;
using Amora.Domain.Entities;
using Amora.Domain.Enums;
using Amora.Domain.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Amora.Api.Controllers;

[ApiController]
[Route("api/admin")]
[Authorize(Roles = "Admin")]
public class AdminSupportController : ControllerBase
{
    private readonly SupportTicketService _supportTicketService;
    private readonly PayOsService _payOsService;
    private readonly IPaymentTransactionRepository _paymentTransactionRepository;
    private readonly ISupportTicketRepository _supportTicketRepository;
    private readonly NotificationService _notificationService;

    public AdminSupportController(
        SupportTicketService supportTicketService,
        PayOsService payOsService,
        IPaymentTransactionRepository paymentTransactionRepository,
        ISupportTicketRepository supportTicketRepository,
        NotificationService notificationService)
    {
        _supportTicketService = supportTicketService;
        _payOsService = payOsService;
        _paymentTransactionRepository = paymentTransactionRepository;
        _supportTicketRepository = supportTicketRepository;
        _notificationService = notificationService;
    }

    [HttpGet("support-tickets")]
    public async Task<IActionResult> GetTickets([FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken cancellationToken = default)
    {
        var result = await _supportTicketService.GetTicketsForAdminAsync(page, pageSize, cancellationToken);
        return Ok(result);
    }

    [HttpGet("payments/order/{orderCode}/payos-status")]
    public async Task<IActionResult> GetPayOsStatus(long orderCode)
    {
        var status = await _payOsService.CheckPayOsStatusAsync(orderCode);
        if (status == null)
            return NotFound(new { message = "Không tìm thấy thông tin trên PayOS hoặc có lỗi xảy ra." });

        return Ok(status);
    }

    [HttpPost("support-tickets/{ticketId}/resolve-payment")]
    public async Task<IActionResult> ResolvePaymentTicket(Guid ticketId, CancellationToken cancellationToken)
    {
        var ticket = await _supportTicketRepository.GetByIdForUpdateAsync(ticketId, cancellationToken)
            ?? throw new NotFoundApiException("Không tìm thấy khiếu nại.");

        if (ticket.Type != SupportTicketType.PaymentIssue)
            return BadRequest(new { message = "Khiếu nại này không phải là lỗi nạp tiền." });

        if (ticket.Status == SupportTicketStatus.Resolved || ticket.Status == SupportTicketStatus.Closed)
            return BadRequest(new { message = "Khiếu nại đã được xử lý." });

        if (!long.TryParse(ticket.ReferenceId, out var orderCode))
            return BadRequest(new { message = "ReferenceId không hợp lệ (cần mã đơn hàng số)." });

        // Tìm transaction
        var targetTransaction = await _paymentTransactionRepository.GetByOrderCodeAsync(orderCode, cancellationToken);

        if (targetTransaction == null || targetTransaction.Status != PaymentTransactionStatus.Pending)
        {
            return BadRequest(new { message = "Không tìm thấy đơn hàng Pending nào với mã này. Có thể hệ thống (Worker) đã quét và cộng tiền rồi." });
        }

        // Gọi hàm của PayOsService (sẽ xử lý logic cộng tiền và thông báo, và xử lý DbUpdateConcurrencyException)
        await _payOsService.ReconcilePendingTransactionAsync(targetTransaction, cancellationToken);

        // Sau khi xử lý xong, update trạng thái ticket
        ticket.Status = SupportTicketStatus.Resolved;
        ticket.ResolvedAt = DateTimeOffset.UtcNow;
        await _supportTicketRepository.UpdateAsync(ticket, cancellationToken);

        return Ok(new { message = "Đã xác nhận và đóng khiếu nại." });
    }

    [HttpPost("support-tickets/{ticketId}/resolve")]
    public async Task<IActionResult> ResolveGeneralTicket(
        Guid ticketId, 
        [FromBody] ResolveGeneralTicketRequest request, 
        CancellationToken cancellationToken)
    {
        var ticket = await _supportTicketRepository.GetByIdForUpdateAsync(ticketId, cancellationToken)
            ?? throw new NotFoundApiException("Không tìm thấy yêu cầu hỗ trợ.");

        ticket.Status = request.Status;
        ticket.ResolvedAt = request.Status == SupportTicketStatus.Resolved ? DateTimeOffset.UtcNow : ticket.ResolvedAt;
        ticket.ResolutionNote = request.ResolutionNote;

        await _supportTicketRepository.UpdateAsync(ticket, cancellationToken);

        // Notify the user who created the ticket
        var bodyMsg = $"Trạng thái: {request.Status}";
        if (!string.IsNullOrWhiteSpace(request.ResolutionNote))
        {
            bodyMsg += $"\nPhản hồi từ Admin: {request.ResolutionNote}";
        }

        var payload = System.Text.Json.JsonSerializer.Serialize(new
        {
            ticketId = ticket.Id.ToString(),
            status = request.Status.ToString()
        });

        await _notificationService.SendNotificationAsync(
            ticket.UserId,
            NotificationType.System,
            "Yêu cầu hỗ trợ đã được cập nhật",
            bodyMsg,
            payload,
            cancellationToken);

        return Ok(new { success = true, message = "Đã cập nhật trạng thái yêu cầu hỗ trợ." });
    }
}
