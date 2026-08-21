using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using RayanTask.Data;
using RayanTask.Models;

namespace RayanTask.Services;

public class MigrationService
{
    private readonly ApplicationDbContext _context;
    private readonly IWebHostEnvironment _environment;

    public MigrationService(ApplicationDbContext context, IWebHostEnvironment environment)
    {
        _context = context;
        _environment = environment;
    }

    // Migration از JSON به SQLite (یک بار اجرا می‌شود)
    public async Task MigrateFromJsonToDatabase()
    {
        var jsonFilePath = Path.Combine(_environment.ContentRootPath, "Data", "activity_records.json");
        
        if (!File.Exists(jsonFilePath))
        {
            return; // فایل JSON وجود ندارد
        }

        // بررسی اینکه آیا دیتابیس خالی است
        if (await _context.ActivityRecords.AnyAsync())
        {
            return; // دیتابیس قبلاً پر شده است
        }

        try
        {
            var json = File.ReadAllText(jsonFilePath);
            var records = JsonConvert.DeserializeObject<List<ActivityRecord>>(json) ?? new List<ActivityRecord>();

            if (records.Any())
            {
                await _context.ActivityRecords.AddRangeAsync(records);
                await _context.SaveChangesAsync();
            }
        }
        catch
        {
            // در صورت خطا، Migration انجام نمی‌شود
        }
    }
}

