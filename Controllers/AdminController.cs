using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using RayanTask.Data;
using RayanTask.Models;
using RayanTask.Services;
using RayanTask.Helpers;

namespace RayanTask.Controllers;

public class AdminController : BaseController
{
    private readonly DataService _dataService;
    private readonly LogService _logService;
    private readonly ILogger<AdminController> _logger;
    private readonly ApplicationDbContext _context;

    public AdminController(DataService dataService, LogService logService, ILogger<AdminController> logger, ApplicationDbContext context)
    {
        _dataService = dataService;
        _logService = logService;
        _logger = logger;
        _context = context;
    }

    private bool HasPermission(string permission)
    {
        var isAdmin = HttpContext.Session.GetString("IsAdmin") == "true";
        if (isAdmin)
        {
            // مدیران به همه چیز دسترسی دارند مگر اینکه صراحتاً محدود شده باشند
            return HttpContext.Session.GetString(permission) != "false";
        }
        return HttpContext.Session.GetString(permission) == "true";
    }

    public override void OnActionExecuting(ActionExecutingContext context)
    {
        var isAdmin = HttpContext.Session.GetString("IsAdmin") == "true";
        if (!isAdmin)
        {
            context.Result = RedirectToAction("Dashboard", "Home");
            return;
        }
        base.OnActionExecuting(context);
    }

    // صفحه اصلی مدیریت - redirect به داشبورد
    public IActionResult Index()
    {
        return RedirectToAction("Dashboard", "Home");
    }

    // ============================================
    // مدیریت کاربران
    // ============================================

    public IActionResult Users()
    {
        if (!HasPermission("CanManageUsers"))
        {
            TempData["ErrorMessage"] = "شما دسترسی به بخش مدیریت کاربران ندارید.";
            return RedirectToAction("Index");
        }
        var users = _context.Users.OrderBy(u => u.FirstName).ThenBy(u => u.LastName).ToList();
        return View(users);
    }

