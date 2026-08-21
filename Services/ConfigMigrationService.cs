using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using RayanTask.Data;
using RayanTask.Models;

namespace RayanTask.Services;

public class ConfigMigrationService
{
    private readonly ApplicationDbContext _context;
    private readonly IWebHostEnvironment _environment;

    public ConfigMigrationService(ApplicationDbContext context, IWebHostEnvironment environment)
    {
        _context = context;
        _environment = environment;
    }

    public async Task MigrateFromJsonAsync()
    {
        var configFilePath = Path.Combine(_environment.ContentRootPath, "Data", "config.json");
        
        if (!File.Exists(configFilePath))
        {
            return; // فایل config.json وجود ندارد
        }

        try
        {
            var json = await File.ReadAllTextAsync(configFilePath);
            var config = JsonConvert.DeserializeObject<ConfigurationData>(json);
            
            if (config == null)
            {
                return;
            }

            // Migration Users
            if (config.Names != null && config.Names.Any())
            {
                var existingUsers = await _context.Users.ToListAsync();
                var existingUserKeys = existingUsers.Select(u => (u.FirstName, u.LastName)).ToHashSet();

                foreach (var nameItem in config.Names)
                {
                    if (!existingUserKeys.Contains((nameItem.FirstName, nameItem.LastName)))
                    {
                        var user = new User
                        {
                            FirstName = nameItem.FirstName,
                            LastName = nameItem.LastName,
                            Password = nameItem.Password,
                            IsAdmin = nameItem.IsAdmin,
                            CanEdit = nameItem.CanEdit,
                            CanDelete = nameItem.CanDelete,
                            CanSelectAnyDate = nameItem.CanSelectAnyDate,
                            IsActive = nameItem.IsActive,
                            CanManageUsers = nameItem.CanManageUsers,
                            CanManageActivityTypes = nameItem.CanManageActivityTypes,
                            CanManageResults = nameItem.CanManageResults,
                            CanViewReports = nameItem.CanViewReports,
                            CanViewLogs = nameItem.CanViewLogs,
                            CanViewActivityList = nameItem.CanViewActivityList,
                            CanViewIssueDetails = nameItem.CanViewIssueDetails,
                            CanEditIssues = nameItem.CanEditIssues,
                            CanDeleteIssues = nameItem.CanDeleteIssues,
                            CanDeleteReports = nameItem.CanDeleteReports,
                            CreatedAt = DateTime.Now
                        };
                        _context.Users.Add(user);
                    }
                }
            }

            // Migration ActivityTypes
            if (config.ActivityTypes != null && config.ActivityTypes.Any())
            {
                var existingActivityTypes = await _context.ActivityTypes.ToListAsync();
                var existingValues = existingActivityTypes.Select(at => at.Value).ToHashSet();

                foreach (var item in config.ActivityTypes)
                {
                    if (!existingValues.Contains(item.Value))
                    {
                        var activityType = new ActivityType
                        {
                            Value = item.Value,
                            IsActive = item.IsActive,
                            CreatedAt = DateTime.Now
                        };
                        _context.ActivityTypes.Add(activityType);
                    }
                }
            }

            // Migration Results
            if (config.Results != null && config.Results.Any())
            {
                var existingResults = await _context.Results.ToListAsync();
                var existingValues = existingResults.Select(r => r.Value).ToHashSet();

                foreach (var item in config.Results)
                {
                    if (!existingValues.Contains(item.Value))
                    {
                        var result = new Result
                        {
                            Value = item.Value,
                            IsActive = item.IsActive,
                            CreatedAt = DateTime.Now
                        };
                        _context.Results.Add(result);
                    }
                }
            }

            // Migration SoftwareNames
            if (config.SoftwareNames != null && config.SoftwareNames.Any())
            {
                var existingSoftwareNames = await _context.SoftwareNames.ToListAsync();
                var existingValues = existingSoftwareNames.Select(sn => sn.Value).ToHashSet();

                foreach (var item in config.SoftwareNames)
                {
                    if (!existingValues.Contains(item.Value))
                    {
                        var softwareName = new SoftwareName
                        {
                            Value = item.Value,
                            IsActive = item.IsActive,
                            CreatedAt = DateTime.Now
                        };
                        _context.SoftwareNames.Add(softwareName);
                    }
                }
            }

            // Migration IssueTypes
            if (config.IssueTypes != null && config.IssueTypes.Any())
            {
                var existingIssueTypes = await _context.IssueTypes.ToListAsync();
                var existingValues = existingIssueTypes.Select(it => it.Value).ToHashSet();

                foreach (var item in config.IssueTypes)
                {
                    if (!existingValues.Contains(item.Value))
                    {
                        var issueType = new IssueType
                        {
                            Value = item.Value,
                            IsActive = item.IsActive,
                            CreatedAt = DateTime.Now
                        };
                        _context.IssueTypes.Add(issueType);
                    }
                }
            }

            await _context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            // Log error if needed
            Console.WriteLine($"Error migrating config from JSON: {ex.Message}");
        }
    }
}



