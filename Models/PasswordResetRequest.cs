namespace RayanTask.Models;

/// <summary>
/// درخواست بازیابی رمز (مرحله قبل از پاسخ به سوال امنیتی) - معتبر ۱۰ دقیقه
/// </summary>
public class PasswordResetRequest
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public User User { get; set; } = null!;
    public string RequestToken { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
