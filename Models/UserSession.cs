namespace RayanTask.Models;

public class UserSession
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public bool IsAdmin { get; set; } = false;
    public bool CanEdit { get; set; } = true;
    public bool CanDelete { get; set; } = true;
    public bool CanSelectAnyDate { get; set; } = false;
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
}

