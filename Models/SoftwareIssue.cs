using System.ComponentModel.DataAnnotations.Schema;

namespace RayanTask.Models;

public class SoftwareIssue
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty; // عنوان مشکل/نظر
    public string Description { get; set; } = string.Empty; // شرح کامل
    
    // Foreign Keys
    public int SoftwareNameId { get; set; } // Foreign Key به SoftwareName
    public int IssueTypeId { get; set; } // Foreign Key به IssueType
    public int ReporterId { get; set; } // Foreign Key به User (ثبت کننده)
    public int? AssignedToId { get; set; } // Foreign Key به User (مسئول - nullable)
    public int StatusId { get; set; } // Foreign Key به Status
    
    // Navigation Properties (با نام‌های متفاوت برای جلوگیری از تداخل)
    public SoftwareName SoftwareNameEntity { get; set; } = null!;
    public IssueType IssueTypeEntity { get; set; } = null!;
    public User ReporterEntity { get; set; } = null!;
    public User? AssignedToEntity { get; set; }
    public Status StatusEntity { get; set; } = null!;
    
    public string? CustomerName { get; set; } // نام مشتری (اختیاری)
    public string? BranchName { get; set; }
    public string? RepositoryUrl { get; set; }
    public string? CommitUrl { get; set; }
    public string? WorkItemType { get; set; }
    public string? GitStatus { get; set; }
    public string? Priority { get; set; }
    public string? Severity { get; set; }
    public DateTime? DueDate { get; set; }
    public string? Environment { get; set; }
    public string? ReportedChannel { get; set; }
    public string? ReportedChannelUrl { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now; // زمان ثبت
    public DateTime? StartedAt { get; set; } // زمان شروع (nullable)
    public DateTime? CompletedAt { get; set; } // زمان پایان (nullable)
    
    // Computed Properties برای سازگاری با کدهای موجود (NotMapped)
    [NotMapped]
    public string SoftwareName => SoftwareNameEntity?.Value ?? string.Empty;
    
    [NotMapped]
    public string IssueType => IssueTypeEntity?.Value ?? string.Empty;
    
    [NotMapped]
    public string ReporterFirstName => ReporterEntity?.FirstName ?? string.Empty;
    
    [NotMapped]
    public string ReporterLastName => ReporterEntity?.LastName ?? string.Empty;
    
    [NotMapped]
    public string? AssignedToFirstName => AssignedToEntity?.FirstName;
    
    [NotMapped]
    public string? AssignedToLastName => AssignedToEntity?.LastName;
    
    [NotMapped]
    public string Status => StatusEntity?.Value ?? "ثبت شده";
    
    // محاسبه مدت زمان انجام (به دقیقه) - NotMapped برای EF Core
    [NotMapped]
    public int? DurationMinutes
    {
        get
        {
            if (StartedAt.HasValue && CompletedAt.HasValue)
            {
                return (int)(CompletedAt.Value - StartedAt.Value).TotalMinutes;
            }
            return null;
        }
    }
    
    // برای نمایش مدت زمان به صورت خوانا
    public string GetDurationDisplay()
    {
        if (!DurationMinutes.HasValue)
            return "-";
        
        var minutes = DurationMinutes.Value;
        if (minutes < 60)
            return $"{minutes} دقیقه";
        
        var hours = minutes / 60;
        var remainingMinutes = minutes % 60;
        
        if (remainingMinutes == 0)
            return $"{hours} ساعت";
        
        return $"{hours} ساعت و {remainingMinutes} دقیقه";
    }
    
    // برای نمایش وضعیت با رنگ
    public string GetStatusBadgeClass()
    {
        return Status switch
        {
            "ثبت شده" => "bg-secondary",
            "در حال بررسی" => "bg-info",
            "در حال انجام" => "bg-warning",
            "تکمیل شده" => "bg-success",
            "لغو شده" => "bg-danger",
            _ => "bg-secondary"
        };
    }
}
