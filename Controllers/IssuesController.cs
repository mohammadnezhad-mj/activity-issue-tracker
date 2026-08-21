using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.EntityFrameworkCore;
using RayanTask.Data;
using RayanTask.Models;
using RayanTask.Services;

namespace RayanTask.Controllers;

public class IssuesController : BaseController
{
    private const long MaxAttachmentSizeBytes = 200 * 1024 * 1024;
    private static readonly HashSet<string> AllowedAttachmentExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".jpg", ".jpeg", ".png", ".gif", ".webp", ".bmp",
        ".mp4", ".webm", ".mov", ".m4v", ".avi", ".mkv",
        ".pdf", ".doc", ".docx", ".xls", ".xlsx", ".ppt", ".pptx",
        ".txt", ".csv", ".zip", ".rar", ".7z"
    };
    private static readonly HashSet<string> AllowedCommentImageExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".jpg", ".jpeg", ".png", ".gif", ".webp", ".bmp"
    };
    private static readonly FileExtensionContentTypeProvider ContentTypeProvider = new();

    private readonly ApplicationDbContext _context;
    private readonly DataService _dataService;
    private readonly LogService _logService;
    private readonly ITelegramNotificationService _telegramNotification;
    private readonly ILogger<IssuesController> _logger;
    private readonly IWebHostEnvironment _environment;

    public IssuesController(ApplicationDbContext context, DataService dataService, LogService logService, ITelegramNotificationService telegramNotification, ILogger<IssuesController> logger, IWebHostEnvironment environment)
    {
        _context = context;
        _dataService = dataService;
        _logService = logService;
        _telegramNotification = telegramNotification;
        _logger = logger;
        _environment = environment;
    }

    public override void OnActionExecuting(ActionExecutingContext context)
    {
        if (!IsLoggedIn())
        {
            context.Result = RedirectToAction("Login", "Account");
            return;
        }
        base.OnActionExecuting(context);
    }

    private bool CanEditCurrentIssue(SoftwareIssue issue)
    {
        var canEditIssues = HttpContext.Session.GetString("CanEditIssues") == "true";
        var isAdmin = IsAdmin();
        var firstName = GetCurrentUserFirstName();
        var lastName = GetCurrentUserLastName();
        var isAssigned = issue.AssignedToEntity != null &&
            issue.AssignedToEntity.FirstName == firstName &&
            issue.AssignedToEntity.LastName == lastName;

        return canEditIssues || isAdmin || isAssigned;
    }

    private bool CanUseIssueQuickActions(SoftwareIssue issue)
    {
        var firstName = GetCurrentUserFirstName();
        var lastName = GetCurrentUserLastName();
        var isAssigned = issue.AssignedToEntity != null &&
            issue.AssignedToEntity.FirstName == firstName &&
            issue.AssignedToEntity.LastName == lastName;

        return IsAdmin() || isAssigned;
    }

    private bool CanViewCurrentIssue(SoftwareIssue issue)
    {
        return IsLoggedIn();
    }

    private IActionResult RedirectBack(string? returnUrl)
    {
        if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
        {
            return Redirect(returnUrl);
        }

        return RedirectToAction("Index");
    }

    private void LoadTelegramIssueReferences(SoftwareIssue issue)
    {
        _context.Entry(issue).Reference(i => i.SoftwareNameEntity).IsLoaded = false;
        _context.Entry(issue).Reference(i => i.IssueTypeEntity).IsLoaded = false;
        _context.Entry(issue).Reference(i => i.ReporterEntity).IsLoaded = false;
        _context.Entry(issue).Reference(i => i.AssignedToEntity).IsLoaded = false;
        _context.Entry(issue).Reference(i => i.StatusEntity).IsLoaded = false;
        _context.Entry(issue).Reference(i => i.SoftwareNameEntity).Load();
        _context.Entry(issue).Reference(i => i.IssueTypeEntity).Load();
        _context.Entry(issue).Reference(i => i.ReporterEntity).Load();
        _context.Entry(issue).Reference(i => i.AssignedToEntity).Load();
        _context.Entry(issue).Reference(i => i.StatusEntity).Load();
    }

    private async Task NotifyIssueUpdatedSafelyAsync(SoftwareIssue issue, string changedBy, IEnumerable<string> changes, string warningMessage)
    {
        try
        {
            LoadTelegramIssueReferences(issue);
            await _telegramNotification.NotifyIssueUpdatedAsync(issue, changedBy, changes);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, warningMessage);
        }
    }

    private async Task NotifyIssueDeletedSafelyAsync(SoftwareIssue issue, string deletedBy)
    {
        try
        {
            LoadTelegramIssueReferences(issue);
            await _telegramNotification.NotifyIssueDeletedAsync(issue, deletedBy);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "ارسال نوتیفیکیشن تلگرام برای حذف درخواست با خطا مواجه شد.");
        }
    }

    private static bool IsAllowedAttachment(IFormFile file, out string contentType, out string errorMessage)
    {
        var extension = Path.GetExtension(file.FileName);
        if (string.IsNullOrWhiteSpace(extension) || !AllowedAttachmentExtensions.Contains(extension))
        {
            contentType = string.Empty;
            errorMessage = $"فرمت فایل {Path.GetFileName(file.FileName)} مجاز نیست.";
            return false;
        }

        if (file.Length > MaxAttachmentSizeBytes)
        {
            contentType = string.Empty;
            errorMessage = $"فایل {Path.GetFileName(file.FileName)} بزرگتر از 200 مگابایت است.";
            return false;
        }

        if (!ContentTypeProvider.TryGetContentType(file.FileName, out var detectedContentType))
        {
            detectedContentType = string.IsNullOrWhiteSpace(file.ContentType) ? "application/octet-stream" : file.ContentType;
        }

        contentType = detectedContentType;

        errorMessage = string.Empty;
        return true;
    }

    private static bool IsAllowedCommentImageAttachment(IFormFile file, out string contentType, out string errorMessage)
    {
        var fileName = Path.GetFileName(file.FileName);
        var extension = Path.GetExtension(file.FileName);
        if (string.IsNullOrWhiteSpace(extension) || !AllowedCommentImageExtensions.Contains(extension))
        {
            contentType = string.Empty;
            errorMessage = $"فایل {fileName} مجاز نیست. در ضمیمه کامنت فقط فایل تصویری قابل بارگذاری است.";
            return false;
        }

        if (file.Length > MaxAttachmentSizeBytes)
        {
            contentType = string.Empty;
            errorMessage = $"فایل {fileName} بزرگتر از 200 مگابایت است.";
            return false;
        }

        if (!ContentTypeProvider.TryGetContentType(file.FileName, out var detectedContentType))
        {
            detectedContentType = string.IsNullOrWhiteSpace(file.ContentType) ? string.Empty : file.ContentType;
        }

        if (!detectedContentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase) ||
            (!string.IsNullOrWhiteSpace(file.ContentType) && !file.ContentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase)))
        {
            contentType = string.Empty;
            errorMessage = $"فایل {fileName} تصویر معتبر نیست. فقط فرمت‌های تصویری jpg، png، gif، webp و bmp مجاز هستند.";
            return false;
        }

        contentType = detectedContentType;
        errorMessage = string.Empty;
        return true;
    }

    private static PhysicalFileResult CreateAttachmentFileResult(string filePath, string? contentType)
    {
        return new PhysicalFileResult(filePath, string.IsNullOrWhiteSpace(contentType) ? "application/octet-stream" : contentType)
        {
            EnableRangeProcessing = true
        };
    }

    // لیست درخواست‌ها
    public IActionResult Index(
        string? statusFilter,
        string? softwareFilter,
        string? assignedToFilter,
        string? customerNameFilter,
        string? dateFrom,
        string? dateTo,
        string? searchTerm,
        string? priorityFilter,
        string? severityFilter,
        string? environmentFilter,
        string? gitStatusFilter,
        string? workItemTypeFilter,
        string? branchFilter,
        string? queue,
        bool unassignedOnly = false,
        bool myItemsOnly = false,
        bool overdueOnly = false)
    {
        var firstName = GetCurrentUserFirstName();
        var lastName = GetCurrentUserLastName();
        var isAdmin = IsAdmin();

        var query = _context.SoftwareIssues
            .Include(i => i.SoftwareNameEntity)
            .Include(i => i.IssueTypeEntity)
            .Include(i => i.ReporterEntity)
            .Include(i => i.AssignedToEntity)
            .Include(i => i.StatusEntity)
            .AsQueryable();

        var today = DateTime.Now.Date;
        var accessibleQuery = query;
        var openCount = accessibleQuery.Count(i => !i.StatusEntity.Value.Contains("تکمیل") && !i.StatusEntity.Value.Contains("لغو"));
        var unassignedCount = accessibleQuery.Count(i => i.AssignedToId == null && !i.StatusEntity.Value.Contains("تکمیل") && !i.StatusEntity.Value.Contains("لغو"));
        var urgentCount = accessibleQuery.Count(i => (i.Priority == "Urgent" || i.Priority == "High") && !i.StatusEntity.Value.Contains("تکمیل") && !i.StatusEntity.Value.Contains("لغو"));
        var overdueCount = accessibleQuery.Count(i => i.DueDate.HasValue && i.DueDate.Value < today && !i.StatusEntity.Value.Contains("تکمیل") && !i.StatusEntity.Value.Contains("لغو"));
        var myItemsCount = accessibleQuery.Count(i => i.AssignedToEntity != null && i.AssignedToEntity.FirstName == firstName && i.AssignedToEntity.LastName == lastName && !i.StatusEntity.Value.Contains("تکمیل") && !i.StatusEntity.Value.Contains("لغو"));
        var reportedByMeCount = accessibleQuery.Count(i => i.ReporterEntity.FirstName == firstName && i.ReporterEntity.LastName == lastName);

        var selectedQueue = string.IsNullOrWhiteSpace(queue) ? "my" : queue;

        if (!string.IsNullOrWhiteSpace(selectedQueue))
        {
            query = selectedQueue switch
            {
                "support" => query.Where(i =>
                    (i.AssignedToId == null || i.Priority == "Urgent" || i.Priority == "High" ||
                     (i.DueDate.HasValue && i.DueDate.Value < today)) &&
                    !i.StatusEntity.Value.Contains("تکمیل") &&
                    !i.StatusEntity.Value.Contains("لغو")),
                "it" => query.Where(i =>
                    (i.BranchName != null && i.BranchName != "") ||
                    (i.RepositoryUrl != null && i.RepositoryUrl != "") ||
                    (i.CommitUrl != null && i.CommitUrl != "") ||
                    (i.WorkItemType != null && i.WorkItemType != "") ||
                    (i.GitStatus != null && i.GitStatus != "") ||
                    (i.Environment != null && i.Environment != "")),
                "my" => query.Where(i => i.AssignedToEntity != null && i.AssignedToEntity.FirstName == firstName && i.AssignedToEntity.LastName == lastName),
                "reported" => query.Where(i => i.ReporterEntity.FirstName == firstName && i.ReporterEntity.LastName == lastName),
                "unassigned" => query.Where(i => i.AssignedToId == null),
                "urgent" => query.Where(i => i.Priority == "Urgent" || i.Priority == "High"),
                "overdue" => query.Where(i => i.DueDate.HasValue && i.DueDate.Value < today && !i.StatusEntity.Value.Contains("تکمیل") && !i.StatusEntity.Value.Contains("لغو")),
                "open" => query.Where(i => !i.StatusEntity.Value.Contains("تکمیل") && !i.StatusEntity.Value.Contains("لغو")),
                _ => query
            };
        }

        if (myItemsOnly)
        {
            query = query.Where(i => i.AssignedToEntity != null &&
                i.AssignedToEntity.FirstName == firstName &&
                i.AssignedToEntity.LastName == lastName);
        }

        if (unassignedOnly)
        {
            query = query.Where(i => i.AssignedToId == null);
        }

        if (overdueOnly)
        {
            query = query.Where(i => i.DueDate.HasValue &&
                i.DueDate.Value < today &&
                !i.StatusEntity.Value.Contains("تکمیل") &&
                !i.StatusEntity.Value.Contains("لغو"));
        }

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            query = query.Where(i =>
                i.Title.Contains(searchTerm) ||
                i.Description.Contains(searchTerm) ||
                (i.CustomerName != null && i.CustomerName.Contains(searchTerm)) ||
                (i.ReportedChannelUrl != null && i.ReportedChannelUrl.Contains(searchTerm)));
        }

        // فیلتر بر اساس وضعیت
        if (!string.IsNullOrWhiteSpace(statusFilter))
        {
            query = query.Where(i => i.StatusEntity.Value == statusFilter);
        }

        // فیلتر بر اساس نرم افزار (تطابق دقیق)
        if (!string.IsNullOrWhiteSpace(softwareFilter))
        {
            query = query.Where(i => i.SoftwareNameEntity.Value == softwareFilter);
        }

        // فیلتر بر اساس مسئول
        if (!string.IsNullOrWhiteSpace(assignedToFilter))
        {
            var nameParts = assignedToFilter.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (nameParts.Length >= 2)
            {
                query = query.Where(i => i.AssignedToEntity != null && 
                                        i.AssignedToEntity.FirstName == nameParts[0] && 
                                        i.AssignedToEntity.LastName == string.Join(" ", nameParts.Skip(1)));
            }
        }

        // فیلتر بر اساس نام مشتری
        if (!string.IsNullOrWhiteSpace(customerNameFilter))
        {
            query = query.Where(i => i.CustomerName != null && i.CustomerName.Contains(customerNameFilter));
        }

        if (!string.IsNullOrWhiteSpace(priorityFilter))
        {
            query = query.Where(i => i.Priority == priorityFilter);
        }

        if (!string.IsNullOrWhiteSpace(severityFilter))
        {
            query = query.Where(i => i.Severity == severityFilter);
        }

        if (!string.IsNullOrWhiteSpace(environmentFilter))
        {
            query = query.Where(i => i.Environment == environmentFilter);
        }

        if (!string.IsNullOrWhiteSpace(gitStatusFilter))
        {
            query = query.Where(i => i.GitStatus == gitStatusFilter);
        }

        if (!string.IsNullOrWhiteSpace(workItemTypeFilter))
        {
            query = query.Where(i => i.WorkItemType == workItemTypeFilter);
        }

        if (!string.IsNullOrWhiteSpace(branchFilter))
        {
            query = query.Where(i => i.BranchName != null && i.BranchName.Contains(branchFilter));
        }

        // فیلتر بر اساس تاریخ ثبت (از)
        if (!string.IsNullOrWhiteSpace(dateFrom) && DateTime.TryParse(dateFrom, out var fromDate))
        {
            query = query.Where(i => i.CreatedAt >= fromDate.Date);
        }

        // فیلتر بر اساس تاریخ ثبت (تا)
        if (!string.IsNullOrWhiteSpace(dateTo) && DateTime.TryParse(dateTo, out var toDate))
        {
            query = query.Where(i => i.CreatedAt <= toDate.Date.AddDays(1).AddTicks(-1));
        }

        var issues = query.OrderByDescending(i => i.CreatedAt).ToList();
        var issueIds = issues.Select(i => i.Id).ToList();
        var lastCommentDates = _context.IssueComments
            .Where(c => issueIds.Contains(c.IssueId))
            .GroupBy(c => c.IssueId)
            .Select(g => new { IssueId = g.Key, LastCommentAt = g.Max(c => c.CommentedAt) })
            .ToDictionary(x => x.IssueId, x => x.LastCommentAt);
        var lastActivityAt = issues.ToDictionary(
            issue => issue.Id,
            issue => lastCommentDates.TryGetValue(issue.Id, out var lastCommentAt) && lastCommentAt > issue.CreatedAt
                ? lastCommentAt
                : issue.CreatedAt);

        // خواندن از دیتابیس به جای JSON
        var activeUsers = _context.Users
            .Where(u => u.IsActive)
            .Select(u => new NameItem { FirstName = u.FirstName, LastName = u.LastName, IsActive = u.IsActive })
            .ToList();
        var activeIssueUsers = _context.Users
            .Where(u => u.IsActive)
            .OrderBy(u => u.FirstName)
            .ThenBy(u => u.LastName)
            .ToList();
        var softwareNames = _context.SoftwareNames
            .Where(sn => sn.IsActive)
            .Select(sn => sn.Value)
            .ToList();
        var statusOptions = _context.Statuses
            .Where(s => s.IsActive)
            .Select(s => s.Value)
            .ToList();

        var canEditIssues = HttpContext.Session.GetString("CanEditIssues") == "true";
        var canDeleteIssues = HttpContext.Session.GetString("CanDeleteIssues") == "true";

        var viewModel = new IssuesIndexViewModel
        {
            Issues = issues,
            StatusFilter = statusFilter,
            SoftwareFilter = softwareFilter,
            AssignedToFilter = assignedToFilter,
            CustomerNameFilter = customerNameFilter,
            DateFrom = dateFrom,
            DateTo = dateTo,
            SearchTerm = searchTerm,
            PriorityFilter = priorityFilter,
            SeverityFilter = severityFilter,
            EnvironmentFilter = environmentFilter,
            GitStatusFilter = gitStatusFilter,
            WorkItemTypeFilter = workItemTypeFilter,
            BranchFilter = branchFilter,
            Queue = selectedQueue,
            UnassignedOnly = unassignedOnly,
            MyItemsOnly = myItemsOnly,
            OverdueOnly = overdueOnly,
            IsAdmin = isAdmin,
            CanEditIssues = canEditIssues,
            CanDeleteIssues = canDeleteIssues,
            ActiveUsers = activeUsers,
            ActiveIssueUsers = activeIssueUsers,
            SoftwareNames = softwareNames,
            StatusOptions = statusOptions,
            LastActivityAt = lastActivityAt,
            OpenCount = openCount,
            UnassignedCount = unassignedCount,
            UrgentCount = urgentCount,
            OverdueCount = overdueCount,
            MyItemsCount = myItemsCount,
            ReportedByMeCount = reportedByMeCount
        };

        return View(viewModel);
    }

    // نمایش جزئیات مشکل
    public IActionResult Details(int id)
    {
        var issue = _context.SoftwareIssues
            .Include(i => i.SoftwareNameEntity)
            .Include(i => i.IssueTypeEntity)
            .Include(i => i.ReporterEntity)
            .Include(i => i.AssignedToEntity)
            .Include(i => i.StatusEntity)
            .FirstOrDefault(i => i.Id == id);
        if (issue == null)
        {
            TempData["ErrorMessage"] = "مشکل یافت نشد.";
            return RedirectToAction("Index");
        }

        var firstName = GetCurrentUserFirstName();
        var lastName = GetCurrentUserLastName();
        var isAdmin = IsAdmin();
        var isReporter = issue.ReporterEntity.FirstName == firstName && issue.ReporterEntity.LastName == lastName;
        var isAssigned = issue.AssignedToEntity != null && issue.AssignedToEntity.FirstName == firstName && issue.AssignedToEntity.LastName == lastName;

        // بارگذاری کامنت‌ها
        var comments = _context.IssueComments
            .Where(c => c.IssueId == id)
            .OrderBy(c => c.CommentedAt)
            .ToList();

        // بارگذاری فایل‌های ضمیمه Issue
        var attachments = _context.IssueAttachments
            .Where(a => a.IssueId == id)
            .OrderBy(a => a.UploadedAt)
            .ToList();

        // بارگذاری فایل‌های ضمیمه کامنت‌ها
        var commentIds = comments.Select(c => c.Id).ToList();
        var commentAttachments = _context.CommentAttachments
            .Where(ca => commentIds.Contains(ca.CommentId))
            .ToList()
            .GroupBy(ca => ca.CommentId)
            .ToDictionary(g => g.Key, g => g.ToList());

        // خواندن از دیتابیس به جای JSON
        var activeUsers = _context.Users
            .Where(u => u.IsActive)
            .Select(u => new NameItem { FirstName = u.FirstName, LastName = u.LastName, IsActive = u.IsActive })
            .ToList();
        var statusOptions = _context.Statuses
            .Where(s => s.IsActive)
            .Select(s => s.Value)
            .ToList();

        var canEditIssues = HttpContext.Session.GetString("CanEditIssues") == "true";
        var canDeleteIssues = HttpContext.Session.GetString("CanDeleteIssues") == "true";

        var viewModel = new IssueDetailsViewModel
        {
            Issue = issue,
            IsAdmin = isAdmin,
            IsAssigned = isAssigned,
            CanEditIssues = canEditIssues,
            CanDeleteIssues = canDeleteIssues,
            ActiveUsers = activeUsers,
            StatusOptions = statusOptions,
            Comments = comments,
            Attachments = attachments,
            CommentAttachments = commentAttachments
        };

        return View(viewModel);
    }

    // فرم ثبت مشکل جدید
    [HttpGet]
    public IActionResult Create()
    {
        // خواندن از دیتابیس به جای JSON
        var softwareNames = _context.SoftwareNames
            .Where(sn => sn.IsActive)
            .Select(sn => sn.Value)
            .ToList();
        
        var issueTypes = _context.IssueTypes
            .Where(it => it.IsActive)
            .Select(it => it.Value)
            .ToList();

        var activeUsers = _context.Users
            .Where(u => u.IsActive)
            .Select(u => new NameItem { FirstName = u.FirstName, LastName = u.LastName, IsActive = u.IsActive })
            .ToList();

        var viewModel = new CreateIssueViewModel
        {
            SoftwareNames = softwareNames,
            IssueTypes = issueTypes,
            ActiveUsers = activeUsers
        };

        return View(viewModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateIssueViewModel model)
    {
        if (string.IsNullOrWhiteSpace(model.Title))
        {
            TempData["ErrorMessage"] = "لطفاً عنوان را وارد کنید.";
            return View(model);
        }

        if (string.IsNullOrWhiteSpace(model.Description))
        {
            TempData["ErrorMessage"] = "لطفاً شرح را وارد کنید.";
            return View(model);
        }

        if (string.IsNullOrWhiteSpace(model.SoftwareName))
        {
            TempData["ErrorMessage"] = "لطفاً نام نرم افزار را انتخاب کنید.";
            return View(model);
        }

        if (string.IsNullOrWhiteSpace(model.IssueType))
        {
            TempData["ErrorMessage"] = "لطفاً نوع مشکل/نظر را انتخاب کنید.";
            return View(model);
        }

        // Validation از دیتابیس
        var validSoftwareNames = _context.SoftwareNames
            .Where(sn => sn.IsActive)
            .Select(sn => sn.Value)
            .ToList();
        
        var validIssueTypes = _context.IssueTypes
            .Where(it => it.IsActive)
            .Select(it => it.Value)
            .ToList();

        if (!validSoftwareNames.Contains(model.SoftwareName))
        {
            TempData["ErrorMessage"] = "نام نرم افزار انتخاب شده معتبر نیست.";
            // بارگذاری مجدد داده‌ها برای نمایش فرم
            var softwareNames = _context.SoftwareNames
                .Where(sn => sn.IsActive)
                .Select(sn => sn.Value)
                .ToList();
            var issueTypes = _context.IssueTypes
                .Where(it => it.IsActive)
                .Select(it => it.Value)
                .ToList();
            var activeUsers = _context.Users
                .Where(u => u.IsActive)
                .Select(u => new NameItem { FirstName = u.FirstName, LastName = u.LastName, IsActive = u.IsActive })
                .ToList();
            model.SoftwareNames = softwareNames;
            model.IssueTypes = issueTypes;
            model.ActiveUsers = activeUsers;
            return View(model);
        }

        if (!validIssueTypes.Contains(model.IssueType))
        {
            TempData["ErrorMessage"] = "نوع مشکل/نظر انتخاب شده معتبر نیست.";
            // بارگذاری مجدد داده‌ها برای نمایش فرم
            var softwareNames = _context.SoftwareNames
                .Where(sn => sn.IsActive)
                .Select(sn => sn.Value)
                .ToList();
            var issueTypes = _context.IssueTypes
                .Where(it => it.IsActive)
                .Select(it => it.Value)
                .ToList();
            var activeUsers = _context.Users
                .Where(u => u.IsActive)
                .Select(u => new NameItem { FirstName = u.FirstName, LastName = u.LastName, IsActive = u.IsActive })
                .ToList();
            model.SoftwareNames = softwareNames;
            model.IssueTypes = issueTypes;
            model.ActiveUsers = activeUsers;
            return View(model);
        }

        var firstName = GetCurrentUserFirstName();
        var lastName = GetCurrentUserLastName();

        // پیدا کردن SoftwareName
        var softwareName = _context.SoftwareNames.FirstOrDefault(sn => sn.Value == model.SoftwareName && sn.IsActive);
        if (softwareName == null)
        {
            TempData["ErrorMessage"] = "نام نرم افزار انتخاب شده یافت نشد.";
            return View(model);
        }

        // پیدا کردن IssueType
        var issueType = _context.IssueTypes.FirstOrDefault(it => it.Value == model.IssueType && it.IsActive);
        if (issueType == null)
        {
            TempData["ErrorMessage"] = "نوع مشکل/نظر انتخاب شده یافت نشد.";
            return View(model);
        }

        // پیدا کردن Status (پیش‌فرض: "ثبت شده")
        var status = _context.Statuses.FirstOrDefault(s => s.Value == "ثبت شده" && s.IsActive);
        if (status == null)
        {
            TempData["ErrorMessage"] = "وضعیت پیش‌فرض یافت نشد.";
            return View(model);
        }

        // پیدا کردن Reporter (کاربر فعلی)
        var reporter = _context.Users.FirstOrDefault(u => u.FirstName == firstName && u.LastName == lastName);
        if (reporter == null)
        {
            TempData["ErrorMessage"] = "کاربر یافت نشد.";
            return View(model);
        }

        // پیدا کردن AssignedTo (اگر مشخص شده باشد)
        User? assignedTo = null;
        if (!string.IsNullOrWhiteSpace(model.AssignedToFirstName) && !string.IsNullOrWhiteSpace(model.AssignedToLastName))
        {
            assignedTo = _context.Users.FirstOrDefault(u => u.FirstName == model.AssignedToFirstName && u.LastName == model.AssignedToLastName);
        }

        var issue = new SoftwareIssue
        {
            Title = SanitizeInput(model.Title),
            Description = SanitizeInput(model.Description),
            CustomerName = !string.IsNullOrWhiteSpace(model.CustomerName) ? SanitizeInput(model.CustomerName) : null,
            BranchName = !string.IsNullOrWhiteSpace(model.BranchName) ? SanitizeInput(model.BranchName) : null,
            RepositoryUrl = !string.IsNullOrWhiteSpace(model.RepositoryUrl) ? SanitizeInput(model.RepositoryUrl) : null,
            CommitUrl = !string.IsNullOrWhiteSpace(model.CommitUrl) ? SanitizeInput(model.CommitUrl) : null,
            WorkItemType = !string.IsNullOrWhiteSpace(model.WorkItemType) ? SanitizeInput(model.WorkItemType) : null,
            GitStatus = !string.IsNullOrWhiteSpace(model.GitStatus) ? SanitizeInput(model.GitStatus) : null,
            Priority = !string.IsNullOrWhiteSpace(model.Priority) ? SanitizeInput(model.Priority) : null,
            Severity = !string.IsNullOrWhiteSpace(model.Severity) ? SanitizeInput(model.Severity) : null,
            DueDate = model.DueDate,
            Environment = !string.IsNullOrWhiteSpace(model.Environment) ? SanitizeInput(model.Environment) : null,
            ReportedChannel = !string.IsNullOrWhiteSpace(model.ReportedChannel) ? SanitizeInput(model.ReportedChannel) : null,
            ReportedChannelUrl = !string.IsNullOrWhiteSpace(model.ReportedChannelUrl) ? SanitizeInput(model.ReportedChannelUrl) : null,
            SoftwareNameId = softwareName.Id,
            IssueTypeId = issueType.Id,
            ReporterId = reporter.Id,
            AssignedToId = assignedTo?.Id,
            StatusId = status.Id,
            CreatedAt = DateTime.Now
        };

        _context.SoftwareIssues.Add(issue);
        _context.SaveChanges();

        // پردازش فایل‌های ضمیمه
        var uploadedFiles = Request.Form.Files.Where(f => f.Name == "attachments" && f.Length > 0).ToList();
        if (uploadedFiles.Any())
        {
            var uploadsFolder = Path.Combine(_environment.WebRootPath, "uploads", "issues", issue.Id.ToString());
            if (!Directory.Exists(uploadsFolder))
            {
                Directory.CreateDirectory(uploadsFolder);
            }

            foreach (var file in uploadedFiles)
            {
                if (!IsAllowedAttachment(file, out var contentType, out var errorMessage))
                {
                    TempData["ErrorMessage"] = errorMessage;
                    continue;
                }

                // ایجاد نام یکتا برای فایل
                var fileExtension = Path.GetExtension(file.FileName);
                var storedFileName = $"{Guid.NewGuid()}{fileExtension}";
                var filePath = Path.Combine(uploadsFolder, storedFileName);

                // ذخیره فایل
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    file.CopyTo(stream);
                }

                // ذخیره اطلاعات فایل در دیتابیس
                var attachment = new IssueAttachment
                {
                    IssueId = issue.Id,
                    FileName = Path.GetFileName(file.FileName),
                    StoredFileName = storedFileName,
                    FilePath = filePath,
                    ContentType = contentType,
                    FileSize = file.Length,
                    UploadedAt = DateTime.Now,
                    UploadedByFirstName = firstName,
                    UploadedByLastName = lastName
                };

                _context.IssueAttachments.Add(attachment);
            }

            _context.SaveChanges();
        }

        _logService.LogAction(
            firstName,
            lastName,
            "Create",
            "SoftwareIssue",
            issue.Id.ToString(),
            $"مشکل/نظر جدید ثبت شد: {issue.Title}"
        );

        try
        {
            _context.Entry(issue).Reference(i => i.SoftwareNameEntity).Load();
            _context.Entry(issue).Reference(i => i.IssueTypeEntity).Load();
            _context.Entry(issue).Reference(i => i.ReporterEntity).Load();
            _context.Entry(issue).Reference(i => i.AssignedToEntity).Load();
            _context.Entry(issue).Reference(i => i.StatusEntity).Load();
            await _telegramNotification.NotifyIssueCreatedAsync(issue);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "ارسال نوتیفیکیشن تلگرام برای مشکل جدید با خطا مواجه شد.");
        }

        TempData["SuccessMessage"] = "مشکل/نظر با موفقیت ثبت شد.";
        return RedirectToAction("Index");
    }

    // ویرایش مشکل (فقط برای کاربران با دسترسی)
    [HttpGet]
    public IActionResult Edit(int id)
    {
        var canEditIssues = HttpContext.Session.GetString("CanEditIssues") == "true";
        var issue = _context.SoftwareIssues
            .Include(i => i.SoftwareNameEntity)
            .Include(i => i.IssueTypeEntity)
            .Include(i => i.ReporterEntity)
            .Include(i => i.AssignedToEntity)
            .Include(i => i.StatusEntity)
            .FirstOrDefault(i => i.Id == id);
        if (issue == null)
        {
            TempData["ErrorMessage"] = "مشکل یافت نشد.";
            return RedirectToAction("Index");
        }

        var canEditFullIssue = canEditIssues || IsAdmin();
        var canEditGitDetails = canEditFullIssue || CanUseIssueQuickActions(issue);
        if (!canEditGitDetails)
        {
            TempData["ErrorMessage"] = "شما دسترسی به ویرایش این مشکل ندارید.";
            return RedirectToAction("Index");
        }

        // خواندن از دیتابیس به جای JSON
        var activeUsers = _context.Users
            .Where(u => u.IsActive)
            .Select(u => new NameItem { FirstName = u.FirstName, LastName = u.LastName, IsActive = u.IsActive })
            .ToList();
        var softwareNames = _context.SoftwareNames
            .Where(sn => sn.IsActive)
            .Select(sn => sn.Value)
            .ToList();
        var issueTypes = _context.IssueTypes
            .Where(it => it.IsActive)
            .Select(it => it.Value)
            .ToList();
        var statusOptions = _context.Statuses
            .Where(s => s.IsActive)
            .Select(s => s.Value)
            .ToList();

        var viewModel = new EditIssueViewModel
        {
            Issue = issue,
            ActiveUsers = activeUsers,
            SoftwareNames = softwareNames,
            IssueTypes = issueTypes,
            StatusOptions = statusOptions,
            CanEditFullIssue = canEditFullIssue,
            CanEditGitDetails = canEditGitDetails
        };

        return View(viewModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(EditIssueViewModel model, string? returnUrl = null)
    {
        var canEditIssues = HttpContext.Session.GetString("CanEditIssues") == "true";
        var issue = _context.SoftwareIssues
            .Include(i => i.SoftwareNameEntity)
            .Include(i => i.IssueTypeEntity)
            .Include(i => i.ReporterEntity)
            .Include(i => i.AssignedToEntity)
            .Include(i => i.StatusEntity)
            .FirstOrDefault(i => i.Id == model.Issue.Id);
        if (issue == null)
        {
            TempData["ErrorMessage"] = "مشکل یافت نشد.";
            return RedirectToAction("Index");
        }

        var firstName = GetCurrentUserFirstName();
        var lastName = GetCurrentUserLastName();
        var canEditFullIssue = canEditIssues || IsAdmin();
        var canEditGitDetails = canEditFullIssue || CanUseIssueQuickActions(issue);
        if (!canEditGitDetails)
        {
            TempData["ErrorMessage"] = "شما دسترسی به ویرایش این مشکل ندارید.";
            return RedirectToAction("Index");
        }

        if (!canEditFullIssue)
        {
            var oldBranchName = issue.BranchName;
            var oldRepositoryUrl = issue.RepositoryUrl;
            var oldCommitUrl = issue.CommitUrl;
            var oldWorkItemType = issue.WorkItemType;
            var oldGitStatus = issue.GitStatus;

            issue.BranchName = !string.IsNullOrWhiteSpace(model.Issue.BranchName) ? SanitizeInput(model.Issue.BranchName) : null;
            issue.RepositoryUrl = !string.IsNullOrWhiteSpace(model.Issue.RepositoryUrl) ? SanitizeInput(model.Issue.RepositoryUrl) : null;
            issue.CommitUrl = !string.IsNullOrWhiteSpace(model.Issue.CommitUrl) ? SanitizeInput(model.Issue.CommitUrl) : null;
            issue.WorkItemType = !string.IsNullOrWhiteSpace(model.Issue.WorkItemType) ? SanitizeInput(model.Issue.WorkItemType) : null;
            issue.GitStatus = !string.IsNullOrWhiteSpace(model.Issue.GitStatus) ? SanitizeInput(model.Issue.GitStatus) : null;

            _context.SaveChanges();

            var gitChanges = new List<string>();
            if (oldBranchName != issue.BranchName) gitChanges.Add("BranchName");
            if (oldRepositoryUrl != issue.RepositoryUrl) gitChanges.Add("RepositoryUrl");
            if (oldCommitUrl != issue.CommitUrl) gitChanges.Add("CommitUrl");
            if (oldWorkItemType != issue.WorkItemType) gitChanges.Add("WorkItemType");
            if (oldGitStatus != issue.GitStatus) gitChanges.Add("GitStatus");

            _logService.LogAction(
                firstName,
                lastName,
                "UpdateGitDetails",
                "SoftwareIssue",
                issue.Id.ToString(),
                $"جزئیات گیت به‌روزرسانی شد: {(gitChanges.Any() ? string.Join(", ", gitChanges) : "بدون تغییر")}"
            );

            await NotifyIssueUpdatedSafelyAsync(
                issue,
                $"{firstName} {lastName}",
                gitChanges.Any() ? gitChanges.Select(c => $"به‌روزرسانی گیت: {c}") : new[] { "جزئیات گیت بدون تغییر ذخیره شد" },
                "ارسال نوتیفیکیشن تلگرام برای به‌روزرسانی گیت با خطا مواجه شد.");

            TempData["SuccessMessage"] = "جزئیات گیت با موفقیت به‌روزرسانی شد.";
            return RedirectBack(returnUrl ?? Url.Action("Details", new { id = issue.Id }));
        }

        // ذخیره مقادیر قدیمی برای لاگ
        var oldStatus = issue.Status;
        var oldAssignedTo = issue.AssignedToEntity != null 
            ? $"{issue.AssignedToEntity.FirstName} {issue.AssignedToEntity.LastName}" 
            : "بدون مسئول";
        var oldStartedAt = issue.StartedAt;
        var oldCompletedAt = issue.CompletedAt;

        // خواندن مقادیر از Request.Form (چون computed properties نمی‌توانند bind شوند)
        var selectedSoftwareName = Request.Form["Issue.SoftwareName"].ToString();
        var selectedIssueType = Request.Form["Issue.IssueType"].ToString();
        var selectedStatus = Request.Form["Issue.Status"].ToString();
        var assignedToFirstName = Request.Form["Issue.AssignedToFirstName"].ToString();
        var assignedToLastName = Request.Form["Issue.AssignedToLastName"].ToString();

        // پیدا کردن SoftwareName
        if (string.IsNullOrWhiteSpace(selectedSoftwareName))
        {
            TempData["ErrorMessage"] = "لطفاً نام نرم افزار را انتخاب کنید.";
            return RedirectToAction("Edit", new { id = model.Issue.Id });
        }

        var softwareName = _context.SoftwareNames.FirstOrDefault(sn => sn.Value == selectedSoftwareName && sn.IsActive);
        if (softwareName == null)
        {
            TempData["ErrorMessage"] = "نام نرم افزار انتخاب شده یافت نشد.";
            return RedirectToAction("Edit", new { id = model.Issue.Id });
        }

        // پیدا کردن IssueType
        if (string.IsNullOrWhiteSpace(selectedIssueType))
        {
            TempData["ErrorMessage"] = "لطفاً نوع مشکل/نظر را انتخاب کنید.";
            return RedirectToAction("Edit", new { id = model.Issue.Id });
        }

        var issueType = _context.IssueTypes.FirstOrDefault(it => it.Value == selectedIssueType && it.IsActive);
        if (issueType == null)
        {
            TempData["ErrorMessage"] = "نوع مشکل/نظر انتخاب شده یافت نشد.";
            return RedirectToAction("Edit", new { id = model.Issue.Id });
        }

        // پیدا کردن Status
        if (string.IsNullOrWhiteSpace(selectedStatus))
        {
            TempData["ErrorMessage"] = "لطفاً وضعیت را انتخاب کنید.";
            return RedirectToAction("Edit", new { id = model.Issue.Id });
        }

        var status = _context.Statuses.FirstOrDefault(s => s.Value == selectedStatus && s.IsActive);
        if (status == null)
        {
            TempData["ErrorMessage"] = "وضعیت انتخاب شده یافت نشد.";
            return RedirectToAction("Edit", new { id = model.Issue.Id });
        }

        // پیدا کردن AssignedTo (اگر مشخص شده باشد)
        User? assignedTo = null;
        if (!string.IsNullOrWhiteSpace(assignedToFirstName) && !string.IsNullOrWhiteSpace(assignedToLastName))
        {
            assignedTo = _context.Users.FirstOrDefault(u => u.FirstName == assignedToFirstName && u.LastName == assignedToLastName);
        }

        // خواندن CustomerName از Request.Form
        var customerName = Request.Form["Issue.CustomerName"].ToString();

        // به‌روزرسانی فیلدها
        issue.Title = SanitizeInput(model.Issue.Title);
        issue.Description = SanitizeInput(model.Issue.Description);
        issue.CustomerName = !string.IsNullOrWhiteSpace(customerName) ? SanitizeInput(customerName) : null;
        issue.BranchName = !string.IsNullOrWhiteSpace(model.Issue.BranchName) ? SanitizeInput(model.Issue.BranchName) : null;
        issue.RepositoryUrl = !string.IsNullOrWhiteSpace(model.Issue.RepositoryUrl) ? SanitizeInput(model.Issue.RepositoryUrl) : null;
        issue.CommitUrl = !string.IsNullOrWhiteSpace(model.Issue.CommitUrl) ? SanitizeInput(model.Issue.CommitUrl) : null;
        issue.WorkItemType = !string.IsNullOrWhiteSpace(model.Issue.WorkItemType) ? SanitizeInput(model.Issue.WorkItemType) : null;
        issue.GitStatus = !string.IsNullOrWhiteSpace(model.Issue.GitStatus) ? SanitizeInput(model.Issue.GitStatus) : null;
        issue.Priority = !string.IsNullOrWhiteSpace(model.Issue.Priority) ? SanitizeInput(model.Issue.Priority) : null;
        issue.Severity = !string.IsNullOrWhiteSpace(model.Issue.Severity) ? SanitizeInput(model.Issue.Severity) : null;
        issue.DueDate = model.Issue.DueDate;
        issue.Environment = !string.IsNullOrWhiteSpace(model.Issue.Environment) ? SanitizeInput(model.Issue.Environment) : null;
        issue.ReportedChannel = !string.IsNullOrWhiteSpace(model.Issue.ReportedChannel) ? SanitizeInput(model.Issue.ReportedChannel) : null;
        issue.ReportedChannelUrl = !string.IsNullOrWhiteSpace(model.Issue.ReportedChannelUrl) ? SanitizeInput(model.Issue.ReportedChannelUrl) : null;
        issue.SoftwareNameId = softwareName.Id;
        issue.IssueTypeId = issueType.Id;
        issue.StatusId = status.Id;
        issue.AssignedToId = assignedTo?.Id;

        // مدیریت زمان شروع
        if (model.StartWork && !issue.StartedAt.HasValue)
        {
            issue.StartedAt = DateTime.Now;
        }
        else if (!model.StartWork && issue.StartedAt.HasValue)
        {
            issue.StartedAt = null;
        }

        // مدیریت زمان پایان
        if (model.CompleteWork && !issue.CompletedAt.HasValue)
        {
            issue.CompletedAt = DateTime.Now;
            // اگر وضعیت تکمیل شده نیست، آن را تغییر بده
            _context.Entry(issue).Reference(i => i.StatusEntity).Load();
            if (issue.StatusEntity.Value != "تکمیل شده")
            {
                var completedStatus = _context.Statuses.FirstOrDefault(s => s.Value == "تکمیل شده" && s.IsActive);
                if (completedStatus != null)
                {
                    issue.StatusId = completedStatus.Id;
                }
            }
        }
        else if (!model.CompleteWork && issue.CompletedAt.HasValue)
        {
            issue.CompletedAt = null;
        }

        _context.SaveChanges();

        // بارگذاری مجدد navigation properties برای لاگ
        _context.Entry(issue).Reference(i => i.SoftwareNameEntity).Load();
        _context.Entry(issue).Reference(i => i.IssueTypeEntity).Load();
        _context.Entry(issue).Reference(i => i.ReporterEntity).Load();
        _context.Entry(issue).Reference(i => i.StatusEntity).Load();
        _context.Entry(issue).Reference(i => i.AssignedToEntity).Load();

        // ثبت لاگ
        var changes = new List<string>();
        if (oldStatus != issue.Status)
            changes.Add($"تغییر وضعیت:   از {oldStatus} به {issue.Status}");
        var newAssignedTo = issue.AssignedToEntity != null ? $"{issue.AssignedToEntity.FirstName} {issue.AssignedToEntity.LastName}" : "بدون مسئول";
        if (oldAssignedTo != newAssignedTo)
            changes.Add($"تغییر مسئول:    از {oldAssignedTo} به {newAssignedTo}");
        if (oldStartedAt != issue.StartedAt)
            changes.Add($"تغییر زمان شروع:    از {(oldStartedAt?.ToString("yyyy/MM/dd HH:mm") ?? "-")} به {(issue.StartedAt?.ToString("yyyy/MM/dd HH:mm") ?? "-")}");
        if (oldCompletedAt != issue.CompletedAt)
            changes.Add($"تغییر زمان پایان:    از {(oldCompletedAt?.ToString("yyyy/MM/dd HH:mm") ?? "-")} به {(issue.CompletedAt?.ToString("yyyy/MM/dd HH:mm") ?? "-")}");

        _logService.LogAction(
            firstName,
            lastName,
            "Update",
            "SoftwareIssue",
            issue.Id.ToString(),
            $"مشکل به‌روزرسانی شد: {string.Join(", ", changes)}"
        );

        var changedBy = $"{firstName} {lastName}";
        try
        {
            await _telegramNotification.NotifyIssueUpdatedAsync(issue, changedBy, changes);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "ارسال نوتیفیکیشن تلگرام برای به‌روزرسانی مشکل با خطا مواجه شد.");
        }

        TempData["SuccessMessage"] = "مشکل با موفقیت به‌روزرسانی شد.";
        return RedirectBack(returnUrl ?? Url.Action("Details", new { id = issue.Id }));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> QuickUpdateStatus(int issueId, string status, string? returnUrl = null)
    {
        var issue = _context.SoftwareIssues
            .Include(i => i.ReporterEntity)
            .Include(i => i.AssignedToEntity)
            .Include(i => i.StatusEntity)
            .FirstOrDefault(i => i.Id == issueId);

        if (issue == null)
        {
            TempData["ErrorMessage"] = "مشکل یافت نشد.";
            return RedirectBack(returnUrl);
        }

        if (!CanUseIssueQuickActions(issue))
        {
            TempData["ErrorMessage"] = "شما دسترسی به تغییر سریع این مشکل ندارید.";
            return RedirectBack(returnUrl);
        }

        var newStatus = _context.Statuses.FirstOrDefault(s => s.Value == status && s.IsActive);
        if (newStatus == null)
        {
            TempData["ErrorMessage"] = "وضعیت انتخاب‌شده معتبر نیست.";
            return RedirectBack(returnUrl);
        }

        var oldStatus = issue.Status;
        issue.StatusId = newStatus.Id;

        if (status.Contains("انجام") && !issue.StartedAt.HasValue)
        {
            issue.StartedAt = DateTime.Now;
        }

        if (status.Contains("تکمیل") && !issue.CompletedAt.HasValue)
        {
            issue.CompletedAt = DateTime.Now;
        }

        _context.SaveChanges();

        var firstName = GetCurrentUserFirstName();
        var lastName = GetCurrentUserLastName();
        _logService.LogAction(firstName, lastName, "QuickUpdateStatus", "SoftwareIssue", issue.Id.ToString(), $"وضعیت از '{oldStatus}' به '{status}' تغییر کرد.");

        await NotifyIssueUpdatedSafelyAsync(
            issue,
            $"{firstName} {lastName}",
            new[] { $"تغییر سریع وضعیت: از {oldStatus} به {status}" },
            "ارسال نوتیفیکیشن تلگرام برای تغییر سریع وضعیت با خطا مواجه شد.");

        TempData["SuccessMessage"] = "وضعیت با موفقیت به‌روزرسانی شد.";
        return RedirectBack(returnUrl);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> QuickAssign(int issueId, int? assignedToId, string? returnUrl = null)
    {
        var issue = _context.SoftwareIssues
            .Include(i => i.SoftwareNameEntity)
            .Include(i => i.IssueTypeEntity)
            .Include(i => i.ReporterEntity)
            .Include(i => i.AssignedToEntity)
            .Include(i => i.StatusEntity)
            .FirstOrDefault(i => i.Id == issueId);

        if (issue == null)
        {
            TempData["ErrorMessage"] = "مشکل یافت نشد.";
            return RedirectBack(returnUrl);
        }

        if (!CanUseIssueQuickActions(issue))
        {
            TempData["ErrorMessage"] = "شما دسترسی به ارجاع این مشکل ندارید.";
            return RedirectBack(returnUrl);
        }

        User? assignedTo = null;
        if (assignedToId.HasValue)
        {
            assignedTo = _context.Users.FirstOrDefault(u => u.Id == assignedToId.Value && u.IsActive);
            if (assignedTo == null)
            {
                TempData["ErrorMessage"] = "مسئول انتخاب‌شده معتبر نیست.";
                return RedirectBack(returnUrl);
            }
        }

        var oldAssignedTo = issue.AssignedToEntity != null ? $"{issue.AssignedToEntity.FirstName} {issue.AssignedToEntity.LastName}" : "بدون مسئول";
        issue.AssignedToId = assignedTo?.Id;
        _context.SaveChanges();

        var newAssignedTo = assignedTo != null ? $"{assignedTo.FirstName} {assignedTo.LastName}" : "بدون مسئول";
        var firstName = GetCurrentUserFirstName();
        var lastName = GetCurrentUserLastName();
        _logService.LogAction(firstName, lastName, "QuickAssign", "SoftwareIssue", issue.Id.ToString(), $"مسئول از '{oldAssignedTo}' به '{newAssignedTo}' تغییر کرد.");

        await NotifyIssueUpdatedSafelyAsync(
            issue,
            $"{firstName} {lastName}",
            new[] { $"ارجاع سریع: از {oldAssignedTo} به {newAssignedTo}" },
            "ارسال نوتیفیکیشن تلگرام برای ارجاع سریع با خطا مواجه شد.");

        TempData["SuccessMessage"] = "مسئول با موفقیت به‌روزرسانی شد.";
        return RedirectBack(returnUrl);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> QuickStart(int issueId, string? returnUrl = null)
    {
        var issue = _context.SoftwareIssues
            .Include(i => i.SoftwareNameEntity)
            .Include(i => i.IssueTypeEntity)
            .Include(i => i.ReporterEntity)
            .Include(i => i.AssignedToEntity)
            .Include(i => i.StatusEntity)
            .FirstOrDefault(i => i.Id == issueId);

        if (issue == null)
        {
            TempData["ErrorMessage"] = "مشکل یافت نشد.";
            return RedirectBack(returnUrl);
        }

        if (!CanUseIssueQuickActions(issue))
        {
            TempData["ErrorMessage"] = "شما دسترسی به شروع کار روی این مشکل ندارید.";
            return RedirectBack(returnUrl);
        }

        issue.StartedAt ??= DateTime.Now;
        var inProgressStatus = _context.Statuses.FirstOrDefault(s => s.IsActive && s.Value.Contains("انجام"));
        if (inProgressStatus != null)
        {
            issue.StatusId = inProgressStatus.Id;
        }

        _context.SaveChanges();

        var firstName = GetCurrentUserFirstName();
        var lastName = GetCurrentUserLastName();
        _logService.LogAction(firstName, lastName, "QuickStart", "SoftwareIssue", issue.Id.ToString(), "کار روی مشکل شروع شد.");

        await NotifyIssueUpdatedSafelyAsync(
            issue,
            $"{firstName} {lastName}",
            new[] { "شروع کار ثبت شد" },
            "ارسال نوتیفیکیشن تلگرام برای شروع کار با خطا مواجه شد.");

        TempData["SuccessMessage"] = "شروع کار ثبت شد.";
        return RedirectBack(returnUrl);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> QuickComplete(int issueId, string? returnUrl = null)
    {
        var issue = _context.SoftwareIssues
            .Include(i => i.SoftwareNameEntity)
            .Include(i => i.IssueTypeEntity)
            .Include(i => i.ReporterEntity)
            .Include(i => i.AssignedToEntity)
            .Include(i => i.StatusEntity)
            .FirstOrDefault(i => i.Id == issueId);

        if (issue == null)
        {
            TempData["ErrorMessage"] = "مشکل یافت نشد.";
            return RedirectBack(returnUrl);
        }

        if (!CanUseIssueQuickActions(issue))
        {
            TempData["ErrorMessage"] = "شما دسترسی به تکمیل این مشکل ندارید.";
            return RedirectBack(returnUrl);
        }

        issue.StartedAt ??= DateTime.Now;
        issue.CompletedAt ??= DateTime.Now;
        var completedStatus = _context.Statuses.FirstOrDefault(s => s.IsActive && s.Value.Contains("تکمیل"));
        if (completedStatus != null)
        {
            issue.StatusId = completedStatus.Id;
        }

        _context.SaveChanges();

        var firstName = GetCurrentUserFirstName();
        var lastName = GetCurrentUserLastName();
        _logService.LogAction(firstName, lastName, "QuickComplete", "SoftwareIssue", issue.Id.ToString(), "مشکل به عنوان تکمیل‌شده ثبت شد.");

        await NotifyIssueUpdatedSafelyAsync(
            issue,
            $"{firstName} {lastName}",
            new[] { "تکمیل کار ثبت شد" },
            "ارسال نوتیفیکیشن تلگرام برای تکمیل کار با خطا مواجه شد.");

        TempData["SuccessMessage"] = "تکمیل کار ثبت شد.";
        return RedirectBack(returnUrl);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> QuickAddComment(int issueId, string commentText, string? returnUrl = null)
    {
        if (string.IsNullOrWhiteSpace(commentText))
        {
            TempData["ErrorMessage"] = "متن کامنت را وارد کنید.";
            return RedirectBack(returnUrl);
        }

        var issue = _context.SoftwareIssues
            .Include(i => i.ReporterEntity)
            .Include(i => i.AssignedToEntity)
            .FirstOrDefault(i => i.Id == issueId);

        if (issue == null)
        {
            TempData["ErrorMessage"] = "مشکل یافت نشد.";
            return RedirectBack(returnUrl);
        }

        if (!CanViewCurrentIssue(issue))
        {
            TempData["ErrorMessage"] = "شما دسترسی به افزودن کامنت برای این مشکل ندارید.";
            return RedirectBack(returnUrl);
        }

        var firstName = GetCurrentUserFirstName();
        var lastName = GetCurrentUserLastName();
        var comment = new IssueComment
        {
            IssueId = issue.Id,
            CommentText = SanitizeInput(commentText),
            CommenterFirstName = firstName,
            CommenterLastName = lastName,
            CommentedAt = DateTime.Now
        };

        _context.IssueComments.Add(comment);
        _context.SaveChanges();
        _logService.LogAction(firstName, lastName, "QuickAddComment", "SoftwareIssue", issue.Id.ToString(), $"کامنت سریع به مشکل #{issue.Id} اضافه شد.");

        try
        {
            LoadTelegramIssueReferences(issue);
            await _telegramNotification.NotifyCommentAddedAsync(issue, $"{firstName} {lastName}", comment.CommentText);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "ارسال نوتیفیکیشن تلگرام برای کامنت سریع با خطا مواجه شد.");
        }

        TempData["SuccessMessage"] = "کامنت سریع ثبت شد.";
        return RedirectBack(returnUrl);
    }

    // حذف مشکل (فقط برای کاربران با دسترسی)
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id, string? returnUrl = null)
    {
        var canDeleteIssues = HttpContext.Session.GetString("CanDeleteIssues") == "true";
        if (!canDeleteIssues && !IsAdmin())
        {
            TempData["ErrorMessage"] = "شما دسترسی به حذف مشکلات ندارید.";
            return RedirectBack(returnUrl);
        }

        var issue = _context.SoftwareIssues
            .Include(i => i.ReporterEntity)
            .Include(i => i.AssignedToEntity)
            .FirstOrDefault(i => i.Id == id);
        if (issue == null)
        {
            TempData["ErrorMessage"] = "مشکل یافت نشد.";
            return RedirectBack(returnUrl);
        }

        if (!CanUseIssueQuickActions(issue))
        {
            TempData["ErrorMessage"] = "شما دسترسی به حذف این مشکل ندارید.";
            return RedirectBack(returnUrl);
        }

        var firstName = GetCurrentUserFirstName();
        var lastName = GetCurrentUserLastName();
        var issueTitle = issue.Title;

        await NotifyIssueDeletedSafelyAsync(issue, $"{firstName} {lastName}");

        _context.SoftwareIssues.Remove(issue);
        _context.SaveChanges();

        _logService.LogAction(
            firstName,
            lastName,
            "Delete",
            "SoftwareIssue",
            id.ToString(),
            $"مشکل حذف شد: {issueTitle}"
        );

        TempData["SuccessMessage"] = "مشکل با موفقیت حذف شد.";
        return RedirectBack(returnUrl);
    }

    // مشکلات انتساب داده شده به من (برای مسئول)
    public IActionResult MyAssignedIssues()
    {
        var firstName = GetCurrentUserFirstName();
        var lastName = GetCurrentUserLastName();

        var issues = _context.SoftwareIssues
            .Include(i => i.SoftwareNameEntity)
            .Include(i => i.IssueTypeEntity)
            .Include(i => i.ReporterEntity)
            .Include(i => i.AssignedToEntity)
            .Include(i => i.StatusEntity)
            .Where(i => i.AssignedToEntity != null && i.AssignedToEntity.FirstName == firstName && i.AssignedToEntity.LastName == lastName)
            .OrderByDescending(i => i.CreatedAt)
            .ToList();

        // بارگذاری کامنت‌ها
        var issueIds = issues.Select(i => i.Id).ToList();
        var comments = _context.IssueComments
            .Where(c => issueIds.Contains(c.IssueId))
            .OrderBy(c => c.CommentedAt)
            .ToList()
            .GroupBy(c => c.IssueId)
            .ToDictionary(g => g.Key, g => g.ToList());

        ViewBag.Comments = comments;

        var viewModel = new MyAssignedIssuesViewModel
        {
            Issues = issues,
            StatusOptions = new List<string> { "ثبت شده", "در حال بررسی", "در حال انجام", "تکمیل شده", "لغو شده" }
        };

        return View(viewModel);
    }

    // افزودن کامنت
    [HttpGet]
    public IActionResult AddComment()
    {
        TempData["ErrorMessage"] = "برای افزودن کامنت، از فرم کامنت داخل صفحه جزئیات استفاده کنید.";
        return RedirectToAction("Index");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddComment(int issueId, string commentText, string? returnUrl = null)
    {
        if (string.IsNullOrWhiteSpace(commentText))
        {
            TempData["ErrorMessage"] = "لطفاً متن کامنت را وارد کنید.";
            return RedirectBack(returnUrl ?? Url.Action("Details", new { id = issueId }));
        }

        var issue = _context.SoftwareIssues
            .Include(i => i.SoftwareNameEntity)
            .Include(i => i.IssueTypeEntity)
            .Include(i => i.ReporterEntity)
            .Include(i => i.AssignedToEntity)
            .Include(i => i.StatusEntity)
            .FirstOrDefault(i => i.Id == issueId);
        if (issue == null)
        {
            TempData["ErrorMessage"] = "مشکل یافت نشد.";
            return RedirectBack(returnUrl);
        }

        if (!CanViewCurrentIssue(issue))
        {
            TempData["ErrorMessage"] = "شما دسترسی به افزودن کامنت برای این مشکل ندارید.";
            return RedirectBack(returnUrl);
        }

        var firstName = GetCurrentUserFirstName();
        var lastName = GetCurrentUserLastName();

        // حذف محدودیت - همه کاربران می‌توانند کامنت بگذارند
        // فقط باید بتوانند مشکل را ببینند (که در Details action بررسی می‌شود)

        var comment = new IssueComment
        {
            IssueId = issueId,
            CommentText = SanitizeInput(commentText),
            CommenterFirstName = firstName,
            CommenterLastName = lastName,
            CommentedAt = DateTime.Now
        };

        _context.IssueComments.Add(comment);
        _context.SaveChanges();

        // پردازش فایل‌های ضمیمه کامنت
        var attachmentErrors = new List<string>();
        var uploadedFiles = Request.Form.Files.Where(f => f.Name == "commentAttachments" && f.Length > 0).ToList();
        if (uploadedFiles.Any())
        {
            var uploadsFolder = Path.Combine(_environment.WebRootPath, "uploads", "comments", comment.Id.ToString());
            if (!Directory.Exists(uploadsFolder))
            {
                Directory.CreateDirectory(uploadsFolder);
            }

            foreach (var file in uploadedFiles)
            {
                if (!IsAllowedCommentImageAttachment(file, out var contentType, out var errorMessage))
                {
                    attachmentErrors.Add(errorMessage);
                    continue;
                }

                // ایجاد نام یکتا برای فایل
                var fileExtension = Path.GetExtension(file.FileName);
                var storedFileName = $"{Guid.NewGuid()}{fileExtension}";
                var filePath = Path.Combine(uploadsFolder, storedFileName);

                // ذخیره فایل
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    file.CopyTo(stream);
                }

                // ذخیره اطلاعات فایل در دیتابیس
                var attachment = new CommentAttachment
                {
                    CommentId = comment.Id,
                    FileName = Path.GetFileName(file.FileName),
                    StoredFileName = storedFileName,
                    FilePath = filePath,
                    ContentType = contentType,
                    FileSize = file.Length,
                    UploadedAt = DateTime.Now,
                    UploadedByFirstName = firstName,
                    UploadedByLastName = lastName
                };

                _context.CommentAttachments.Add(attachment);
            }

            _context.SaveChanges();
        }

        _logService.LogAction(
            firstName,
            lastName,
            "AddComment",
            "SoftwareIssue",
            issueId.ToString(),
            $"کامنت جدید به مشکل #{issueId} اضافه شد"
        );

        var commenterName = $"{firstName} {lastName}";
        try
        {
            await _telegramNotification.NotifyCommentAddedAsync(issue, commenterName, comment.CommentText);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "ارسال نوتیفیکیشن تلگرام برای کامنت جدید با خطا مواجه شد.");
        }

        if (attachmentErrors.Any())
        {
            TempData["ErrorMessage"] = $"کامنت ثبت شد، اما بعضی ضمیمه‌ها بارگذاری نشدند: {string.Join(" ", attachmentErrors)}";
        }
        else
        {
            TempData["SuccessMessage"] = "کامنت با موفقیت اضافه شد.";
        }
        return RedirectBack(returnUrl ?? Url.Action("Details", new { id = issueId }));
    }

    // تغییر وضعیت (توسط مسئول)
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateStatus(int issueId, string status, string? returnUrl = null)
    {
        var issue = _context.SoftwareIssues
            .Include(i => i.AssignedToEntity)
            .Include(i => i.StatusEntity)
            .FirstOrDefault(i => i.Id == issueId);
        if (issue == null)
        {
            TempData["ErrorMessage"] = "مشکل یافت نشد.";
            return RedirectBack(returnUrl);
        }

        var firstName = GetCurrentUserFirstName();
        var lastName = GetCurrentUserLastName();

        // بررسی اینکه آیا کاربر مسئول این مشکل است
        if (issue.AssignedToEntity == null || issue.AssignedToEntity.FirstName != firstName || issue.AssignedToEntity.LastName != lastName)
        {
            TempData["ErrorMessage"] = "شما مسئول این مشکل نیستید.";
            return RedirectBack(returnUrl);
        }

        // پیدا کردن Status جدید
        var newStatus = _context.Statuses.FirstOrDefault(s => s.Value == status && s.IsActive);
        if (newStatus == null)
        {
            TempData["ErrorMessage"] = "وضعیت معتبر نیست.";
            return RedirectBack(returnUrl);
        }

        var oldStatus = issue.Status;
        issue.StatusId = newStatus.Id;

        // اگر وضعیت به "در حال انجام" تغییر کرد و زمان شروع وجود ندارد، آن را تنظیم کن
        if (status == "در حال انجام" && !issue.StartedAt.HasValue)
        {
            issue.StartedAt = DateTime.Now;
        }

        // اگر وضعیت به "تکمیل شده" تغییر کرد و زمان پایان وجود ندارد، آن را تنظیم کن
        if (status == "تکمیل شده" && !issue.CompletedAt.HasValue)
        {
            issue.CompletedAt = DateTime.Now;
        }

        _context.SaveChanges();

        _logService.LogAction(
            firstName,
            lastName,
            "UpdateStatus",
            "SoftwareIssue",
            issueId.ToString(),
            $"وضعیت مشکل #{issueId} از '{oldStatus}' به '{status}' تغییر یافت"
        );

        await NotifyIssueUpdatedSafelyAsync(
            issue,
            $"{firstName} {lastName}",
            new[] { $"تغییر وضعیت: از {oldStatus} به {status}" },
            "ارسال نوتیفیکیشن تلگرام برای تغییر وضعیت با خطا مواجه شد.");

        TempData["SuccessMessage"] = "وضعیت با موفقیت به‌روزرسانی شد.";
        return RedirectBack(returnUrl ?? Url.Action("Details", new { id = issueId }));
    }

    // مشاهده/دانلود فایل ضمیمه
    public IActionResult ViewAttachment(int id)
    {
        var attachment = _context.IssueAttachments.FirstOrDefault(a => a.Id == id);
        if (attachment == null || !System.IO.File.Exists(attachment.FilePath))
        {
            return NotFound();
        }

        // بررسی دسترسی - کاربر باید بتواند مشکل را ببیند
        var issue = _context.SoftwareIssues
            .Include(i => i.ReporterEntity)
            .Include(i => i.AssignedToEntity)
            .FirstOrDefault(i => i.Id == attachment.IssueId);
        if (issue == null)
        {
            return NotFound();
        }

        if (!CanViewCurrentIssue(issue))
        {
            TempData["ErrorMessage"] = "شما دسترسی به این فایل ندارید.";
            return RedirectToAction("Index");
        }

        return CreateAttachmentFileResult(attachment.FilePath, attachment.ContentType);
    }

    // مشاهده/دانلود فایل ضمیمه کامنت
    public IActionResult ViewCommentAttachment(int id)
    {
        var attachment = _context.CommentAttachments.FirstOrDefault(a => a.Id == id);
        if (attachment == null || !System.IO.File.Exists(attachment.FilePath))
        {
            return NotFound();
        }

        // بررسی دسترسی - کاربر باید بتواند کامنت و مشکل را ببیند
        var comment = _context.IssueComments.FirstOrDefault(c => c.Id == attachment.CommentId);
        if (comment == null)
        {
            return NotFound();
        }

        var issue = _context.SoftwareIssues
            .Include(i => i.ReporterEntity)
            .Include(i => i.AssignedToEntity)
            .FirstOrDefault(i => i.Id == comment.IssueId);
        if (issue == null)
        {
            return NotFound();
        }

        if (!CanViewCurrentIssue(issue))
        {
            TempData["ErrorMessage"] = "شما دسترسی به این فایل ندارید.";
            return RedirectToAction("Index");
        }

        return CreateAttachmentFileResult(attachment.FilePath, attachment.ContentType);
    }
}

// ViewModels
public class IssuesIndexViewModel
{
    public List<SoftwareIssue> Issues { get; set; } = new();
    public string? StatusFilter { get; set; }
    public string? SoftwareFilter { get; set; }
    public string? AssignedToFilter { get; set; }
    public string? CustomerNameFilter { get; set; }
    public string? DateFrom { get; set; }
    public string? DateTo { get; set; }
    public string? SearchTerm { get; set; }
    public string? PriorityFilter { get; set; }
    public string? SeverityFilter { get; set; }
    public string? EnvironmentFilter { get; set; }
    public string? GitStatusFilter { get; set; }
    public string? WorkItemTypeFilter { get; set; }
    public string? BranchFilter { get; set; }
    public string? Queue { get; set; }
    public bool UnassignedOnly { get; set; }
    public bool MyItemsOnly { get; set; }
    public bool OverdueOnly { get; set; }
    public int OpenCount { get; set; }
    public int UnassignedCount { get; set; }
    public int UrgentCount { get; set; }
    public int OverdueCount { get; set; }
    public int MyItemsCount { get; set; }
    public int ReportedByMeCount { get; set; }
    public bool IsAdmin { get; set; }
    public bool CanEditIssues { get; set; }
    public bool CanDeleteIssues { get; set; }
    public List<NameItem> ActiveUsers { get; set; } = new();
    public List<User> ActiveIssueUsers { get; set; } = new();
    public List<string> SoftwareNames { get; set; } = new();
    public List<string> StatusOptions { get; set; } = new();
    public Dictionary<int, DateTime> LastActivityAt { get; set; } = new();
}

public class IssueDetailsViewModel
{
    public SoftwareIssue Issue { get; set; } = new();
    public bool IsAdmin { get; set; }
    public bool IsAssigned { get; set; }
    public bool CanEditIssues { get; set; }
    public bool CanDeleteIssues { get; set; }
    public List<NameItem> ActiveUsers { get; set; } = new();
    public List<string> StatusOptions { get; set; } = new();
    public List<IssueComment> Comments { get; set; } = new();
    public List<IssueAttachment> Attachments { get; set; } = new();
    public Dictionary<int, List<CommentAttachment>> CommentAttachments { get; set; } = new();
}

public class CreateIssueViewModel
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string SoftwareName { get; set; } = string.Empty;
    public string IssueType { get; set; } = string.Empty;
    public string? CustomerName { get; set; } // نام مشتری (اختیاری)
    public string? BranchName { get; set; }
    public string? RepositoryUrl { get; set; }
    public string? CommitUrl { get; set; }
    public string? WorkItemType { get; set; }
    public string? GitStatus { get; set; }
    public string? Priority { get; set; } = "Medium";
    public string? Severity { get; set; } = "Major";
    public DateTime? DueDate { get; set; }
    public string? Environment { get; set; }
    public string? ReportedChannel { get; set; }
    public string? ReportedChannelUrl { get; set; }
    public string? AssignedToFirstName { get; set; }
    public string? AssignedToLastName { get; set; }
    public List<string> SoftwareNames { get; set; } = new();
    public List<string> IssueTypes { get; set; } = new();
    public List<NameItem> ActiveUsers { get; set; } = new();
}

public class EditIssueViewModel
{
    public SoftwareIssue Issue { get; set; } = new();
    public List<NameItem> ActiveUsers { get; set; } = new();
    public List<string> SoftwareNames { get; set; } = new();
    public List<string> IssueTypes { get; set; } = new();
    public List<string> StatusOptions { get; set; } = new();
    public bool CanEditFullIssue { get; set; }
    public bool CanEditGitDetails { get; set; }
    public bool StartWork { get; set; }
    public bool CompleteWork { get; set; }
}

public class MyAssignedIssuesViewModel
{
    public List<SoftwareIssue> Issues { get; set; } = new();
    public List<string> StatusOptions { get; set; } = new();
}
