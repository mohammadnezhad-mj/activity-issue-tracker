namespace RayanTask.Models;

public class ActivityType
{
    public int Id { get; set; }
    public string Value { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime? UpdatedAt { get; set; }
}



