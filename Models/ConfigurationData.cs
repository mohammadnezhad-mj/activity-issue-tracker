namespace RayanTask.Models;

public class ConfigurationData
{
    public List<NameItem> Names { get; set; } = new();
    public List<ConfigItem> ActivityTypes { get; set; } = new();
    public List<ConfigItem> Results { get; set; } = new();
    public List<ConfigItem> SoftwareNames { get; set; } = new();
    public List<ConfigItem> IssueTypes { get; set; } = new();
    public RateLimitSettings? RateLimitSettings { get; set; }
}

public class RateLimitSettings
{
    public int MaxRequests { get; set; } = 5; // حداکثر تعداد درخواست
    public int TimeWindowMinutes { get; set; } = 15; // بازه زمانی به دقیقه
}

public class NameItem
{
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
}

public class ConfigItem
{
    public int Id { get; set; }
    public string Value { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
}
