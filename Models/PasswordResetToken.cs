namespace RayanTask.Models;

/// <summary>
/// توکن بازیابی رمز عبور - معتبر برای مدت محدود (مثلاً ۱ ساعت)
/// </summary>
public class PasswordResetToken
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public User User { get; set; } = null!;
    public string Token { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
