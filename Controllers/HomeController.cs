using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using RayanTask.Data;
using RayanTask.Models;
using RayanTask.Services;

namespace RayanTask.Controllers;

public class HomeController : BaseController
{
    private readonly DataService _dataService;
    private readonly LogService _logService;
    private readonly ApplicationDbContext _context;
    private readonly ILogger<HomeController> _logger;

    public HomeController(DataService dataService, LogService logService, ApplicationDbContext context, ILogger<HomeController> logger)
    {
        _dataService = dataService;
        _logService = logService;
        _context = context;
        _logger = logger;
    }

    private bool CanViewActivityList()
    {
        var canViewActivityListSession = HttpContext.Session.GetString("CanViewActivityList");
        var canViewActivityList = canViewActivityListSession == "true" || string.IsNullOrEmpty(canViewActivityListSession);

        if (IsAdmin())
        {
            canViewActivityList = canViewActivityListSession != "false";
        }

        return canViewActivityList;
    }

    public override void OnActionExecuting(ActionExecutingContext context)
    {
        // بررسی لاگین برای همه Actionها به جز Error
        if (context.ActionDescriptor.RouteValues["action"] != "Error")
        {
            if (!IsLoggedIn())
            {
                context.Result = RedirectToAction("Login", "Account");
                return;
            }
        }
        base.OnActionExecuting(context);
    }

    public IActionResult Index()
    {
        var userId = GetCurrentUserId();
        if (!userId.HasValue)
        {
            return RedirectToAction("Login", "Account");
        }

        var firstName = HttpContext.Session.GetString("FirstName") ?? "";
        var lastName = HttpContext.Session.GetString("LastName") ?? "";
        var isAdmin = HttpContext.Session.GetString("IsAdmin") == "true";
        var canEdit = HttpContext.Session.GetString("CanEdit") == "true";
        var canDelete = HttpContext.Session.GetString("CanDelete") == "true";
        var canSelectAnyDate = HttpContext.Session.GetString("CanSelectAnyDate") == "true";
        var canViewActivityList = CanViewActivityList();

        if (!canViewActivityList)
        {
            TempData["ErrorMessage"] = "شما دسترسی به مشاهده لیست فعالیت‌ها ندارید.";
            return RedirectToAction("Dashboard");
        }

        var allRecords = _dataService.LoadRecords();
        var config = _dataService.LoadConfiguration();
        
        // فیلتر کردن فقط آیتم‌های فعال
        config.ActivityTypes = config.ActivityTypes.Where(at => at.IsActive).ToList();
        config.Results = config.Results.Where(r => r.IsActive).ToList();
        config.Names = config.Names.Where(n => n.IsActive).ToList();
        
        // همه کاربران (شامل مدیر) فقط رکوردهای خودشان را می‌بینند
        var today = DateTime.Now.Date;
        var yesterday = today.AddDays(-1);
        
        var userRecords = allRecords
            .Where(r => r.UserId == userId.Value)
            .Where(r => r.Date.Date == today || r.Date.Date == yesterday)
            .OrderByDescending(r => r.Date)
            .ThenByDescending(r => r.Id)
            .ToList();
        
        var viewModel = new ActivityViewModel
        {
            Records = userRecords,
            Configuration = config,
            CurrentUser = new UserSession
            {
                FirstName = firstName,
                LastName = lastName,
                IsAdmin = isAdmin,
                CanEdit = canEdit,
                CanDelete = canDelete,
                CanSelectAnyDate = canSelectAnyDate,
                CanViewActivityList = canViewActivityList
            }
        };

        return View(viewModel);
    }

