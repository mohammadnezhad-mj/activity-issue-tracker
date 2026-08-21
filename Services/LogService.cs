using RayanTask.Data;
using RayanTask.Models;

namespace RayanTask.Services;

public class LogService
{
    private readonly ApplicationDbContext _context;

    public LogService(ApplicationDbContext context)
    {
        _context = context;
    }

    public void LogAction(string userFirstName, string userLastName, string action, string entityType, 
        string entityId, string description, string? oldValue = null, string? newValue = null)
    {
        try
        {
            var log = new AuditLog
            {
                Timestamp = DateTime.Now,
                UserFirstName = userFirstName ?? string.Empty,
                UserLastName = userLastName ?? string.Empty,
                Action = action ?? string.Empty,
                EntityType = entityType ?? string.Empty,
                EntityId = entityId ?? string.Empty,
                Description = description ?? string.Empty,
                OldValue = oldValue ?? string.Empty,
                NewValue = newValue ?? string.Empty
            };

            _context.AuditLogs.Add(log);
            _context.SaveChanges();
        }
        catch (Exception ex)
        {
            // در صورت خطا، لاگ را نادیده بگیر (نباید عملیات اصلی را مختل کند)
            // اما می‌توانیم خطا را در Console لاگ کنیم برای دیباگ
            Console.WriteLine($"Error logging action: {ex.Message}");
            Console.WriteLine($"Stack trace: {ex.StackTrace}");
        }
    }

    public List<AuditLog> GetLogs(int? limit = null)
    {
        try
        {
            var query = _context.AuditLogs.OrderByDescending(l => l.Timestamp).AsQueryable();
            
            if (limit.HasValue)
            {
                query = query.Take(limit.Value);
            }

            return query.ToList();
        }
        catch
        {
            return new List<AuditLog>();
        }
    }

    public List<AuditLog> GetLogsByEntity(string entityType, string? entityId = null)
    {
        try
        {
            var query = _context.AuditLogs
                .Where(l => l.EntityType == entityType)
                .OrderByDescending(l => l.Timestamp)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(entityId))
            {
                query = query.Where(l => l.EntityId == entityId);
            }

            return query.ToList();
        }
        catch
        {
            return new List<AuditLog>();
        }
    }

    public List<AuditLog> GetLogsByUser(string userFirstName, string userLastName)
    {
        try
        {
            return _context.AuditLogs
                .Where(l => l.UserFirstName == userFirstName && l.UserLastName == userLastName)
                .OrderByDescending(l => l.Timestamp)
                .ToList();
        }
        catch
        {
            return new List<AuditLog>();
        }
    }
}

