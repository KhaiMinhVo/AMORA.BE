using System.ComponentModel.DataAnnotations;

namespace Amora.Application.Dtos.Admin;

public sealed class RefundDiamondsRequest
{
    [Required]
    [Range(1, 1000000, ErrorMessage = "Số lượng kim cương phải lớn hơn 0.")]
    public int Amount { get; set; }

    [Required]
    [MaxLength(500, ErrorMessage = "Lý do không được vượt quá 500 ký tự.")]
    public string Reason { get; set; } = string.Empty;
}
