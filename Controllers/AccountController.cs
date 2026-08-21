using System.Security.Cryptography;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RayanTask.Data;
using RayanTask.Helpers;
using RayanTask.Models;
using RayanTask.Services;

namespace RayanTask.Controllers;

public class AccountController : BaseController
{
    private readonly DataService _dataService;
    private readonly LogService _logService;
    private readonly ILogger<AccountController> _logger;
    private readonly ApplicationDbContext _context;

    public AccountController(DataService dataService, LogService logService, ILogger<AccountController> logger, ApplicationDbContext context)
    {
        _dataService = dataService;
        _logService = logService;
        _logger = logger;
        _context = context;
    }

    [HttpGet]
    public IActionResult Login()
    {
        // اگر قبلاً لاگین کرده، به داشبورد برو
        if (IsLoggedIn())
        {
            return RedirectToAction("Dashboard", "Home");
        }

        // خواندن کاربران از دیتابیس به جای JSON
        var activeUsers = _context.Users
            .Where(u => u.IsActive)
            .Select(u => new NameItem 
            { 
                FirstName = u.FirstName, 
                LastName = u.LastName, 
                IsActive = u.IsActive 
            })
            .ToList();
        
        var config = new ConfigurationData
        {
            Names = activeUsers
        };
        
        return View(config);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Login(string fullName, string password)
    {
        var config = _dataService.LoadConfiguration();
        
        // جدا کردن نام و نام خانوادگی
        if (string.IsNullOrWhiteSpace(fullName))
        {
            ViewBag.ErrorMessage = "لطفاً نام و نام خانوادگی را انتخاب کنید.";
            return View(config);
        }

        var nameParts = fullName.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (nameParts.Length < 2)
        {
            ViewBag.ErrorMessage = "لطفاً نام و نام خانوادگی را به درستی انتخاب کنید.";
            return View(config);
        }

        var firstName = nameParts[0];
        var lastName = string.Join(" ", nameParts.Skip(1));
        
        // بررسی وجود کاربر در دیتابیس (فقط کاربران فعال)
        var user = _context.Users.FirstOrDefault(u => 
            u.FirstName == firstName && u.LastName == lastName && u.IsActive);

        if (user == null)
        {
            // Log failed login attempt
            LogFailedLoginAttempt($"{firstName} {lastName}", _logger);
            ViewBag.ErrorMessage = "نام و نام خانوادگی یا رمز عبور صحیح نیست.";
            return View(config);
        }

        // بررسی رمز عبور کاربر
        if (string.IsNullOrWhiteSpace(password))
        {
            LogFailedLoginAttempt($"{firstName} {lastName}", _logger);
            ViewBag.ErrorMessage = "لطفاً رمز عبور را وارد کنید.";
            return View(config);
        }

        // بررسی طول رمز عبور (حداکثر 100 کاراکتر)
        if (password.Length > 100)
        {
            LogFailedLoginAttempt($"{firstName} {lastName}", _logger);
            ViewBag.ErrorMessage = "رمز عبور معتبر نیست.";
            return View(config);
        }

        // بررسی رمز عبور از دیتابیس
        bool passwordValid = PasswordHelper.VerifyPassword(password, user.Password, firstName, lastName);
        
        if (!passwordValid)
        {
            // Log failed login attempt
            LogFailedLoginAttempt($"{firstName} {lastName}", _logger);
            ViewBag.ErrorMessage = "نام و نام خانوادگی یا رمز عبور صحیح نیست.";
            return View(config);
        }

        // اگر password به صورت plain text بود یا hash قدیمی (بدون salt)، آن را hash کن با salt و ذخیره کن
        if (!PasswordHelper.IsHashed(user.Password))
        {
            // Plain text - hash کن با salt
            user.Password = PasswordHelper.HashPassword(user.Password, firstName, lastName);
            user.UpdatedAt = DateTime.Now;
            _context.SaveChanges();
        }
        else
        {
            // بررسی اینکه آیا hash قدیمی است (بدون salt) یا جدید (با salt)
            // اگر hash قدیمی است، آن را به hash جدید با salt تبدیل کن
            var testHashLegacy = PasswordHelper.HashPasswordLegacy(password);
            if (testHashLegacy.Equals(user.Password, StringComparison.OrdinalIgnoreCase))
            {
                // Hash قدیمی است - به hash جدید با salt تبدیل کن
                user.Password = PasswordHelper.HashPassword(password, firstName, lastName);
                user.UpdatedAt = DateTime.Now;
                _context.SaveChanges();
            }
        }

        // ذخیره در Session
        HttpContext.Session.SetString("UserId", user.Id.ToString());
        HttpContext.Session.SetString("FirstName", user.FirstName);
        HttpContext.Session.SetString("LastName", user.LastName);
        HttpContext.Session.SetString("IsAdmin", user.IsAdmin ? "true" : "false");
        HttpContext.Session.SetString("CanEdit", user.CanEdit ? "true" : "false");
        HttpContext.Session.SetString("CanDelete", user.CanDelete ? "true" : "false");
        HttpContext.Session.SetString("CanSelectAnyDate", user.CanSelectAnyDate ? "true" : "false");
        // دسترسی‌های بخشی
        HttpContext.Session.SetString("CanManageUsers", user.CanManageUsers ? "true" : "false");
        HttpContext.Session.SetString("CanManageActivityTypes", user.CanManageActivityTypes ? "true" : "false");
        HttpContext.Session.SetString("CanManageResults", user.CanManageResults ? "true" : "false");
        HttpContext.Session.SetString("CanViewReports", user.CanViewReports ? "true" : "false");
        HttpContext.Session.SetString("CanViewLogs", user.CanViewLogs ? "true" : "false");
        HttpContext.Session.SetString("CanViewActivityList", user.CanViewActivityList ? "true" : "false");
        HttpContext.Session.SetString("CanViewIssueDetails", user.CanViewIssueDetails ? "true" : "false");
        HttpContext.Session.SetString("CanEditIssues", user.CanEditIssues ? "true" : "false");
        HttpContext.Session.SetString("CanDeleteIssues", user.CanDeleteIssues ? "true" : "false");
        HttpContext.Session.SetString("CanDeleteReports", user.CanDeleteReports ? "true" : "false");

        return RedirectToAction("Dashboard", "Home");
    }

    [HttpGet]
    public IActionResult ForgotPassword()
    {
        if (IsLoggedIn())
        {
            return RedirectToAction("Dashboard", "Home");
        }

        var activeUsers = _context.Users
            .Where(u => u.IsActive)
            .Select(u => new NameItem
            {
                FirstName = u.FirstName,
                LastName = u.LastName,
                IsActive = u.IsActive
            })
            .ToList();

        var config = new ConfigurationData
        {
            Names = activeUsers
        };

        return View(config);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ForgotPassword(string fullName)
    {
        if (IsLoggedIn())
        {
            return RedirectToAction("Dashboard", "Home");
        }

        if (string.IsNullOrWhiteSpace(fullName))
        {
            ViewBag.ErrorMessage = "لطفاً نام و نام خانوادگی را انتخاب کنید.";
            return View(GetConfigForForgotPassword());
        }

        var nameParts = fullName.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (nameParts.Length < 2)
        {
            ViewBag.ErrorMessage = "لطفاً نام و نام خانوادگی را به درستی انتخاب کنید.";
            return View(GetConfigForForgotPassword());
        }

        var firstName = nameParts[0];
        var lastName = string.Join(" ", nameParts.Skip(1));

        var user = _context.Users.FirstOrDefault(u =>
            u.FirstName == firstName && u.LastName == lastName && u.IsActive);

        // برای امنیت، همیشه پیام یکسان نشان می‌دهیم (نکشیدن وجود یا عدم وجود کاربر)
        if (user == null)
        {
            TempData["ForgotPasswordMessage"] = "در صورت وجود این کاربر در سیستم، لینک بازیابی رمز عبور برای شما نمایش داده می‌شود.";
            return RedirectToAction("ForgotPasswordConfirmation");
        }

        // اگر کاربر سوال امنیتی تنظیم نکرده، بازیابی بدون سوال ممکن نیست
        if (string.IsNullOrWhiteSpace(user.SecurityQuestion) || string.IsNullOrWhiteSpace(user.SecurityAnswerHash))
        {
            TempData["ForgotPasswordMessage"] = "برای این حساب، سوال امنیتی تنظیم نشده است. لطفاً پس از ورود از بخش «سوال امنیتی» در تنظیمات حساب، سوال و جواب را تنظیم کنید. در حال حاضر امکان بازیابی رمز از این طریق وجود ندارد.";
            return RedirectToAction("ForgotPasswordConfirmation");
        }

        // حذف درخواست‌های قبلی این کاربر
        await _context.PasswordResetRequests
            .Where(r => r.UserId == user.Id)
            .ExecuteDeleteAsync();

        var requestToken = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
        var resetRequest = new PasswordResetRequest
        {
            UserId = user.Id,
            RequestToken = requestToken,
            ExpiresAt = DateTime.UtcNow.AddMinutes(10),
            CreatedAt = DateTime.UtcNow
        };
        _context.PasswordResetRequests.Add(resetRequest);
        await _context.SaveChangesAsync();

        return RedirectToAction("ForgotPasswordAnswer", new { requestToken });
    }

    [HttpGet]
    public IActionResult ForgotPasswordAnswer(string? requestToken)
    {
        if (IsLoggedIn())
        {
            return RedirectToAction("Dashboard", "Home");
        }

        if (string.IsNullOrWhiteSpace(requestToken))
        {
            TempData["ErrorMessage"] = "لینک نامعتبر است.";
            return RedirectToAction("Login");
        }

        var request = _context.PasswordResetRequests
            .Include(r => r.User)
            .FirstOrDefault(r => r.RequestToken == requestToken && r.ExpiresAt > DateTime.UtcNow);

        if (request == null)
        {
            TempData["ErrorMessage"] = "لینک منقضی یا نامعتبر است. لطفاً دوباره از ابتدا درخواست بازیابی رمز عبور دهید.";
            return RedirectToAction("Login");
        }

        ViewBag.RequestToken = requestToken;
        ViewBag.SecurityQuestion = request.User.SecurityQuestion ?? "";
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ForgotPasswordAnswer(string? requestToken, string? securityAnswer)
    {
        if (IsLoggedIn())
        {
            return RedirectToAction("Dashboard", "Home");
        }

        if (string.IsNullOrWhiteSpace(requestToken))
        {
            TempData["ErrorMessage"] = "لینک نامعتبر است.";
            return RedirectToAction("Login");
        }

        var request = _context.PasswordResetRequests
            .Include(r => r.User)
            .FirstOrDefault(r => r.RequestToken == requestToken && r.ExpiresAt > DateTime.UtcNow);

        if (request == null)
        {
            TempData["ErrorMessage"] = "لینک منقضی یا نامعتبر است. لطفاً دوباره از ابتدا درخواست بازیابی رمز عبور دهید.";
            return RedirectToAction("Login");
        }

        if (string.IsNullOrWhiteSpace(securityAnswer))
        {
            ViewBag.ErrorMessage = "لطفاً جواب سوال امنیتی را وارد کنید.";
            ViewBag.RequestToken = requestToken;
            ViewBag.SecurityQuestion = request.User.SecurityQuestion ?? "";
            return View();
        }

        var user = request.User;
        if (!PasswordHelper.VerifySecurityAnswer(securityAnswer, user.SecurityAnswerHash, user.FirstName, user.LastName))
        {
            ViewBag.ErrorMessage = "جواب سوال امنیتی صحیح نیست.";
            ViewBag.RequestToken = requestToken;
            ViewBag.SecurityQuestion = request.User.SecurityQuestion ?? "";
            return View();
        }

        // حذف درخواست و توکن‌های قبلی این کاربر
        _context.PasswordResetRequests.Remove(request);
        await _context.PasswordResetTokens
            .Where(t => t.UserId == user.Id)
            .ExecuteDeleteAsync();

        var token = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
        var resetToken = new PasswordResetToken
        {
            UserId = user.Id,
            Token = token,
            ExpiresAt = DateTime.UtcNow.AddHours(1),
            CreatedAt = DateTime.UtcNow
        };
        _context.PasswordResetTokens.Add(resetToken);
        await _context.SaveChangesAsync();

        TempData["ResetToken"] = token;
        TempData["ForgotPasswordMessage"] = "لینک بازیابی رمز عبور شما در زیر نمایش داده شده است. این لینک تا ۱ ساعت معتبر است.";
        return RedirectToAction("ForgotPasswordConfirmation");
    }

    [HttpGet]
    public IActionResult ForgotPasswordConfirmation()
    {
        if (IsLoggedIn())
        {
            return RedirectToAction("Dashboard", "Home");
        }

        var token = TempData["ResetToken"] as string;
        ViewBag.ResetToken = token;
        ViewBag.Message = TempData["ForgotPasswordMessage"] ?? "در صورت درخواست بازیابی، لینک برای شما نمایش داده شده است.";
        return View();
    }

    [HttpGet]
    public IActionResult ResetPassword(string? token)
    {
        if (IsLoggedIn())
        {
            return RedirectToAction("Dashboard", "Home");
        }

        if (string.IsNullOrWhiteSpace(token))
        {
            TempData["ErrorMessage"] = "لینک بازیابی نامعتبر است.";
            return RedirectToAction("Login");
        }

        var resetToken = _context.PasswordResetTokens
            .Include(t => t.User)
            .FirstOrDefault(t => t.Token == token && t.ExpiresAt > DateTime.UtcNow);

        if (resetToken == null)
        {
            TempData["ErrorMessage"] = "لینک بازیابی منقضی یا نامعتبر است. لطفاً دوباره درخواست بازیابی رمز عبور دهید.";
            return RedirectToAction("Login");
        }

        ViewBag.Token = token;
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ResetPassword(string? token, string newPassword, string confirmPassword)
    {
        if (IsLoggedIn())
        {
            return RedirectToAction("Dashboard", "Home");
        }

        if (string.IsNullOrWhiteSpace(token))
        {
            TempData["ErrorMessage"] = "لینک بازیابی نامعتبر است.";
            return RedirectToAction("Login");
        }

        if (string.IsNullOrWhiteSpace(newPassword))
        {
            ViewBag.ErrorMessage = "لطفاً رمز عبور جدید را وارد کنید.";
            ViewBag.Token = token;
            return View();
        }

        if (newPassword.Length > 100)
        {
            ViewBag.ErrorMessage = "رمز عبور جدید نمی‌تواند بیشتر از ۱۰۰ کاراکتر باشد.";
            ViewBag.Token = token;
            return View();
        }

        if (newPassword != confirmPassword)
        {
            ViewBag.ErrorMessage = "رمز عبور جدید و تکرار آن یکسان نیستند.";
            ViewBag.Token = token;
            return View();
        }

        var resetToken = _context.PasswordResetTokens
            .Include(t => t.User)
            .FirstOrDefault(t => t.Token == token && t.ExpiresAt > DateTime.UtcNow);

        if (resetToken == null)
        {
            TempData["ErrorMessage"] = "لینک بازیابی منقضی یا نامعتبر است. لطفاً دوباره درخواست بازیابی رمز عبور دهید.";
            return RedirectToAction("Login");
        }

        var user = resetToken.User;
        user.Password = PasswordHelper.HashPassword(newPassword, user.FirstName, user.LastName);
        user.UpdatedAt = DateTime.Now;

        _context.PasswordResetTokens.Remove(resetToken);
        await _context.SaveChangesAsync();

        if (_logService != null)
        {
            _logService.LogAction(
                user.FirstName,
                user.LastName,
                "PasswordReset",
                "User",
                $"{user.FirstName} {user.LastName}",
                $"کاربر '{user.FirstName} {user.LastName}' رمز عبور خود را از طریق بازیابی تنظیم کرد."
            );
        }

        TempData["SuccessMessage"] = "رمز عبور با موفقیت تغییر یافت. اکنون می‌توانید با رمز جدید وارد شوید.";
        return RedirectToAction("Login");
    }

    private ConfigurationData GetConfigForForgotPassword()
    {
        var activeUsers = _context.Users
            .Where(u => u.IsActive)
            .Select(u => new NameItem
            {
                FirstName = u.FirstName,
                LastName = u.LastName,
                IsActive = u.IsActive
            })
            .ToList();
        return new ConfigurationData { Names = activeUsers };
    }

    [HttpGet]
    public IActionResult SecurityQuestion()
    {
        if (!IsLoggedIn())
        {
            return RedirectToAction("Login");
        }

        var firstName = GetCurrentUserFirstName();
        var lastName = GetCurrentUserLastName();
        var user = _context.Users.FirstOrDefault(u => u.FirstName == firstName && u.LastName == lastName);
        if (user == null)
        {
            return RedirectToAction("Login");
        }

        ViewBag.SecurityQuestion = user.SecurityQuestion ?? "";
        ViewBag.HasQuestion = !string.IsNullOrWhiteSpace(user.SecurityQuestion);
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult SecurityQuestion(string? securityQuestion, string? securityAnswer, string? confirmAnswer)
    {
        if (!IsLoggedIn())
        {
            return RedirectToAction("Login");
        }

        var firstName = GetCurrentUserFirstName();
        var lastName = GetCurrentUserLastName();
        var user = _context.Users.FirstOrDefault(u => u.FirstName == firstName && u.LastName == lastName);
        if (user == null)
        {
            return RedirectToAction("Login");
        }

        if (string.IsNullOrWhiteSpace(securityQuestion) || securityQuestion.Trim().Length < 3)
        {
            ViewBag.ErrorMessage = "لطفاً یک سوال امنیتی معتبر (حداقل ۳ کاراکتر) وارد کنید.";
            ViewBag.SecurityQuestion = securityQuestion ?? "";
            ViewBag.HasQuestion = !string.IsNullOrWhiteSpace(user.SecurityQuestion);
            return View();
        }

        if (securityQuestion.Trim().Length > 500)
        {
            ViewBag.ErrorMessage = "سوال امنیتی نمی‌تواند بیشتر از ۵۰۰ کاراکتر باشد.";
            ViewBag.SecurityQuestion = securityQuestion.Trim().Substring(0, 500);
            ViewBag.HasQuestion = !string.IsNullOrWhiteSpace(user.SecurityQuestion);
            return View();
        }

        if (string.IsNullOrWhiteSpace(securityAnswer) || securityAnswer.Trim().Length < 2)
        {
            ViewBag.ErrorMessage = "لطفاً جواب سوال امنیتی را وارد کنید (حداقل ۲ کاراکتر).";
            ViewBag.SecurityQuestion = securityQuestion.Trim();
            ViewBag.HasQuestion = !string.IsNullOrWhiteSpace(user.SecurityQuestion);
            return View();
        }

        if (securityAnswer != confirmAnswer)
        {
            ViewBag.ErrorMessage = "جواب سوال امنیتی و تکرار آن یکسان نیستند.";
            ViewBag.SecurityQuestion = securityQuestion.Trim();
            ViewBag.HasQuestion = !string.IsNullOrWhiteSpace(user.SecurityQuestion);
            return View();
        }

        user.SecurityQuestion = securityQuestion.Trim();
        user.SecurityAnswerHash = PasswordHelper.HashSecurityAnswer(securityAnswer.Trim(), firstName, lastName);
        user.UpdatedAt = DateTime.Now;
        _context.SaveChanges();

        TempData["SuccessMessage"] = "سوال امنیتی با موفقیت ذخیره شد. در بازیابی رمز عبور از شما این سوال پرسیده می‌شود.";
        return RedirectToAction("SecurityQuestion");
    }

    [HttpGet]
    public IActionResult ChangePassword()
    {
        if (!IsLoggedIn())
        {
            return RedirectToAction("Login");
        }
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult ChangePassword(string currentPassword, string newPassword, string confirmPassword)
    {
        if (!IsLoggedIn())
        {
            return RedirectToAction("Login");
        }

        var firstName = GetCurrentUserFirstName();
        var lastName = GetCurrentUserLastName();

        // بررسی ورودی‌ها
        if (string.IsNullOrWhiteSpace(currentPassword))
        {
            ViewBag.ErrorMessage = "لطفاً رمز عبور فعلی را وارد کنید.";
            return View();
        }

        if (string.IsNullOrWhiteSpace(newPassword))
        {
            ViewBag.ErrorMessage = "لطفاً رمز عبور جدید را وارد کنید.";
            return View();
        }

        if (newPassword.Length > 100)
        {
            ViewBag.ErrorMessage = "رمز عبور جدید نمی‌تواند بیشتر از 100 کاراکتر باشد.";
            return View();
        }

        if (newPassword != confirmPassword)
        {
            ViewBag.ErrorMessage = "رمز عبور جدید و تکرار آن یکسان نیستند.";
            return View();
        }

        // بارگذاری کاربر از دیتابیس
        var user = _context.Users.FirstOrDefault(u => u.FirstName == firstName && u.LastName == lastName);

        if (user == null)
        {
            ViewBag.ErrorMessage = "کاربر یافت نشد.";
            return View();
        }

        // بررسی رمز عبور فعلی
        if (!PasswordHelper.VerifyPassword(currentPassword, user.Password, firstName, lastName))
        {
            ViewBag.ErrorMessage = "رمز عبور فعلی صحیح نیست.";
            return View();
        }

        // بررسی اینکه رمز جدید با رمز فعلی یکسان نباشد
        if (PasswordHelper.VerifyPassword(newPassword, user.Password, firstName, lastName))
        {
            ViewBag.ErrorMessage = "رمز عبور جدید باید با رمز عبور فعلی متفاوت باشد.";
            return View();
        }

        // تغییر رمز عبور
        user.Password = PasswordHelper.HashPassword(newPassword, firstName, lastName);
        user.UpdatedAt = DateTime.Now;
        _context.SaveChanges();

        // ثبت لاگ
        if (_logService != null)
        {
            _logService.LogAction(
                firstName,
                lastName,
                "ChangePassword",
                "User",
                $"{firstName} {lastName}",
                $"کاربر '{firstName} {lastName}' رمز عبور خود را تغییر داد."
            );
        }

        TempData["SuccessMessage"] = "رمز عبور با موفقیت تغییر یافت.";
        return RedirectToAction("Dashboard", "Home");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Logout()
    {
        var userName = $"{GetCurrentUserFirstName()} {GetCurrentUserLastName()}";
        HttpContext.Session.Clear();
        _logger.LogInformation("User logged out: {UserName}, IP: {IP}", userName, GetClientIpAddress());
        return RedirectToAction("Login");
    }
}
