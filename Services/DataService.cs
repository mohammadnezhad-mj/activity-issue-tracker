using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RayanTask.Data;
using RayanTask.Models;

namespace RayanTask.Services;

public class DataService
{
    private readonly ApplicationDbContext _context;
    private readonly IWebHostEnvironment _environment;
    private readonly string _configFilePath;

    public DataService(ApplicationDbContext context, IWebHostEnvironment environment)
    {
        _context = context;
        _environment = environment;
        var dataDir = Path.Combine(_environment.ContentRootPath, "Data");
        
        if (!Directory.Exists(dataDir))
        {
            Directory.CreateDirectory(dataDir);
        }

        _configFilePath = Path.Combine(dataDir, "config.json");
    }

    // بارگذاری رکوردهای فعالیت از دیتابیس
    public List<ActivityRecord> LoadRecords()
    {
        try
        {
            return _context.ActivityRecords
                .Include(r => r.User)
                .Include(r => r.Result)
                .Include(r => r.ActivityTypes)
                .ToList();
        }
        catch
        {
            return new List<ActivityRecord>();
        }
    }

    // افزودن رکورد جدید
    public void AddRecord(ActivityRecord record)
    {
        try
        {
            // اطمینان از اینکه ActivityTypes collection initialize شده است
            if (record.ActivityTypes == null)
            {
                record.ActivityTypes = new List<ActivityType>();
            }

            _context.ActivityRecords.Add(record);
            _context.SaveChanges();
        }
        catch (Exception ex)
        {
            // Log the exception for debugging
            Console.WriteLine($"Error adding record: {ex.Message}");
            Console.WriteLine($"Stack trace: {ex.StackTrace}");
            throw; // Re-throw to let the controller handle it
        }
    }

    // دریافت رکورد بر اساس ID
    public ActivityRecord? GetRecordById(int id)
    {
        return _context.ActivityRecords
            .Include(r => r.User)
            .Include(r => r.Result)
            .Include(r => r.ActivityTypes)
            .FirstOrDefault(r => r.Id == id);
    }

    // به‌روزرسانی رکورد
    public bool UpdateRecord(ActivityRecord record, List<int> activityTypeIds)
    {
        try
        {
            var existingRecord = _context.ActivityRecords
                .Include(r => r.ActivityTypes)
                .FirstOrDefault(r => r.Id == record.Id);
            
            if (existingRecord == null)
            {
                return false;
            }

            existingRecord.UserId = record.UserId;
            existingRecord.Date = record.Date;
            existingRecord.Description = record.Description;
            existingRecord.DurationMinutes = record.DurationMinutes;
            existingRecord.Stakeholder = record.Stakeholder;
            existingRecord.ResultId = record.ResultId;

            // به‌روزرسانی ActivityTypes
            existingRecord.ActivityTypes.Clear();
            if (activityTypeIds != null && activityTypeIds.Any())
            {
                var activityTypes = _context.ActivityTypes
                    .Where(at => activityTypeIds.Contains(at.Id))
                    .ToList();
                foreach (var activityType in activityTypes)
                {
                    existingRecord.ActivityTypes.Add(activityType);
                }
            }

            _context.SaveChanges();
            return true;
        }
        catch
        {
            return false;
        }
    }

    // حذف رکورد
    public bool DeleteRecord(int id)
    {
        var recordToDelete = _context.ActivityRecords.FirstOrDefault(r => r.Id == id);
        
        if (recordToDelete == null)
        {
            return false;
        }

        _context.ActivityRecords.Remove(recordToDelete);
        _context.SaveChanges();
        return true;
    }

    // حذف تمام رکوردهای یک کاربر
    public int DeleteAllUserRecords(int userId)
    {
        try
        {
            var recordsToDelete = _context.ActivityRecords
                .Where(r => r.UserId == userId)
                .ToList();

            if (!recordsToDelete.Any())
            {
                return 0;
            }

            var count = recordsToDelete.Count;
            _context.ActivityRecords.RemoveRange(recordsToDelete);
            _context.SaveChanges();
            return count;
        }
        catch
        {
            return 0;
        }
    }

