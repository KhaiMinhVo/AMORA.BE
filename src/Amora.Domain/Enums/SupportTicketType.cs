namespace Amora.Domain.Enums;

public enum SupportTicketType
{
    PaymentIssue = 0,       // Vấn đề thanh toán
    BugReport = 1,          // Lỗi ứng dụng
    Other = 2,              // Khác
    VoiceError = 3,         // Lỗi âm thanh/Voice
    UserReport = 4,         // Báo cáo người dùng
    FeatureRequest = 5      // Đóng góp tính năng
}
