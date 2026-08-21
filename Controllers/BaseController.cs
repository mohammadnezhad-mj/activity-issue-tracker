using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace RayanTask.Controllers;

public abstract class BaseController : Controller
{
    protected bool IsLoggedIn()
    {
        return !string.IsNullOrEmpty(HttpContext.Session.GetString("FirstName"));
    }

    protected string GetCurrentUserFirstName()
    {
        return HttpContext.Session.GetString("FirstName") ?? "";
    }

    protected string GetCurrentUserLastName()
    {
        return HttpContext.Session.GetString("LastName") ?? "";
    }

    protected bool IsAdmin()
    {
        return HttpContext.Session.GetString("IsAdmin") == "true";
    }

    protected int? GetCurrentUserId()
    {
        var userIdStr = HttpContext.Session.GetString("UserId");
        if (int.TryParse(userIdStr, out int userId))
        {
            return userId;
        }
        return null;
    }

    // Sanitize input برای جلوگیری از XSS
    protected string SanitizeInput(string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return string.Empty;

        // حذف تگ‌های HTML خطرناک
        return System.Net.WebUtility.HtmlEncode(input);
    }

    // Validate و Sanitize string input
    protected bool ValidateStringInput(string? input, int maxLength, out string sanitized)
    {
        sanitized = string.Empty;
        
        if (string.IsNullOrWhiteSpace(input))
            return false;

        if (input.Length > maxLength)
            return false;

        sanitized = SanitizeInput(input);
        return true;
    }

    // Log failed login attempts
    protected void LogFailedLoginAttempt(string identifier, ILogger logger)
    {
        var clientIp = GetClientIpAddress();
        logger.LogWarning("Failed login attempt - Identifier: {Identifier}, IP: {IP}, Time: {Time}",
            identifier, clientIp, DateTime.UtcNow);
    }

    protected string GetClientIpAddress()
    {
        var forwardedFor = Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrEmpty(forwardedFor))
        {
            return forwardedFor.Split(',')[0].Trim();
        }

        var realIp = Request.Headers["X-Real-IP"].FirstOrDefault();
        if (!string.IsNullOrEmpty(realIp))
        {
            return realIp;
        }

        return HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
    }
}

