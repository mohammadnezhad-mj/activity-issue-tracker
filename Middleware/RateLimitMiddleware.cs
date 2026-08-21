using System.Collections.Concurrent;
using RayanTask.Services;

namespace RayanTask.Middleware;

public class RateLimitMiddleware
{
    private readonly RequestDelegate _next;
    private static readonly ConcurrentDictionary<string, List<DateTime>> _requestHistory = new();
    private readonly IServiceProvider _serviceProvider;

    public RateLimitMiddleware(RequestDelegate next, IServiceProvider serviceProvider)
    {
        _next = next;
        _serviceProvider = serviceProvider;
    }

    // متد برای دریافت تنظیمات Rate Limit از config.json
    private (int maxRequests, TimeSpan timeWindow) GetRateLimitSettings()
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var dataService = scope.ServiceProvider.GetRequiredService<DataService>();
            var config = dataService.LoadConfiguration();

            if (config.RateLimitSettings != null)
            {
                var maxRequests = config.RateLimitSettings.MaxRequests > 0 
                    ? config.RateLimitSettings.MaxRequests 
                    : 5; // مقدار پیش‌فرض
                
                var timeWindowMinutes = config.RateLimitSettings.TimeWindowMinutes > 0 
                    ? config.RateLimitSettings.TimeWindowMinutes 
                    : 15; // مقدار پیش‌فرض

                return (maxRequests, TimeSpan.FromMinutes(timeWindowMinutes));
            }
        }
        catch
        {
            // در صورت خطا، از مقادیر پیش‌فرض استفاده کن
        }

        // مقادیر پیش‌فرض
        return (5, TimeSpan.FromMinutes(15));
    }

    // متد استاتیک برای reset کردن rate limit یک IP خاص
    public static void ResetRateLimit(string ipAddress)
    {
        var key = $"login_{ipAddress}";
        _requestHistory.TryRemove(key, out _);
    }

    // متد استاتیک برای reset کردن تمام rate limits
    public static void ResetAllRateLimits()
    {
        _requestHistory.Clear();
    }

    // متد استاتیک برای دریافت لیست IP های محدود شده
    public static List<string> GetBlockedIps(IServiceProvider? serviceProvider = null)
    {
        var now = DateTime.UtcNow;
        var blockedIps = new List<string>();
        
        // دریافت تنظیمات از config.json
        int maxRequests = 5;
        TimeSpan timeWindow = TimeSpan.FromMinutes(15);
        
        if (serviceProvider != null)
        {
            try
            {
                using var scope = serviceProvider.CreateScope();
                var dataService = scope.ServiceProvider.GetRequiredService<DataService>();
                var config = dataService.LoadConfiguration();
                
                if (config.RateLimitSettings != null)
                {
                    maxRequests = config.RateLimitSettings.MaxRequests > 0 
                        ? config.RateLimitSettings.MaxRequests 
                        : 5;
                    timeWindow = TimeSpan.FromMinutes(
                        config.RateLimitSettings.TimeWindowMinutes > 0 
                            ? config.RateLimitSettings.TimeWindowMinutes 
                            : 15
                    );
                }
            }
            catch
            {
                // در صورت خطا، از مقادیر پیش‌فرض استفاده کن
            }
        }
        
        foreach (var kvp in _requestHistory)
        {
            if (kvp.Key.StartsWith("login_"))
            {
                var requests = kvp.Value;
                lock (requests)
                {
                    requests.RemoveAll(r => r < now - timeWindow);
                    if (requests.Count >= maxRequests)
                    {
                        var ip = kvp.Key.Replace("login_", "");
                        blockedIps.Add(ip);
                    }
                }
            }
        }
        
        return blockedIps;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // فقط برای صفحه Login اعمال می‌شود
        if (context.Request.Path.StartsWithSegments("/Account/Login") && 
            context.Request.Method == "POST")
        {
            // دریافت تنظیمات از config.json
            var (maxRequests, timeWindow) = GetRateLimitSettings();

            var clientIp = GetClientIpAddress(context);
            var key = $"login_{clientIp}";

            var now = DateTime.UtcNow;
            var requests = _requestHistory.GetOrAdd(key, _ => new List<DateTime>());

            // حذف درخواست‌های قدیمی و بررسی محدودیت
            bool shouldBlock = false;
            lock (requests)
            {
                requests.RemoveAll(r => r < now - timeWindow);

                if (requests.Count >= maxRequests)
                {
                    shouldBlock = true;
                }
                else
                {
                    requests.Add(now);
                }
            }

            if (shouldBlock)
            {
                context.Response.StatusCode = 429; // Too Many Requests
                context.Response.ContentType = "text/html; charset=utf-8";
                var timeWindowMinutes = (int)timeWindow.TotalMinutes;
                await context.Response.WriteAsync($@"
                    <html dir='rtl' lang='fa'>
                    <head><meta charset='utf-8'><title>درخواست بیش از حد</title></head>
                    <body style='font-family: Tahoma; text-align: center; padding: 50px;'>
                        <h2>درخواست بیش از حد</h2>
                        <p>شما تعداد زیادی درخواست ورود ارسال کرده‌اید. لطفاً {timeWindowMinutes} دقیقه صبر کنید و دوباره تلاش کنید.</p>
                        <a href='/Account/Login'>بازگشت به صفحه ورود</a>
                    </body>
                    </html>");
                return;
            }
        }

        await _next(context);
    }

    private string GetClientIpAddress(HttpContext context)
    {
        // بررسی X-Forwarded-For برای پروکسی/load balancer
        var forwardedFor = context.Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrEmpty(forwardedFor))
        {
            return forwardedFor.Split(',')[0].Trim();
        }

        // بررسی X-Real-IP
        var realIp = context.Request.Headers["X-Real-IP"].FirstOrDefault();
        if (!string.IsNullOrEmpty(realIp))
        {
            return realIp;
        }

        return context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
    }
}

