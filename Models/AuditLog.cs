namespace RayanTask.Models;

public class AuditLog
{
    public int Id { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.Now;
    public string UserFirstName { get; set; } = string.Empty;
    public string UserLastName { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty; // Create, Update, Delete, ToggleStatus, etc.
    public string EntityType { get; set; } = string.Empty; // ActivityRecord, User, ActivityType, Result
    public string EntityId { get; set; } = string.Empty; // ID or identifier of the entity
    public string Description { get; set; } = string.Empty; // Detailed description of the change
    public string OldValue { get; set; } = string.Empty; // JSON of old values (optional)
    public string NewValue { get; set; } = string.Empty; // JSON of new values (optional)
}