    // صفحه ثبت فعالیت جدید
    [HttpGet]
    public IActionResult Create()
    {
        if (!CanViewActivityList())
        {
            TempData["ErrorMessage"] = "شما دسترسی به ثبت فعالیت ندارید.";
            return RedirectToAction("Dashboard");
        }

        var firstName = HttpContext.Session.GetString("FirstName") ?? "";
        var lastName = HttpContext.Session.GetString("LastName") ?? "";
        var isAdmin = HttpContext.Session.GetString("IsAdmin") == "true";
        var canEdit = HttpContext.Session.GetString("CanEdit") == "true";
        var canDelete = HttpContext.Session.GetString("CanDelete") == "true";
        var canSelectAnyDate = HttpContext.Session.GetString("CanSelectAnyDate") == "true";

        var config = _dataService.LoadConfiguration();
        
        // فیلتر کردن فقط آیتم‌های فعال
        config.ActivityTypes = config.ActivityTypes.Where(at => at.IsActive).ToList();
        config.Results = config.Results.Where(r => r.IsActive).ToList();
        config.Names = config.Names.Where(n => n.IsActive).ToList();
        
        var viewModel = new ActivityViewModel
        {
            Records = new List<ActivityRecord>(), // لیست خالی برای فرم ثبت
            Configuration = config,
            CurrentUser = new UserSession
            {
                FirstName = firstName,
                LastName = lastName,
                IsAdmin = isAdmin,
                CanEdit = canEdit,
                CanDelete = canDelete,
                CanSelectAnyDate = canSelectAnyDate,
                CanViewActivityList = true
            }
        };

        return View(viewModel);
    }

