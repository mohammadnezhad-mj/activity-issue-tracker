namespace RayanTask.Models;

public class User
{
    public int Id { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public bool IsAdmin { get; set; } = false;
    public bool CanEdit { get; set; } = true;
    public bool CanDelete { get; set; } = true;
    public bool CanSelectAnyDate { get; set; } = false;
    public bool IsActive { get; set; } = true;
    // دسترسی‌های بخشی
    public bool CanManageUsers { get; set; } = false;
    public bool CanManageActivityTypes { get; set; } = false;
    public bool CanManageResults { get; set; } = false;
    public bool CanViewReports { get; set; } = false;
    public bool CanViewLogs { get; set; } = false;
    public bool CanViewActivityList { get; set; } = false;
    public bool CanViewIssueDetails { get; set; } = false;
    public bool CanEditIssues { get; set; } = false;
    public bool CanDeleteIssues { get; set; } = false;
    public bool CanDeleteReports { get; set; } = false;
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime? UpdatedAt { get; set; }

    /// <summary>
    /// سوال امنیتی برای بازیابی رمز عبور (در بخش پروفایل/تنظیمات تنظیم می‌شود)
    /// </summary>
    public string? SecurityQuestion { get; set; }

    /// <summary>
    /// Hash جواب سوال امنیتی (برای مقایسه امن در بازیابی رمز)
    /// </summary>
    public string? SecurityAnswerHash { get; set; }
}