    // بارگذاری تنظیمات از دیتابیس
    public ConfigurationData LoadConfiguration()
    {
        try
        {
            var config = new ConfigurationData();

            // بارگذاری Users از دیتابیس
            var users = _context.Users.ToList();
            config.Names = users.Select(u => new NameItem
            {
                FirstName = u.FirstName,
                LastName = u.LastName,
                Password = u.Password,
                IsAdmin = u.IsAdmin,
                CanEdit = u.CanEdit,
                CanDelete = u.CanDelete,
                CanSelectAnyDate = u.CanSelectAnyDate,
                IsActive = u.IsActive,
                CanManageUsers = u.CanManageUsers,
                CanManageActivityTypes = u.CanManageActivityTypes,
                CanManageResults = u.CanManageResults,
                CanViewReports = u.CanViewReports,
                CanViewLogs = u.CanViewLogs,
                CanViewActivityList = u.CanViewActivityList,
                CanViewIssueDetails = u.CanViewIssueDetails,
                CanEditIssues = u.CanEditIssues,
                CanDeleteIssues = u.CanDeleteIssues,
                CanDeleteReports = u.CanDeleteReports
            }).ToList();

            // بارگذاری ActivityTypes از دیتابیس
            var activityTypes = _context.ActivityTypes.ToList();
            config.ActivityTypes = activityTypes.Select(at => new ConfigItem
            {
                Id = at.Id,
                Value = at.Value,
                IsActive = at.IsActive
            }).ToList();

            // بارگذاری Results از دیتابیس
            var results = _context.Results.ToList();
            config.Results = results.Select(r => new ConfigItem
            {
                Id = r.Id,
                Value = r.Value,
                IsActive = r.IsActive
            }).ToList();

            // بارگذاری SoftwareNames از دیتابیس
            var softwareNames = _context.SoftwareNames.ToList();
            config.SoftwareNames = softwareNames.Select(sn => new ConfigItem
            {
                Value = sn.Value,
                IsActive = sn.IsActive
            }).ToList();

            // بارگذاری IssueTypes از دیتابیس
            var issueTypes = _context.IssueTypes.ToList();
            config.IssueTypes = issueTypes.Select(it => new ConfigItem
            {
                Value = it.Value,
                IsActive = it.IsActive
            }).ToList();

            // RateLimitSettings همچنان از JSON خوانده می‌شود (اختیاری)
            if (File.Exists(_configFilePath))
            {
                try
                {
                    var json = File.ReadAllText(_configFilePath);
                    var jsonConfig = JsonConvert.DeserializeObject<ConfigurationData>(json);
                    if (jsonConfig?.RateLimitSettings != null)
                    {
                        config.RateLimitSettings = jsonConfig.RateLimitSettings;
                    }
                }
                catch
                {
                    // اگر خطا داد، نادیده بگیر
                }
            }

            return config;
        }
        catch
        {
            return new ConfigurationData();
        }
    }

    // ذخیره تنظیمات - دیگر استفاده نمی‌شود (همه چیز در دیتابیس است)
    // فقط RateLimitSettings در JSON ذخیره می‌شود
    public void SaveConfiguration(ConfigurationData config)
    {
        // فقط RateLimitSettings را در JSON ذخیره می‌کنیم
        if (config.RateLimitSettings != null)
        {
            try
            {
                var jsonConfig = new ConfigurationData
                {
                    RateLimitSettings = config.RateLimitSettings
                };
                var json = JsonConvert.SerializeObject(jsonConfig, Formatting.Indented);
                File.WriteAllText(_configFilePath, json);
            }
            catch
            {
                // اگر خطا داد، نادیده بگیر
            }
        }
    }
}