    // داشبورد
    public IActionResult Dashboard()
    {
        var firstName = HttpContext.Session.GetString("FirstName") ?? "";
        var lastName = HttpContext.Session.GetString("LastName") ?? "";
        var isAdmin = HttpContext.Session.GetString("IsAdmin") == "true";
        
        // خواندن تمام دسترسی‌ها از session
        var canEdit = HttpContext.Session.GetString("CanEdit") == "true";
        var canDelete = HttpContext.Session.GetString("CanDelete") == "true";
        var canSelectAnyDate = HttpContext.Session.GetString("CanSelectAnyDate") == "true";
        var canManageUsers = HttpContext.Session.GetString("CanManageUsers") == "true";
        var canManageActivityTypes = HttpContext.Session.GetString("CanManageActivityTypes") == "true";
        var canManageResults = HttpContext.Session.GetString("CanManageResults") == "true";
        var canViewReports = HttpContext.Session.GetString("CanViewReports") == "true";
        var canViewLogs = HttpContext.Session.GetString("CanViewLogs") == "true";
        var canViewActivityList = CanViewActivityList();
        var canViewIssueDetails = HttpContext.Session.GetString("CanViewIssueDetails") == "true";
        var canEditIssues = HttpContext.Session.GetString("CanEditIssues") == "true";
        var canDeleteIssues = HttpContext.Session.GetString("CanDeleteIssues") == "true";
        var canDeleteReports = HttpContext.Session.GetString("CanDeleteReports") == "true";

        // اگر مدیر است و دسترسی صراحتاً false نشده، دسترسی دارد
        if (isAdmin)
        {
            canManageUsers = HttpContext.Session.GetString("CanManageUsers") != "false";
            canManageActivityTypes = HttpContext.Session.GetString("CanManageActivityTypes") != "false";
            canManageResults = HttpContext.Session.GetString("CanManageResults") != "false";
            canViewReports = HttpContext.Session.GetString("CanViewReports") != "false";
            canViewLogs = HttpContext.Session.GetString("CanViewLogs") != "false";
            canViewActivityList = CanViewActivityList();
        }

        var allRecords = _dataService.LoadRecords();
        var config = _dataService.LoadConfiguration();
        
        // آمار کاربر فعلی
        var currentUserId = GetCurrentUserId();
        var userRecords = allRecords
            .Where(r => currentUserId.HasValue && r.UserId == currentUserId.Value)
            .ToList();
        
        var today = DateTime.Now.Date;
        var thisWeek = today.AddDays(-7);
        var thisMonth = new DateTime(today.Year, today.Month, 1);
        
        var todayRecords = userRecords.Where(r => r.Date.Date == today).ToList();
        var weekRecords = userRecords.Where(r => r.Date.Date >= thisWeek).ToList();
        
        // آمار مشکلات انتساب داده شده به کاربر
        var allIssues = _context.SoftwareIssues
            .Include(i => i.AssignedToEntity)
            .Include(i => i.ReporterEntity)
            .Include(i => i.StatusEntity)
            .ToList();

        var assignedIssues = allIssues
            .Where(i => currentUserId.HasValue && i.AssignedToId == currentUserId.Value)
            .ToList();

        var reportedIssues = allIssues
            .Where(i => currentUserId.HasValue && i.ReporterId == currentUserId.Value)
            .ToList();

        var visibleIssues = isAdmin
            ? allIssues
            : allIssues
                .Where(i => currentUserId.HasValue && (i.AssignedToId == currentUserId.Value || i.ReporterId == currentUserId.Value))
                .ToList();

        static bool IsClosedIssue(SoftwareIssue issue)
        {
            var status = issue.Status ?? string.Empty;
            return status.Contains("تکمیل") || status.Contains("لغو");
        }
        
        var issuesByStatus = visibleIssues
            .GroupBy(i => i.Status)
            .ToDictionary(g => g.Key, g => g.Count());
        
        var dashboardViewModel = new DashboardViewModel
        {
            UserName = $"{firstName} {lastName}",
            IsAdmin = isAdmin,
            CanEdit = canEdit,
            CanDelete = canDelete,
            CanSelectAnyDate = canSelectAnyDate,
            CanManageUsers = canManageUsers,
            CanManageActivityTypes = canManageActivityTypes,
            CanManageResults = canManageResults,
            CanViewReports = canViewReports,
            CanViewLogs = canViewLogs,
            CanViewActivityList = canViewActivityList,
            CanViewIssueDetails = canViewIssueDetails,
            CanEditIssues = canEditIssues,
            CanDeleteIssues = canDeleteIssues,
            CanDeleteReports = canDeleteReports,
            TodayRecordsCount = todayRecords.Count,
            WeekRecordsCount = weekRecords.Count,
            TodayTotalMinutes = todayRecords.Sum(r => r.DurationMinutes),
            WeekTotalMinutes = weekRecords.Sum(r => r.DurationMinutes),
            TotalAssignedIssues = assignedIssues.Count,
            TotalVisibleIssues = visibleIssues.Count,
            MyAssignedIssuesCount = assignedIssues.Count,
            MyReportedIssuesCount = reportedIssues.Count,
            OpenIssuesCount = visibleIssues.Count(i => !IsClosedIssue(i)),
            CompletedIssuesCount = visibleIssues.Count(i => (i.Status ?? string.Empty).Contains("تکمیل")),
            UnassignedIssuesCount = isAdmin ? visibleIssues.Count(i => !i.AssignedToId.HasValue && !IsClosedIssue(i)) : 0,
            UrgentIssuesCount = visibleIssues.Count(i => (i.Priority == "Urgent" || i.Priority == "High") && !IsClosedIssue(i)),
            OverdueIssuesCount = visibleIssues.Count(i => i.DueDate.HasValue && i.DueDate.Value.Date < today && !IsClosedIssue(i)),
            IssuesByStatus = issuesByStatus
        };

        return View(dashboardViewModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Create(ActivityRecord record)
    {
        if (!IsLoggedIn())
        {
            return RedirectToAction("Login", "Account");
        }

        if (!CanViewActivityList())
        {
            TempData["ErrorMessage"] = "شما دسترسی به ثبت فعالیت ندارید.";
            return RedirectToAction("Dashboard");
        }

        var currentUserId = GetCurrentUserId();
        if (!currentUserId.HasValue)
        {
            TempData["ErrorMessage"] = "خطا در شناسایی کاربر.";
            return RedirectToAction("Index");
        }

        // تنظیم UserId از Session
        record.UserId = currentUserId.Value;

        // دریافت Description از form (اگر Model Binding کار نکرد)
        var description = Request.Form["Description"].ToString().Trim();
        if (string.IsNullOrWhiteSpace(description))
        {
            description = record.Description?.Trim() ?? "";
        }
        record.Description = description;

        // دریافت Date از form
        var dateStr = Request.Form["Date"].ToString();
        if (!string.IsNullOrWhiteSpace(dateStr) && DateTime.TryParse(dateStr, out var parsedDate))
        {
            record.Date = parsedDate.Date;
        }
        else if (record.Date == default(DateTime))
        {
            record.Date = DateTime.Now.Date;
        }

        // دریافت DurationMinutes از form
        if (!int.TryParse(Request.Form["DurationMinutes"].ToString(), out int durationMinutes))
        {
            durationMinutes = record.DurationMinutes;
        }
        record.DurationMinutes = durationMinutes;

        // دریافت Stakeholder از form
        var stakeholder = Request.Form["Stakeholder"].ToString().Trim();
        if (string.IsNullOrWhiteSpace(stakeholder))
        {
            stakeholder = record.Stakeholder?.Trim() ?? "";
        }
        record.Stakeholder = stakeholder;

        // بررسی‌های اعتبارسنجی
        if (string.IsNullOrWhiteSpace(record.Description))
        {
            TempData["ErrorMessage"] = "لطفاً شرح را وارد کنید.";
            return RedirectToAction("Create");
        }

        if (record.Description.Length > 1000)
        {
            TempData["ErrorMessage"] = "شرح نمی‌تواند بیشتر از 1000 کاراکتر باشد.";
            return RedirectToAction("Create");
        }

        // Sanitize Description
        record.Description = SanitizeInput(record.Description);

        // دریافت لیست ActivityType IDs از form
        var activityTypeIdsStr = Request.Form["ActivityTypes"].ToList();
        if (activityTypeIdsStr == null || !activityTypeIdsStr.Any() || activityTypeIdsStr.All(string.IsNullOrWhiteSpace))
        {
            TempData["ErrorMessage"] = "لطفاً حداقل یک نوع فعالیت را انتخاب کنید.";
            return RedirectToAction("Create");
        }

        // تبدیل string IDs به int
        var activityTypeIds = new List<int>();
        foreach (var idStr in activityTypeIdsStr)
        {
            if (int.TryParse(idStr, out int id))
            {
                activityTypeIds.Add(id);
            }
        }

        if (!activityTypeIds.Any())
        {
            TempData["ErrorMessage"] = "لطفاً حداقل یک نوع فعالیت را انتخاب کنید.";
            return RedirectToAction("Create");
        }

        // بررسی اینکه تمام ActivityType IDs معتبر و فعال باشند
        var validActivityTypeIds = _context.ActivityTypes
            .Where(at => at.IsActive && activityTypeIds.Contains(at.Id))
            .Select(at => at.Id)
            .ToList();

        if (validActivityTypeIds.Count != activityTypeIds.Count)
        {
            TempData["ErrorMessage"] = "یکی از انواع فعالیت انتخاب شده معتبر نیست.";
            return RedirectToAction("Create");
        }

        // دریافت ResultId از form
        var resultIdStr = Request.Form["ResultId"].ToString().Trim();
        if (string.IsNullOrWhiteSpace(resultIdStr) || !int.TryParse(resultIdStr, out int resultId))
        {
            TempData["ErrorMessage"] = "لطفاً نتیجه را انتخاب کنید.";
            return RedirectToAction("Create");
        }

        // بررسی اینکه ResultId معتبر و فعال باشد
        var validResult = _context.Results
            .FirstOrDefault(r => r.Id == resultId && r.IsActive);
        
        if (validResult == null)
        {
            TempData["ErrorMessage"] = "نتیجه انتخاب شده معتبر نیست.";
            return RedirectToAction("Create");
        }
        
        record.ResultId = resultId;

        // بررسی مدت زمان
        if (record.DurationMinutes <= 0)
        {
            TempData["ErrorMessage"] = "لطفاً مدت زمان را وارد کنید.";
            return RedirectToAction("Create");
        }

        // بررسی ذی‌نفع / مشتری
        if (string.IsNullOrWhiteSpace(record.Stakeholder))
        {
            TempData["ErrorMessage"] = "لطفاً ذی‌نفع / مشتری را وارد کنید.";
            return RedirectToAction("Create");
        }

        // Sanitize Stakeholder
        if (!ValidateStringInput(record.Stakeholder, 200, out var sanitizedStakeholder))
        {
            TempData["ErrorMessage"] = "ذی‌نفع / مشتری معتبر نیست.";
            return RedirectToAction("Create");
        }
        record.Stakeholder = sanitizedStakeholder;

        // بررسی تاریخ (بر اساس دسترسی کاربر)
        var canSelectAnyDate = HttpContext.Session.GetString("CanSelectAnyDate") == "true";
        if (!canSelectAnyDate)
        {
            var today = DateTime.Now.Date;
            var yesterday = today.AddDays(-1);
            var selectedDate = record.Date.Date;

            if (selectedDate != today && selectedDate != yesterday)
            {
                TempData["ErrorMessage"] = "فقط می‌توانید برای امروز و دیروز رکورد ثبت کنید.";
                return RedirectToAction("Create");
            }
        }

        // اضافه کردن ActivityTypes به record
        // اطمینان از اینکه ActivityTypes collection initialize شده است
        if (record.ActivityTypes == null)
        {
            record.ActivityTypes = new List<ActivityType>();
        }

        var activityTypes = _context.ActivityTypes
            .Where(at => activityTypeIds.Contains(at.Id))
            .ToList();
        foreach (var activityType in activityTypes)
        {
            record.ActivityTypes.Add(activityType);
        }

        try
        {
            _dataService.AddRecord(record);
            
            // ثبت لاگ ایجاد
            var firstName = HttpContext.Session.GetString("FirstName") ?? "";
            var lastName = HttpContext.Session.GetString("LastName") ?? "";
            _logService.LogAction(
                firstName,
                lastName,
                "Create",
                "ActivityRecord",
                record.Id.ToString(),
                $"رکورد فعالیت جدید ایجاد شد. تاریخ: {record.Date:yyyy/MM/dd}, نوع فعالیت: {record.GetActivityTypesDisplay()}"
            );
            
            TempData["SuccessMessage"] = "رکورد با موفقیت ذخیره شد.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "خطا در ایجاد رکورد");
            TempData["ErrorMessage"] = "خطا در ذخیره رکورد. لطفاً دوباره تلاش کنید.";
        }

        return RedirectToAction("Create");
    }

    [HttpGet]
    public IActionResult Edit(int id)
    {
        if (!IsLoggedIn())
        {
            return RedirectToAction("Login", "Account");
        }

        var firstName = HttpContext.Session.GetString("FirstName") ?? "";
        var lastName = HttpContext.Session.GetString("LastName") ?? "";
        var isAdmin = HttpContext.Session.GetString("IsAdmin") == "true";
        var canEdit = HttpContext.Session.GetString("CanEdit") == "true";

        // بررسی دسترسی ویرایش
        if (!canEdit)
        {
            TempData["ErrorMessage"] = "شما دسترسی به ویرایش رکوردها ندارید.";
            return RedirectToAction("Index");
        }

        var record = _dataService.GetRecordById(id);
        
        if (record == null)
        {
            TempData["ErrorMessage"] = "رکورد یافت نشد.";
            return RedirectToAction("Index");
        }

        // بررسی مالکیت رکورد با استفاده از UserId
        var currentUserId = GetCurrentUserId();
        if (!currentUserId.HasValue)
        {
            TempData["ErrorMessage"] = "خطا در شناسایی کاربر.";
            return RedirectToAction("Index");
        }

        if (!isAdmin && record.UserId != currentUserId.Value)
        {
            TempData["ErrorMessage"] = "شما دسترسی به این رکورد ندارید.";
            return RedirectToAction("Index");
        }

        // بررسی تاریخ (بر اساس دسترسی کاربر)
        var canSelectAnyDate = HttpContext.Session.GetString("CanSelectAnyDate") == "true";
        if (!canSelectAnyDate)
        {
            var today = DateTime.Now.Date;
            var yesterday = today.AddDays(-1);
            if (record.Date.Date != today && record.Date.Date != yesterday)
            {
                TempData["ErrorMessage"] = "فقط می‌توانید رکوردهای امروز و دیروز را ویرایش کنید.";
                return RedirectToAction("Index");
            }
        }

        var config = _dataService.LoadConfiguration();
        var viewModel = new EditActivityViewModel
        {
            Record = record,
            Configuration = config,
            IsAdmin = isAdmin,
            CanSelectAnyDate = canSelectAnyDate
        };

        return View(viewModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Edit(ActivityRecord record)
    {
        if (!IsLoggedIn())
        {
            return RedirectToAction("Login", "Account");
        }

        var firstName = HttpContext.Session.GetString("FirstName") ?? "";
        var lastName = HttpContext.Session.GetString("LastName") ?? "";
        var isAdmin = HttpContext.Session.GetString("IsAdmin") == "true";
        var canEdit = HttpContext.Session.GetString("CanEdit") == "true";
        var canSelectAnyDate = HttpContext.Session.GetString("CanSelectAnyDate") == "true";

        // بررسی دسترسی ویرایش
        if (!canEdit)
        {
            TempData["ErrorMessage"] = "شما دسترسی به ویرایش رکوردها ندارید.";
            return RedirectToAction("Index");
        }

        // بررسی مالکیت رکورد (مدیر می‌تواند همه را ویرایش کند)
        var existingRecord = _dataService.GetRecordById(record.Id);
        if (existingRecord == null)
        {
            TempData["ErrorMessage"] = "رکورد یافت نشد.";
            return RedirectToAction("Index");
        }

        var currentUserId = GetCurrentUserId();
        if (!currentUserId.HasValue)
        {
            TempData["ErrorMessage"] = "خطا در شناسایی کاربر.";
            return RedirectToAction("Index");
        }

        // بررسی مالکیت رکورد با استفاده از UserId
        if (!isAdmin && existingRecord.UserId != currentUserId.Value)
        {
            TempData["ErrorMessage"] = "شما دسترسی به این رکورد ندارید.";
            return RedirectToAction("Index");
        }

        // تنظیم UserId (مدیر می‌تواند UserId را تغییر دهد، اما کاربر عادی نمی‌تواند)
        if (!isAdmin)
        {
            record.UserId = currentUserId.Value;
        }
        else
        {
            // برای مدیر، از existingRecord استفاده می‌کنیم
            record.UserId = existingRecord.UserId;
        }

        // بررسی تاریخ (بر اساس دسترسی کاربر)
        if (!canSelectAnyDate)
        {
            var today = DateTime.Now.Date;
            var yesterday = today.AddDays(-1);
            if (existingRecord.Date.Date != today && existingRecord.Date.Date != yesterday)
            {
                TempData["ErrorMessage"] = "فقط می‌توانید رکوردهای امروز و دیروز را ویرایش کنید.";
                return RedirectToAction("Index");
            }
        }

        var config = _dataService.LoadConfiguration();
        
        // فیلتر کردن فقط آیتم‌های فعال
        config.ActivityTypes = config.ActivityTypes.Where(at => at.IsActive).ToList();
        config.Results = config.Results.Where(r => r.IsActive).ToList();

        // دریافت لیست ActivityType IDs از form
        var activityTypeIdsStr = Request.Form["ActivityTypes"].ToList();
        if (activityTypeIdsStr == null || !activityTypeIdsStr.Any() || activityTypeIdsStr.All(string.IsNullOrWhiteSpace))
        {
            TempData["ErrorMessage"] = "لطفاً حداقل یک نوع فعالیت را انتخاب کنید.";
            return RedirectToAction("Edit", new { id = record.Id });
        }

        // تبدیل string IDs به int
        var activityTypeIds = new List<int>();
        foreach (var idStr in activityTypeIdsStr)
        {
            if (int.TryParse(idStr, out int id))
            {
                activityTypeIds.Add(id);
            }
        }

        if (!activityTypeIds.Any())
        {
            TempData["ErrorMessage"] = "لطفاً حداقل یک نوع فعالیت را انتخاب کنید.";
            return RedirectToAction("Edit", new { id = record.Id });
        }

        // بررسی اینکه تمام ActivityType IDs معتبر و فعال باشند
        var validActivityTypeIds = _context.ActivityTypes
            .Where(at => at.IsActive && activityTypeIds.Contains(at.Id))
            .Select(at => at.Id)
            .ToList();

        if (validActivityTypeIds.Count != activityTypeIds.Count)
        {
            TempData["ErrorMessage"] = "یکی از انواع فعالیت انتخاب شده معتبر نیست.";
            return RedirectToAction("Edit", new { id = record.Id });
        }

        // دریافت شرح از form (اگر Model Binding کار نکرد)
        var description = Request.Form["Description"].ToString().Trim();
        if (string.IsNullOrWhiteSpace(description))
        {
            // اگر از form خالی بود، از Model Binding استفاده کن
            description = record.Description?.Trim() ?? "";
        }
        
        // بررسی شرح
        if (string.IsNullOrWhiteSpace(description))
        {
            TempData["ErrorMessage"] = "لطفاً شرح را وارد کنید.";
            return RedirectToAction("Edit", new { id = record.Id });
        }
        
        // تنظیم شرح در record
        record.Description = description;

        if (record.Description.Length > 1000)
        {
            TempData["ErrorMessage"] = "شرح نمی‌تواند بیشتر از 1000 کاراکتر باشد.";
            return RedirectToAction("Edit", new { id = record.Id });
        }

        // دریافت ResultId از form
        var resultIdStr = Request.Form["ResultId"].ToString().Trim();
        if (string.IsNullOrWhiteSpace(resultIdStr) || !int.TryParse(resultIdStr, out int resultId))
        {
            TempData["ErrorMessage"] = "لطفاً نتیجه را انتخاب کنید.";
            return RedirectToAction("Edit", new { id = record.Id });
        }

        // بررسی اینکه ResultId معتبر و فعال باشد
        var validResult = _context.Results
            .FirstOrDefault(r => r.Id == resultId && r.IsActive);
        
        if (validResult == null)
        {
            TempData["ErrorMessage"] = "نتیجه انتخاب شده معتبر نیست.";
            return RedirectToAction("Edit", new { id = record.Id });
        }
        
        record.ResultId = resultId;

        // دریافت مدت زمان از form
        if (!int.TryParse(Request.Form["DurationMinutes"].ToString(), out int durationMinutes))
        {
            durationMinutes = record.DurationMinutes;
        }
        
        // بررسی مدت زمان
        if (durationMinutes <= 0)
        {
            TempData["ErrorMessage"] = "لطفاً مدت زمان را وارد کنید.";
            return RedirectToAction("Edit", new { id = record.Id });
        }
        
        record.DurationMinutes = durationMinutes;

        // دریافت ذی‌نفع از form
        var stakeholder = Request.Form["Stakeholder"].ToString().Trim();
        if (string.IsNullOrWhiteSpace(stakeholder))
        {
            stakeholder = record.Stakeholder?.Trim() ?? "";
        }
        
        // بررسی ذی‌نفع / مشتری
        if (string.IsNullOrWhiteSpace(stakeholder))
        {
            TempData["ErrorMessage"] = "لطفاً ذی‌نفع / مشتری را وارد کنید.";
            return RedirectToAction("Edit", new { id = record.Id });
        }
        
        record.Stakeholder = stakeholder;

        // دریافت تاریخ از form
        var dateStr = Request.Form["Date"].ToString();
        DateTime selectedDate;
        if (!string.IsNullOrWhiteSpace(dateStr) && DateTime.TryParse(dateStr, out var parsedDate))
        {
            selectedDate = parsedDate.Date;
        }
        else
        {
            selectedDate = record.Date.Date;
        }
        
        // بررسی تاریخ جدید (بر اساس دسترسی کاربر)
        if (!canSelectAnyDate)
        {
            var today = DateTime.Now.Date;
            var yesterday = today.AddDays(-1);
            if (selectedDate != today && selectedDate != yesterday)
            {
                TempData["ErrorMessage"] = "فقط می‌توانید تاریخ امروز یا دیروز را انتخاب کنید.";
                return RedirectToAction("Edit", new { id = record.Id });
            }
        }
        
        record.Date = selectedDate;

        // نام و نام خانوادگی قبلاً در ابتدای متد تنظیم شده‌اند

        try
        {
            if (_dataService.UpdateRecord(record, activityTypeIds))
            {
                // ثبت لاگ به‌روزرسانی
                var updatedRecord = _dataService.GetRecordById(record.Id);
                _logService.LogAction(
                    firstName,
                    lastName,
                    "Update",
                    "ActivityRecord",
                    record.Id.ToString(),
                    $"رکورد فعالیت به‌روزرسانی شد. تاریخ: {record.Date:yyyy/MM/dd}, نوع فعالیت: {updatedRecord?.GetActivityTypesDisplay() ?? ""}"
                );
                
                TempData["SuccessMessage"] = "رکورد با موفقیت به‌روزرسانی شد.";
            }
            else
            {
                TempData["ErrorMessage"] = "خطا در به‌روزرسانی رکورد. لطفاً دوباره تلاش کنید.";
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "خطا در به‌روزرسانی رکورد با ID {RecordId}", record.Id);
            TempData["ErrorMessage"] = "خطا در به‌روزرسانی رکورد. لطفاً دوباره تلاش کنید.";
        }

        return RedirectToAction("Index");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Delete(int id)
    {
        if (!IsLoggedIn())
        {
            return RedirectToAction("Login", "Account");
        }

        var firstName = HttpContext.Session.GetString("FirstName") ?? "";
        var lastName = HttpContext.Session.GetString("LastName") ?? "";
        var isAdmin = HttpContext.Session.GetString("IsAdmin") == "true";
        var canDelete = HttpContext.Session.GetString("CanDelete") == "true";

        // بررسی دسترسی حذف (مدیر همیشه می‌تواند حذف کند)
        if (!isAdmin && !canDelete)
        {
            TempData["ErrorMessage"] = "شما دسترسی به حذف رکوردها ندارید.";
            return RedirectToAction("Index");
        }

        // بررسی وجود رکورد
        var record = _dataService.GetRecordById(id);
        if (record == null)
        {
            TempData["ErrorMessage"] = "رکورد یافت نشد.";
            return RedirectToAction("Index");
        }

        // بررسی مالکیت رکورد با استفاده از UserId
        var currentUserId = GetCurrentUserId();
        if (!currentUserId.HasValue)
        {
            TempData["ErrorMessage"] = "خطا در شناسایی کاربر.";
            return RedirectToAction("Index");
        }

        if (!isAdmin && record.UserId != currentUserId.Value)
        {
            TempData["ErrorMessage"] = "شما دسترسی به این رکورد ندارید.";
            return RedirectToAction("Index");
        }

        // بررسی تاریخ (مدیر می‌تواند همه تاریخ‌ها را حذف کند)
        if (!isAdmin)
        {
            var today = DateTime.Now.Date;
            var yesterday = today.AddDays(-1);
            if (record.Date.Date != today && record.Date.Date != yesterday)
            {
                TempData["ErrorMessage"] = "فقط می‌توانید رکوردهای امروز و دیروز را حذف کنید.";
                return RedirectToAction("Index");
            }
        }

        // ذخیره اطلاعات رکورد برای لاگ قبل از حذف
        var recordInfo = $"تاریخ: {record.Date:yyyy/MM/dd}, نوع فعالیت: {record.GetActivityTypesDisplay()}, کاربر: {record.Name} {record.LastName}";

        // حذف رکورد
        if (_dataService.DeleteRecord(id))
        {
            // ثبت لاگ حذف
            _logService.LogAction(
                firstName,
                lastName,
                "Delete",
                "ActivityRecord",
                id.ToString(),
                $"رکورد فعالیت حذف شد. {recordInfo}"
            );

            TempData["SuccessMessage"] = "رکورد با موفقیت حذف شد.";
        }
        else
        {
            TempData["ErrorMessage"] = "خطا در حذف رکورد.";
        }

        return RedirectToAction("Index");
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error(string? message = null)
    {
        var exceptionHandlerPathFeature = HttpContext.Features.Get<Microsoft.AspNetCore.Diagnostics.IExceptionHandlerPathFeature>();
        var exception = exceptionHandlerPathFeature?.Error;
        
        ViewData["Exception"] = exception;
        ViewData["RequestId"] = System.Diagnostics.Activity.Current?.Id ?? HttpContext.TraceIdentifier;
        ViewData["ErrorMessage"] = message ?? exception?.Message ?? "یک خطا در پردازش درخواست شما رخ داده است.";
        
        if (exception != null)
        {
            _logger.LogError(exception, "خطا در صفحه Error: {Message}", exception.Message);
        }
        
        return View();
    }

}

public class ActivityViewModel
{
    public List<ActivityRecord> Records { get; set; } = new();
    public ConfigurationData Configuration { get; set; } = new();
    public UserSession CurrentUser { get; set; } = new();
}

public class DashboardViewModel
{
    public string UserName { get; set; } = string.Empty;
    public bool IsAdmin { get; set; }
    public bool CanEdit { get; set; }
    public bool CanDelete { get; set; }
    public bool CanSelectAnyDate { get; set; }
    public bool CanManageUsers { get; set; }
    public bool CanManageActivityTypes { get; set; }
    public bool CanManageResults { get; set; }
    public bool CanViewReports { get; set; }
    public bool CanViewLogs { get; set; }
    public bool CanViewActivityList { get; set; }
    public bool CanViewIssueDetails { get; set; }
    public bool CanEditIssues { get; set; }
    public bool CanDeleteIssues { get; set; }
    public bool CanDeleteReports { get; set; }
    public int TodayRecordsCount { get; set; }
    public int WeekRecordsCount { get; set; }
    public int TodayTotalMinutes { get; set; }
    public int WeekTotalMinutes { get; set; }
    public int TotalAssignedIssues { get; set; }
    public int TotalVisibleIssues { get; set; }
    public int MyAssignedIssuesCount { get; set; }
    public int MyReportedIssuesCount { get; set; }
    public int OpenIssuesCount { get; set; }
    public int CompletedIssuesCount { get; set; }
    public int UnassignedIssuesCount { get; set; }
    public int UrgentIssuesCount { get; set; }
    public int OverdueIssuesCount { get; set; }
    public Dictionary<string, int> IssuesByStatus { get; set; } = new();
}

public class EditActivityViewModel
{
    public ActivityRecord Record { get; set; } = new();
    public ConfigurationData Configuration { get; set; } = new();
    public bool IsAdmin { get; set; }
    public bool CanSelectAnyDate { get; set; }
}