    [HttpGet]
    public IActionResult CreateUser()
    {
        return View(new NameItem());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult CreateUser(NameItem user)
    {
        if (string.IsNullOrWhiteSpace(user.FirstName) || string.IsNullOrWhiteSpace(user.LastName))
        {
            TempData["ErrorMessage"] = "نام و نام خانوادگی الزامی است.";
            return View(user);
        }

        // بررسی تکراری نبودن
        if (_context.Users.Any(u => u.FirstName == user.FirstName && u.LastName == user.LastName))
        {
            TempData["ErrorMessage"] = "این کاربر قبلاً ثبت شده است.";
            return View(user);
        }

        var newUser = new User
        {
            FirstName = user.FirstName,
            LastName = user.LastName,
            Password = string.Empty,
            IsAdmin = user.IsAdmin,
            CanEdit = user.CanEdit,
            CanDelete = user.CanDelete,
            CanSelectAnyDate = user.CanSelectAnyDate,
            IsActive = user.IsActive,
            CanManageUsers = user.CanManageUsers,
            CanManageActivityTypes = user.CanManageActivityTypes,
            CanManageResults = user.CanManageResults,
            CanViewReports = user.CanViewReports,
            CanViewLogs = user.CanViewLogs,
            CanViewActivityList = user.CanViewActivityList,
            CanViewIssueDetails = user.CanViewIssueDetails,
            CanEditIssues = user.CanEditIssues,
            CanDeleteIssues = user.CanDeleteIssues,
            CanDeleteReports = user.CanDeleteReports,
            CreatedAt = DateTime.Now
        };

        _context.Users.Add(newUser);
        _context.SaveChanges();

        // Hash کردن رمز عبور - بعد از SaveChanges چون salt بر اساس شناسه‌ی (Id) کاربر است
        if (!string.IsNullOrWhiteSpace(user.Password))
        {
            newUser.Password = PasswordHelper.HashPassword(user.Password, newUser.Id);
            _context.SaveChanges();
        }

        _logService.LogAction(
            GetCurrentUserFirstName(),
            GetCurrentUserLastName(),
            "Create",
            "User",
            $"{user.FirstName} {user.LastName}",
            $"کاربر جدید '{user.FirstName} {user.LastName}' ایجاد شد. (مدیر: {user.IsAdmin}, ویرایش: {user.CanEdit}, حذف: {user.CanDelete})"
        );

        TempData["SuccessMessage"] = "کاربر با موفقیت ثبت شد.";
        return RedirectToAction("Users");
    }

    [HttpGet]
    public IActionResult EditUser(int id)
    {
        var user = _context.Users.FirstOrDefault(u => u.Id == id);
        if (user == null)
        {
            TempData["ErrorMessage"] = "کاربر یافت نشد.";
            return RedirectToAction("Users");
        }

        var nameItem = new NameItem
        {
            FirstName = user.FirstName,
            LastName = user.LastName,
            Password = user.Password,
            IsAdmin = user.IsAdmin,
            CanEdit = user.CanEdit,
            CanDelete = user.CanDelete,
            CanSelectAnyDate = user.CanSelectAnyDate,
            IsActive = user.IsActive,
            CanManageUsers = user.CanManageUsers,
            CanManageActivityTypes = user.CanManageActivityTypes,
            CanManageResults = user.CanManageResults,
            CanViewReports = user.CanViewReports,
            CanViewLogs = user.CanViewLogs,
            CanViewActivityList = user.CanViewActivityList,
            CanViewIssueDetails = user.CanViewIssueDetails,
            CanEditIssues = user.CanEditIssues,
            CanDeleteIssues = user.CanDeleteIssues,
            CanDeleteReports = user.CanDeleteReports
        };
        
        ViewBag.Id = id;
        return View(nameItem);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult EditUser(int id, NameItem user, string? newPassword)
    {
        var existingUser = _context.Users.FirstOrDefault(u => u.Id == id);
        if (existingUser == null)
        {
            TempData["ErrorMessage"] = "کاربر یافت نشد.";
            return RedirectToAction("Users");
        }

        // بررسی تکراری نبودن (به جز خودش)
        if (_context.Users.Any(u => u.FirstName == user.FirstName && u.LastName == user.LastName && u.Id != id))
        {
            TempData["ErrorMessage"] = "این کاربر قبلاً ثبت شده است.";
            ViewBag.Id = id;
            return View(user);
        }

        // به‌روزرسانی اطلاعات
        existingUser.FirstName = user.FirstName;
        existingUser.LastName = user.LastName;
        existingUser.IsAdmin = user.IsAdmin;
        existingUser.CanEdit = user.CanEdit;
        existingUser.CanDelete = user.CanDelete;
        existingUser.CanSelectAnyDate = user.CanSelectAnyDate;
        existingUser.IsActive = user.IsActive;
        existingUser.CanManageUsers = user.CanManageUsers;
        existingUser.CanManageActivityTypes = user.CanManageActivityTypes;
        existingUser.CanManageResults = user.CanManageResults;
        existingUser.CanViewReports = user.CanViewReports;
        existingUser.CanViewLogs = user.CanViewLogs;
        existingUser.CanViewActivityList = user.CanViewActivityList;
        existingUser.CanViewIssueDetails = user.CanViewIssueDetails;
        existingUser.CanEditIssues = user.CanEditIssues;
        existingUser.CanDeleteIssues = user.CanDeleteIssues;
        existingUser.CanDeleteReports = user.CanDeleteReports;

        // اگر رمز عبور جدید وارد شده، آن را hash کن
        if (!string.IsNullOrWhiteSpace(newPassword))
        {
            existingUser.Password = PasswordHelper.HashPassword(newPassword, existingUser.Id);
        }

        existingUser.UpdatedAt = DateTime.Now;
        _context.SaveChanges();
        
        _logService.LogAction(
            GetCurrentUserFirstName(),
            GetCurrentUserLastName(),
            "Update",
            "User",
            $"{existingUser.FirstName} {existingUser.LastName}",
            $"کاربر '{existingUser.FirstName} {existingUser.LastName}' به‌روزرسانی شد."
        );
        
        TempData["SuccessMessage"] = "کاربر با موفقیت به‌روزرسانی شد.";
        return RedirectToAction("Users");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult DeleteUser(int id)
    {
        var user = _context.Users.FirstOrDefault(u => u.Id == id);
        if (user == null)
        {
            TempData["ErrorMessage"] = "کاربر یافت نشد.";
            return RedirectToAction("Users");
        }

        var deletedUser = $"{user.FirstName} {user.LastName}";
        _context.Users.Remove(user);
        _context.SaveChanges();
        
        _logService.LogAction(
            GetCurrentUserFirstName(),
            GetCurrentUserLastName(),
            "Delete",
            "User",
            deletedUser,
            $"کاربر '{deletedUser}' حذف شد."
        );
        
        TempData["SuccessMessage"] = "کاربر با موفقیت حذف شد.";
        return RedirectToAction("Users");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult ToggleUserStatus(int id)
    {
        var user = _context.Users.FirstOrDefault(u => u.Id == id);
        if (user == null)
        {
            TempData["ErrorMessage"] = "کاربر یافت نشد.";
            return RedirectToAction("Users");
        }

        user.IsActive = !user.IsActive;
        user.UpdatedAt = DateTime.Now;
        _context.SaveChanges();
        
        _logService.LogAction(
            GetCurrentUserFirstName(),
            GetCurrentUserLastName(),
            "ToggleStatus",
            "User",
            $"{user.FirstName} {user.LastName}",
            $"کاربر '{user.FirstName} {user.LastName}' {(user.IsActive ? "فعال" : "غیرفعال")} شد."
        );
        
        TempData["SuccessMessage"] = $"کاربر {(user.IsActive ? "فعال" : "غیرفعال")} شد.";
        return RedirectToAction("Users");
    }

    // حذف تمام رکوردهای فعالیت یک کاربر
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult DeleteAllUserRecords(string firstName, string lastName)
    {
        if (!HasPermission("CanManageUsers"))
        {
            TempData["ErrorMessage"] = "شما دسترسی به این بخش ندارید.";
            return RedirectToAction("Users");
        }

        if (string.IsNullOrWhiteSpace(firstName) || string.IsNullOrWhiteSpace(lastName))
        {
            TempData["ErrorMessage"] = "اطلاعات کاربر معتبر نیست.";
            return RedirectToAction("Users");
        }

        // پیدا کردن UserId از firstName و lastName
        var user = _context.Users.FirstOrDefault(u => u.FirstName == firstName && u.LastName == lastName);
        if (user == null)
        {
            TempData["ErrorMessage"] = "کاربر یافت نشد.";
            return RedirectToAction("Users");
        }

        // حذف تمام رکوردهای کاربر
        var deletedCount = _dataService.DeleteAllUserRecords(user.Id);

        if (deletedCount > 0)
        {
            // ثبت لاگ حذف
            _logService.LogAction(
                GetCurrentUserFirstName(),
                GetCurrentUserLastName(),
                "DeleteAll",
                "ActivityRecord",
                $"{firstName} {lastName}",
                $"تمام رکوردهای فعالیت کاربر '{firstName} {lastName}' حذف شد. تعداد: {deletedCount} رکورد"
            );

            TempData["SuccessMessage"] = $"تمام رکوردهای فعالیت کاربر '{firstName} {lastName}' ({deletedCount} رکورد) با موفقیت حذف شد.";
        }
        else
        {
            TempData["InfoMessage"] = $"هیچ رکورد فعالیتی برای کاربر '{firstName} {lastName}' یافت نشد.";
        }

        return RedirectToAction("Users");
    }

    // ============================================
    // مدیریت نوع فعالیت
    // ============================================

    public IActionResult ActivityTypes()
    {
        if (!HasPermission("CanManageActivityTypes"))
        {
            TempData["ErrorMessage"] = "شما دسترسی به بخش مدیریت نوع فعالیت ندارید.";
            return RedirectToAction("Index");
        }
        var activityTypes = _context.ActivityTypes.OrderBy(at => at.Value).ToList();
        return View(activityTypes);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult CreateActivityType(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            TempData["ErrorMessage"] = "نوع فعالیت نمی‌تواند خالی باشد.";
            return RedirectToAction("ActivityTypes");
        }

        // بررسی تکراری نبودن
        if (_context.ActivityTypes.Any(at => at.Value == value))
        {
            TempData["ErrorMessage"] = "این نوع فعالیت قبلاً ثبت شده است.";
            return RedirectToAction("ActivityTypes");
        }

        var activityType = new ActivityType
        {
            Value = value,
            IsActive = true,
            CreatedAt = DateTime.Now
        };
        
        _context.ActivityTypes.Add(activityType);
        _context.SaveChanges();

        _logService.LogAction(
            GetCurrentUserFirstName(),
            GetCurrentUserLastName(),
            "Create",
            "ActivityType",
            value,
            $"نوع فعالیت '{value}' ایجاد شد."
        );

        TempData["SuccessMessage"] = "نوع فعالیت با موفقیت ثبت شد.";
        return RedirectToAction("ActivityTypes");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult EditActivityType(int id, string value)
    {
        var activityType = _context.ActivityTypes.FirstOrDefault(at => at.Id == id);
        if (activityType == null)
        {
            TempData["ErrorMessage"] = "نوع فعالیت یافت نشد.";
            return RedirectToAction("ActivityTypes");
        }

        if (string.IsNullOrWhiteSpace(value))
        {
            TempData["ErrorMessage"] = "نوع فعالیت نمی‌تواند خالی باشد.";
            return RedirectToAction("ActivityTypes");
        }

        // بررسی تکراری نبودن (به جز خودش)
        if (_context.ActivityTypes.Any(at => at.Value == value && at.Id != id))
        {
            TempData["ErrorMessage"] = "این نوع فعالیت قبلاً ثبت شده است.";
            return RedirectToAction("ActivityTypes");
        }

        var oldValue = activityType.Value;
        activityType.Value = value;
        activityType.UpdatedAt = DateTime.Now;
        _context.SaveChanges();
        
        _logService.LogAction(
            GetCurrentUserFirstName(),
            GetCurrentUserLastName(),
            "Update",
            "ActivityType",
            value,
            $"نوع فعالیت از '{oldValue}' به '{value}' تغییر یافت."
        );
        
        TempData["SuccessMessage"] = "نوع فعالیت با موفقیت به‌روزرسانی شد.";
        return RedirectToAction("ActivityTypes");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult DeleteActivityType(int id)
    {
        var activityType = _context.ActivityTypes.FirstOrDefault(at => at.Id == id);
        if (activityType == null)
        {
            TempData["ErrorMessage"] = "نوع فعالیت یافت نشد.";
            return RedirectToAction("ActivityTypes");
        }

        var deletedValue = activityType.Value;
        _context.ActivityTypes.Remove(activityType);
        _context.SaveChanges();
        
        _logService.LogAction(
            GetCurrentUserFirstName(),
            GetCurrentUserLastName(),
            "Delete",
            "ActivityType",
            deletedValue,
            $"نوع فعالیت '{deletedValue}' حذف شد."
        );
        
        TempData["SuccessMessage"] = "نوع فعالیت با موفقیت حذف شد.";
        return RedirectToAction("ActivityTypes");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult ToggleActivityTypeStatus(int id)
    {
        var activityType = _context.ActivityTypes.FirstOrDefault(at => at.Id == id);
        if (activityType == null)
        {
            TempData["ErrorMessage"] = "نوع فعالیت یافت نشد.";
            return RedirectToAction("ActivityTypes");
        }

        activityType.IsActive = !activityType.IsActive;
        activityType.UpdatedAt = DateTime.Now;
        _context.SaveChanges();
        
        _logService.LogAction(
            GetCurrentUserFirstName(),
            GetCurrentUserLastName(),
            "ToggleStatus",
            "ActivityType",
            activityType.Value,
            $"نوع فعالیت '{activityType.Value}' {(activityType.IsActive ? "فعال" : "غیرفعال")} شد."
        );
        
        TempData["SuccessMessage"] = $"نوع فعالیت {(activityType.IsActive ? "فعال" : "غیرفعال")} شد.";
        return RedirectToAction("ActivityTypes");
    }

    // ============================================
    // مدیریت نتیجه
    // ============================================

    public IActionResult Results()
    {
        if (!HasPermission("CanManageResults"))
        {
            TempData["ErrorMessage"] = "شما دسترسی به بخش مدیریت نتیجه ندارید.";
            return RedirectToAction("Index");
        }
        var results = _context.Results.OrderBy(r => r.Value).ToList();
        return View(results);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult CreateResult(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            TempData["ErrorMessage"] = "نتیجه نمی‌تواند خالی باشد.";
            return RedirectToAction("Results");
        }

        // بررسی تکراری نبودن
        if (_context.Results.Any(r => r.Value == value))
        {
            TempData["ErrorMessage"] = "این نتیجه قبلاً ثبت شده است.";
            return RedirectToAction("Results");
        }

        var result = new Result
        {
            Value = value,
            IsActive = true,
            CreatedAt = DateTime.Now
        };
        
        _context.Results.Add(result);
        _context.SaveChanges();

        _logService.LogAction(
            GetCurrentUserFirstName(),
            GetCurrentUserLastName(),
            "Create",
            "Result",
            value,
            $"نتیجه '{value}' ایجاد شد."
        );

        TempData["SuccessMessage"] = "نتیجه با موفقیت ثبت شد.";
        return RedirectToAction("Results");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult EditResult(int id, string value)
    {
        var result = _context.Results.FirstOrDefault(r => r.Id == id);
        if (result == null)
        {
            TempData["ErrorMessage"] = "نتیجه یافت نشد.";
            return RedirectToAction("Results");
        }

        if (string.IsNullOrWhiteSpace(value))
        {
            TempData["ErrorMessage"] = "نتیجه نمی‌تواند خالی باشد.";
            return RedirectToAction("Results");
        }

        // بررسی تکراری نبودن (به جز خودش)
        if (_context.Results.Any(r => r.Value == value && r.Id != id))
        {
            TempData["ErrorMessage"] = "این نتیجه قبلاً ثبت شده است.";
            return RedirectToAction("Results");
        }

        result.Value = value;
        result.UpdatedAt = DateTime.Now;
        _context.SaveChanges();
        TempData["SuccessMessage"] = "نتیجه با موفقیت به‌روزرسانی شد.";
        return RedirectToAction("Results");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult DeleteResult(int id)
    {
        var result = _context.Results.FirstOrDefault(r => r.Id == id);
        if (result == null)
        {
            TempData["ErrorMessage"] = "نتیجه یافت نشد.";
            return RedirectToAction("Results");
        }

        var deletedValue = result.Value;
        _context.Results.Remove(result);
        _context.SaveChanges();
        
        _logService.LogAction(
            GetCurrentUserFirstName(),
            GetCurrentUserLastName(),
            "Delete",
            "Result",
            deletedValue,
            $"نتیجه '{deletedValue}' حذف شد."
        );
        
        TempData["SuccessMessage"] = "نتیجه با موفقیت حذف شد.";
        return RedirectToAction("Results");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult ToggleResultStatus(int id)
    {
        var result = _context.Results.FirstOrDefault(r => r.Id == id);
        if (result == null)
        {
            TempData["ErrorMessage"] = "نتیجه یافت نشد.";
            return RedirectToAction("Results");
        }

        result.IsActive = !result.IsActive;
        result.UpdatedAt = DateTime.Now;
        _context.SaveChanges();
        
        _logService.LogAction(
            GetCurrentUserFirstName(),
            GetCurrentUserLastName(),
            "ToggleStatus",
            "Result",
            result.Value,
            $"نتیجه '{result.Value}' {(result.IsActive ? "فعال" : "غیرفعال")} شد."
        );
        
        TempData["SuccessMessage"] = $"نتیجه {(result.IsActive ? "فعال" : "غیرفعال")} شد.";
        return RedirectToAction("Results");
    }

    // ============================================
    // گزارش‌گیری (منتقل شده از ReportsController)
    // ============================================

    public IActionResult Reports(ReportFilterViewModel? filter, string? fullName)
    {
        if (!HasPermission("CanViewReports"))
        {
            TempData["ErrorMessage"] = "شما دسترسی به بخش گزارش‌گیری ندارید.";
            return RedirectToAction("Index");
        }

        var allRecords = _dataService.LoadRecords();
        var config = _dataService.LoadConfiguration();
        
        // فیلتر کردن فقط آیتم‌های فعال
        config.ActivityTypes = config.ActivityTypes.Where(at => at.IsActive).ToList();
        config.Results = config.Results.Where(r => r.IsActive).ToList();
        config.Names = config.Names.Where(n => n.IsActive).ToList();

        // تبدیل fullName به Name و LastName
        if (!string.IsNullOrWhiteSpace(fullName) && filter != null)
        {
            var nameParts = fullName.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (nameParts.Length >= 2)
            {
                filter.Name = nameParts[0];
                filter.LastName = string.Join(" ", nameParts.Skip(1));
            }
        }

        // اعمال فیلترها
        var filteredRecords = allRecords.AsQueryable();

        if (filter != null)
        {
            // فیلتر بر اساس نام
            if (!string.IsNullOrWhiteSpace(filter.Name))
            {
                filteredRecords = filteredRecords.Where(r => r.Name == filter.Name);
            }

            // فیلتر بر اساس نام خانوادگی
            if (!string.IsNullOrWhiteSpace(filter.LastName))
            {
                filteredRecords = filteredRecords.Where(r => r.LastName == filter.LastName);
            }

            // فیلتر بر اساس تاریخ از
            if (filter.DateFrom.HasValue)
            {
                filteredRecords = filteredRecords.Where(r => r.Date.Date >= filter.DateFrom.Value.Date);
            }

            // فیلتر بر اساس تاریخ تا
            if (filter.DateTo.HasValue)
            {
                filteredRecords = filteredRecords.Where(r => r.Date.Date <= filter.DateTo.Value.Date);
            }

            // فیلتر بر اساس نوع فعالیت (چندگانه)
            if (filter.ActivityTypes != null && filter.ActivityTypes.Any())
            {
                var activeActivityTypes = config.ActivityTypes.Where(at => at.IsActive).Select(at => at.Value).ToList();
                var validActivityTypes = filter.ActivityTypes.Where(at => activeActivityTypes.Contains(at)).ToList();
                
                if (validActivityTypes.Any())
                {
                    // تبدیل به لیست برای استفاده از LINQ to Objects
                    var recordsList = filteredRecords.ToList();
                    filteredRecords = recordsList.Where(r =>
                    {
                        var recordActivityTypes = r.GetActivityTypes();
                        return validActivityTypes.Any(fat => recordActivityTypes.Contains(fat));
                    }).AsQueryable();
                }
            }
            // Backward compatibility: اگر ActivityType (تک) وجود داشت
            else if (!string.IsNullOrWhiteSpace(filter.ActivityType))
            {
                var activeActivityTypes = config.ActivityTypes.Where(at => at.IsActive).Select(at => at.Value).ToList();
                if (activeActivityTypes.Contains(filter.ActivityType))
                {
                    // تبدیل به لیست برای استفاده از LINQ to Objects
                    var recordsList = filteredRecords.ToList();
                    filteredRecords = recordsList.Where(r =>
                    {
                        var recordActivityTypes = r.GetActivityTypes();
                        return recordActivityTypes.Contains(filter.ActivityType);
                    }).AsQueryable();
                }
            }

            // فیلتر بر اساس نتیجه (فقط آیتم‌های فعال)
            if (!string.IsNullOrWhiteSpace(filter.Result))
            {
                var activeResults = config.Results.Where(r => r.IsActive).Select(r => r.Value).ToList();
                if (activeResults.Contains(filter.Result))
                {
                    // پیدا کردن ResultId از filter.Result
                    var resultId = config.Results.FirstOrDefault(r => r.Value == filter.Result && r.IsActive)?.Id;
                    if (resultId.HasValue)
                    {
                        filteredRecords = filteredRecords.Where(r => r.ResultId == resultId.Value);
                    }
                }
            }

            // فیلتر بر اساس ذی‌نفع
            if (!string.IsNullOrWhiteSpace(filter.Stakeholder))
            {
                filteredRecords = filteredRecords.Where(r => r.Stakeholder.Contains(filter.Stakeholder));
            }

            // فیلتر بر اساس مدت زمان (حداقل)
            if (filter.MinDuration.HasValue)
            {
                filteredRecords = filteredRecords.Where(r => r.DurationMinutes >= filter.MinDuration.Value);
            }

            // فیلتر بر اساس مدت زمان (حداکثر)
            if (filter.MaxDuration.HasValue)
            {
                filteredRecords = filteredRecords.Where(r => r.DurationMinutes <= filter.MaxDuration.Value);
            }
        }

        var records = filteredRecords
            .OrderByDescending(r => r.Date)
            .ThenByDescending(r => r.Id)
            .ToList();

        var canDeleteReports = HttpContext.Session.GetString("CanDeleteReports") == "true";

        var viewModel = new ReportViewModel
        {
            Records = records,
            Configuration = config,
            Filter = filter ?? new ReportFilterViewModel(),
            CanDeleteReports = canDeleteReports
        };

        return View(viewModel);
    }

    // حذف رکورد از صفحه Reports
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult DeleteFromReports(int id, string? returnUrl)
    {
        if (!HasPermission("CanViewReports"))
        {
            TempData["ErrorMessage"] = "شما دسترسی به این بخش ندارید.";
            return RedirectToAction("Index");
        }

        var canDeleteReports = HttpContext.Session.GetString("CanDeleteReports") == "true";
        if (!canDeleteReports)
        {
            TempData["ErrorMessage"] = "شما دسترسی به حذف رکوردها از گزارش‌ها ندارید.";
            return RedirectToAction("Reports");
        }

        // بررسی وجود رکورد
        var record = _dataService.GetRecordById(id);
        if (record == null)
        {
            TempData["ErrorMessage"] = "رکورد یافت نشد.";
            return RedirectToAction("Reports");
        }

        // ذخیره اطلاعات رکورد برای لاگ قبل از حذف
        var recordInfo = $"کاربر: {record.Name} {record.LastName}, تاریخ: {record.Date:yyyy/MM/dd}, نوع فعالیت: {record.GetActivityTypesDisplay()}";

        // حذف رکورد
        if (_dataService.DeleteRecord(id))
        {
            // ثبت لاگ حذف
            _logService.LogAction(
                GetCurrentUserFirstName(),
                GetCurrentUserLastName(),
                "Delete",
                "ActivityRecord",
                id.ToString(),
                $"رکورد فعالیت از صفحه Reports حذف شد. {recordInfo}"
            );

            TempData["SuccessMessage"] = "رکورد با موفقیت حذف شد.";
        }
        else
        {
            TempData["ErrorMessage"] = "خطا در حذف رکورد.";
        }

        // بازگشت به صفحه Reports با فیلترهای قبلی
        if (!string.IsNullOrWhiteSpace(returnUrl))
        {
            return Redirect($"/Admin/Reports{returnUrl}");
        }
        return RedirectToAction("Reports");
    }

    // ============================================
    // نمایش لاگ‌ها
    // ============================================

    public IActionResult Logs()
    {
        if (!HasPermission("CanViewLogs"))
        {
            TempData["ErrorMessage"] = "شما دسترسی به بخش لاگ‌ها ندارید.";
            return RedirectToAction("Index");
        }

        var logs = _logService.GetLogs(1000); // آخرین 1000 لاگ
        return View(logs);
    }

    // ============================================
    // مدیریت Rate Limit
    // ============================================

    public IActionResult RateLimits()
    {
        if (!HasPermission("CanViewLogs"))
        {
            TempData["ErrorMessage"] = "شما دسترسی به این بخش ندارید.";
            return RedirectToAction("Index");
        }

        // دریافت ServiceProvider از HttpContext
        var serviceProvider = HttpContext.RequestServices;
        var blockedIps = RayanTask.Middleware.RateLimitMiddleware.GetBlockedIps(serviceProvider);
        
        // دریافت تنظیمات فعلی برای نمایش
        var config = _dataService.LoadConfiguration();
        ViewBag.RateLimitSettings = config.RateLimitSettings ?? new RayanTask.Models.RateLimitSettings 
        { 
            MaxRequests = 5, 
            TimeWindowMinutes = 15 
        };
        
        return View(blockedIps);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult ResetRateLimit(string? ipAddress)
    {
        if (!HasPermission("CanViewLogs"))
        {
            TempData["ErrorMessage"] = "شما دسترسی به این بخش ندارید.";
            return RedirectToAction("Index");
        }

        if (string.IsNullOrWhiteSpace(ipAddress))
        {
            TempData["ErrorMessage"] = "آدرس IP معتبر نیست.";
            return RedirectToAction("RateLimits");
        }

        RayanTask.Middleware.RateLimitMiddleware.ResetRateLimit(ipAddress);

        _logService.LogAction(
            GetCurrentUserFirstName(),
            GetCurrentUserLastName(),
            "ResetRateLimit",
            "RateLimit",
            ipAddress,
            $"Rate limit برای IP '{ipAddress}' ریست شد."
        );

        TempData["SuccessMessage"] = $"Rate limit برای IP '{ipAddress}' با موفقیت ریست شد.";
        return RedirectToAction("RateLimits");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult ResetAllRateLimits()
    {
        if (!HasPermission("CanViewLogs"))
        {
            TempData["ErrorMessage"] = "شما دسترسی به این بخش ندارید.";
            return RedirectToAction("Index");
        }

        RayanTask.Middleware.RateLimitMiddleware.ResetAllRateLimits();

        _logService.LogAction(
            GetCurrentUserFirstName(),
            GetCurrentUserLastName(),
            "ResetAllRateLimits",
            "RateLimit",
            "All",
            "تمام Rate limits ریست شدند."
        );

        TempData["SuccessMessage"] = "تمام Rate limits با موفقیت ریست شدند.";
        return RedirectToAction("RateLimits");
    }

    // ============================================
    // مدیریت نام نرم افزارها
    // ============================================

    public IActionResult SoftwareNames()
    {
        if (!HasPermission("CanManageActivityTypes"))
        {
            TempData["ErrorMessage"] = "شما دسترسی به بخش مدیریت نام نرم افزارها ندارید.";
            return RedirectToAction("Index");
        }
        var softwareNames = _context.SoftwareNames.OrderBy(sn => sn.Value).ToList();
        return View(softwareNames);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult CreateSoftwareName(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            TempData["ErrorMessage"] = "نام نرم افزار نمی‌تواند خالی باشد.";
            return RedirectToAction("SoftwareNames");
        }

        // بررسی تکراری نبودن
        if (_context.SoftwareNames.Any(sn => sn.Value == value))
        {
            TempData["ErrorMessage"] = "این نام نرم افزار قبلاً ثبت شده است.";
            return RedirectToAction("SoftwareNames");
        }

        var softwareName = new SoftwareName
        {
            Value = value,
            IsActive = true,
            CreatedAt = DateTime.Now
        };
        
        _context.SoftwareNames.Add(softwareName);
        _context.SaveChanges();

        _logService.LogAction(
            GetCurrentUserFirstName(),
            GetCurrentUserLastName(),
            "Create",
            "SoftwareName",
            value,
            $"نام نرم افزار '{value}' ایجاد شد."
        );

        TempData["SuccessMessage"] = "نام نرم افزار با موفقیت ثبت شد.";
        return RedirectToAction("SoftwareNames");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult EditSoftwareName(int id, string value)
    {
        var softwareName = _context.SoftwareNames.FirstOrDefault(sn => sn.Id == id);
        if (softwareName == null)
        {
            TempData["ErrorMessage"] = "نام نرم افزار یافت نشد.";
            return RedirectToAction("SoftwareNames");
        }

        if (string.IsNullOrWhiteSpace(value))
        {
            TempData["ErrorMessage"] = "نام نرم افزار نمی‌تواند خالی باشد.";
            return RedirectToAction("SoftwareNames");
        }

        // بررسی تکراری نبودن (به جز خودش)
        if (_context.SoftwareNames.Any(sn => sn.Value == value && sn.Id != id))
        {
            TempData["ErrorMessage"] = "این نام نرم افزار قبلاً ثبت شده است.";
            return RedirectToAction("SoftwareNames");
        }

        var oldValue = softwareName.Value;
        softwareName.Value = value;
        softwareName.UpdatedAt = DateTime.Now;
        _context.SaveChanges();
        
        _logService.LogAction(
            GetCurrentUserFirstName(),
            GetCurrentUserLastName(),
            "Update",
            "SoftwareName",
            value,
            $"نام نرم افزار از '{oldValue}' به '{value}' تغییر یافت."
        );
        
        TempData["SuccessMessage"] = "نام نرم افزار با موفقیت به‌روزرسانی شد.";
        return RedirectToAction("SoftwareNames");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult DeleteSoftwareName(int id)
    {
        var softwareName = _context.SoftwareNames.FirstOrDefault(sn => sn.Id == id);
        if (softwareName == null)
        {
            TempData["ErrorMessage"] = "نام نرم افزار یافت نشد.";
            return RedirectToAction("SoftwareNames");
        }

        var deletedValue = softwareName.Value;
        _context.SoftwareNames.Remove(softwareName);
        _context.SaveChanges();
        
        _logService.LogAction(
            GetCurrentUserFirstName(),
            GetCurrentUserLastName(),
            "Delete",
            "SoftwareName",
            deletedValue,
            $"نام نرم افزار '{deletedValue}' حذف شد."
        );
        
        TempData["SuccessMessage"] = "نام نرم افزار با موفقیت حذف شد.";
        return RedirectToAction("SoftwareNames");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult ToggleSoftwareNameStatus(int id)
    {
        var softwareName = _context.SoftwareNames.FirstOrDefault(sn => sn.Id == id);
        if (softwareName == null)
        {
            TempData["ErrorMessage"] = "نام نرم افزار یافت نشد.";
            return RedirectToAction("SoftwareNames");
        }

        softwareName.IsActive = !softwareName.IsActive;
        softwareName.UpdatedAt = DateTime.Now;
        _context.SaveChanges();
        
        _logService.LogAction(
            GetCurrentUserFirstName(),
            GetCurrentUserLastName(),
            "ToggleStatus",
            "SoftwareName",
            softwareName.Value,
            $"نام نرم افزار '{softwareName.Value}' {(softwareName.IsActive ? "فعال" : "غیرفعال")} شد."
        );
        
        TempData["SuccessMessage"] = $"نام نرم افزار {(softwareName.IsActive ? "فعال" : "غیرفعال")} شد.";
        return RedirectToAction("SoftwareNames");
    }

    // ============================================
    // مدیریت انواع مشکل/نظر
    // ============================================

    public IActionResult IssueTypes()
    {
        if (!HasPermission("CanManageActivityTypes"))
        {
            TempData["ErrorMessage"] = "شما دسترسی به بخش مدیریت انواع مشکل/نظر ندارید.";
            return RedirectToAction("Index");
        }
        var issueTypes = _context.IssueTypes.OrderBy(it => it.Value).ToList();
        return View(issueTypes);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult CreateIssueType(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            TempData["ErrorMessage"] = "نوع مشکل/نظر نمی‌تواند خالی باشد.";
            return RedirectToAction("IssueTypes");
        }

        // بررسی تکراری نبودن
        if (_context.IssueTypes.Any(it => it.Value == value))
        {
            TempData["ErrorMessage"] = "این نوع مشکل/نظر قبلاً ثبت شده است.";
            return RedirectToAction("IssueTypes");
        }

        var issueType = new IssueType
        {
            Value = value,
            IsActive = true,
            CreatedAt = DateTime.Now
        };
        
        _context.IssueTypes.Add(issueType);
        _context.SaveChanges();

        _logService.LogAction(
            GetCurrentUserFirstName(),
            GetCurrentUserLastName(),
            "Create",
            "IssueType",
            value,
            $"نوع مشکل/نظر '{value}' ایجاد شد."
        );

        TempData["SuccessMessage"] = "نوع مشکل/نظر با موفقیت ثبت شد.";
        return RedirectToAction("IssueTypes");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult EditIssueType(int id, string value)
    {
        var issueType = _context.IssueTypes.FirstOrDefault(it => it.Id == id);
        if (issueType == null)
        {
            TempData["ErrorMessage"] = "نوع مشکل/نظر یافت نشد.";
            return RedirectToAction("IssueTypes");
        }

        if (string.IsNullOrWhiteSpace(value))
        {
            TempData["ErrorMessage"] = "نوع مشکل/نظر نمی‌تواند خالی باشد.";
            return RedirectToAction("IssueTypes");
        }

        // بررسی تکراری نبودن (به جز خودش)
        if (_context.IssueTypes.Any(it => it.Value == value && it.Id != id))
        {
            TempData["ErrorMessage"] = "این نوع مشکل/نظر قبلاً ثبت شده است.";
            return RedirectToAction("IssueTypes");
        }

        var oldValue = issueType.Value;
        issueType.Value = value;
        issueType.UpdatedAt = DateTime.Now;
        _context.SaveChanges();
        
        _logService.LogAction(
            GetCurrentUserFirstName(),
            GetCurrentUserLastName(),
            "Update",
            "IssueType",
            value,
            $"نوع مشکل/نظر از '{oldValue}' به '{value}' تغییر یافت."
        );
        
        TempData["SuccessMessage"] = "نوع مشکل/نظر با موفقیت به‌روزرسانی شد.";
        return RedirectToAction("IssueTypes");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult DeleteIssueType(int id)
    {
        var issueType = _context.IssueTypes.FirstOrDefault(it => it.Id == id);
        if (issueType == null)
        {
            TempData["ErrorMessage"] = "نوع مشکل/نظر یافت نشد.";
            return RedirectToAction("IssueTypes");
        }

        var deletedValue = issueType.Value;
        _context.IssueTypes.Remove(issueType);
        _context.SaveChanges();
        
        _logService.LogAction(
            GetCurrentUserFirstName(),
            GetCurrentUserLastName(),
            "Delete",
            "IssueType",
            deletedValue,
            $"نوع مشکل/نظر '{deletedValue}' حذف شد."
        );
        
        TempData["SuccessMessage"] = "نوع مشکل/نظر با موفقیت حذف شد.";
        return RedirectToAction("IssueTypes");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult ToggleIssueTypeStatus(int id)
    {
        var issueType = _context.IssueTypes.FirstOrDefault(it => it.Id == id);
        if (issueType == null)
        {
            TempData["ErrorMessage"] = "نوع مشکل/نظر یافت نشد.";
            return RedirectToAction("IssueTypes");
        }

        issueType.IsActive = !issueType.IsActive;
        issueType.UpdatedAt = DateTime.Now;
        _context.SaveChanges();
        
        _logService.LogAction(
            GetCurrentUserFirstName(),
            GetCurrentUserLastName(),
            "ToggleStatus",
            "IssueType",
            issueType.Value,
            $"نوع مشکل/نظر '{issueType.Value}' {(issueType.IsActive ? "فعال" : "غیرفعال")} شد."
        );
        
        TempData["SuccessMessage"] = $"نوع مشکل/نظر {(issueType.IsActive ? "فعال" : "غیرفعال")} شد.";
        return RedirectToAction("IssueTypes");
    }
}

// ViewModels برای Reports
public class ReportFilterViewModel
{
    public string? Name { get; set; }
    public string? LastName { get; set; }
    public DateTime? DateFrom { get; set; }
    public DateTime? DateTo { get; set; }
    public string? ActivityType { get; set; } // برای backward compatibility
    public List<string>? ActivityTypes { get; set; } // برای فیلتر چندگانه
    public string? Result { get; set; }
    public string? Stakeholder { get; set; }
    public int? MinDuration { get; set; }
    public int? MaxDuration { get; set; }
}

public class ReportViewModel
{
    public List<ActivityRecord> Records { get; set; } = new();
    public ConfigurationData Configuration { get; set; } = new();
    public ReportFilterViewModel Filter { get; set; } = new();
    public bool CanDeleteReports { get; set; } = false;
}

