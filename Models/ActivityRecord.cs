namespace RayanTask.Models;

public class ActivityRecord
{
    public int Id { get; set; }
    
    // Foreign Key به User
    public int UserId { get; set; }
    public User User { get; set; } = null!;
    
    public DateTime Date { get; set; } = DateTime.Now;
    
    // Many-to-Many relationship با ActivityType
    public ICollection<ActivityType> ActivityTypes { get; set; } = new List<ActivityType>();
    
    public string Description { get; set; } = string.Empty;
    public int DurationMinutes { get; set; }
    public string Stakeholder { get; set; } = string.Empty;
    
    // Foreign Key به Result
    public int ResultId { get; set; }
    public Result Result { get; set; } = null!;

    // Helper methods برای کار با ActivityType (برای backward compatibility)
    [System.ComponentModel.DataAnnotations.Schema.NotMapped]
    public string Name
    {
        get => User?.FirstName ?? string.Empty;
        set { } // فقط برای backward compatibility
    }

    [System.ComponentModel.DataAnnotations.Schema.NotMapped]
    public string LastName
    {
        get => User?.LastName ?? string.Empty;
        set { } // فقط برای backward compatibility
    }

    // برای نمایش در UI
    public string GetActivityTypesDisplay()
    {
        if (ActivityTypes == null || !ActivityTypes.Any())
            return string.Empty;
        
        return string.Join("، ", ActivityTypes.Select(at => at.Value));
    }

    // Helper method برای backward compatibility
    public List<string> GetActivityTypes()
    {
        if (ActivityTypes == null || !ActivityTypes.Any())
            return new List<string>();
        
        return ActivityTypes.Select(at => at.Value).ToList();
    }

    // Helper method برای backward compatibility (دیگر استفاده نمی‌شود، اما برای جلوگیری از خطا نگه داشته شده)
    public void SetActivityTypes(List<string> activityTypeValues)
    {
        // این متد دیگر استفاده نمی‌شود، چون ActivityTypes از طریق relation تنظیم می‌شود
        // اما برای جلوگیری از خطا در کدهای قدیمی نگه داشته شده
    }
}

