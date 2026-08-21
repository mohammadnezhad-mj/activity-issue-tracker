namespace RayanTask.Models;

// جدول میانی برای رابطه Many-to-Many بین ActivityRecord و ActivityType
public class ActivityRecordActivityType
{
    public int ActivityRecordId { get; set; }
    public ActivityRecord ActivityRecord { get; set; } = null!;
    
    public int ActivityTypeId { get; set; }
    public ActivityType ActivityType { get; set; } = null!;
}

