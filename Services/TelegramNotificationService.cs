using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using RayanTask.Models;

namespace RayanTask.Services;

/// <summary>
/// ارسال نوتیفیکیشن رویدادهای Issues به تلگرام.
/// </summary>
public class TelegramNotificationService : ITelegramNotificationService
{
    private const int TelegramMessageLimit = 3900;

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;
    private readonly ILogger<TelegramNotificationService> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    public TelegramNotificationService(
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration,
        ILogger<TelegramNotificationService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
        _logger = logger;
    }

    private string? BotToken => _configuration["Telegram:BotToken"]?.Trim();
    private string? ChatId => _configuration["Telegram:ChatId"]?.Trim();
    private string? AppBaseUrl => _configuration["Telegram:AppBaseUrl"]?.Trim();

    /// <summary>
    /// آدرس پایه API تلگرام. در صورت نیاز می‌تواند Cloudflare Worker یا reverse proxy باشد.
    /// </summary>
    private string TelegramApiBase => _configuration["Telegram:ApiBaseUrl"]?.Trim() is { Length: > 0 } baseUrl
        ? baseUrl.TrimEnd('/')
        : "https://api.telegram.org";

    private bool IsConfigured => !string.IsNullOrWhiteSpace(BotToken) && !string.IsNullOrWhiteSpace(ChatId);

    private string DetailsUrl(int issueId)
    {
        var path = $"/Issues/Details/{issueId}";
        if (!string.IsNullOrWhiteSpace(AppBaseUrl))
        {
            return $"{AppBaseUrl.TrimEnd('/')}{path}";
        }

        return path;
    }

    public async Task NotifyIssueCreatedAsync(SoftwareIssue issue, CancellationToken cancellationToken = default)
    {
        if (!IsConfigured)
            return;

        var text = BuildIssueMessage("درخواست جدید ثبت شد", issue);
        await SendAsync(text, cancellationToken).ConfigureAwait(false);
    }

    public async Task NotifyIssueUpdatedAsync(SoftwareIssue issue, string changedBy, IEnumerable<string> changes, CancellationToken cancellationToken = default)
    {
        if (!IsConfigured)
            return;

        var changesList = changes.Where(c => !string.IsNullOrWhiteSpace(c)).ToList();
        if (changesList.Count == 0)
            return;

        var builder = new StringBuilder();
        builder.AppendLine("درخواست به‌روزرسانی شد");
        builder.AppendLine();
        AppendIssueHeader(builder, issue);
        AppendLine(builder, "توسط", changedBy);
        builder.AppendLine();
        builder.AppendLine("تغییرات:");
        foreach (var change in changesList.Take(12))
        {
            builder.AppendLine($"- {change}");
        }

        if (changesList.Count > 12)
        {
            builder.AppendLine($"- و {changesList.Count - 12} تغییر دیگر");
        }

        builder.AppendLine();
        AppendIssueOperationalSummary(builder, issue);
        AppendIssueLinks(builder, issue);

        await SendAsync(builder.ToString(), cancellationToken).ConfigureAwait(false);
    }

    public async Task NotifyCommentAddedAsync(SoftwareIssue issue, string commenterName, string commentText, CancellationToken cancellationToken = default)
    {
        if (!IsConfigured)
            return;

        var builder = new StringBuilder();
        builder.AppendLine("کامنت جدید ثبت شد");
        builder.AppendLine();
        AppendIssueHeader(builder, issue);
        AppendLine(builder, "کاربر", commenterName);
        AppendLine(builder, "وضعیت", ValueOrDash(issue.Status));
        AppendLine(builder, "مسئول", UserDisplayName(issue.AssignedToEntity));
        builder.AppendLine();
        builder.AppendLine("متن کامنت:");
        builder.AppendLine(Truncate(commentText.Trim(), 700));
        builder.AppendLine();
        AppendIssueLinks(builder, issue);

        await SendAsync(builder.ToString(), cancellationToken).ConfigureAwait(false);
    }

    public async Task NotifyIssueDeletedAsync(SoftwareIssue issue, string deletedBy, CancellationToken cancellationToken = default)
    {
        if (!IsConfigured)
            return;

        var builder = new StringBuilder();
        builder.AppendLine("درخواست حذف شد");
        builder.AppendLine();
        AppendIssueHeader(builder, issue);
        AppendLine(builder, "حذف‌کننده", deletedBy);
        AppendLine(builder, "وضعیت آخر", ValueOrDash(issue.Status));
        AppendLine(builder, "مسئول آخر", UserDisplayName(issue.AssignedToEntity));
        builder.AppendLine();
        AppendIssueSourceSummary(builder, issue);
        AppendIssueGitSummary(builder, issue);

        await SendAsync(builder.ToString(), cancellationToken).ConfigureAwait(false);
    }

    private string BuildIssueMessage(string title, SoftwareIssue issue)
    {
        var builder = new StringBuilder();
        builder.AppendLine(title);
        builder.AppendLine();
        AppendIssueHeader(builder, issue);
        AppendIssueOperationalSummary(builder, issue);
        AppendIssueSourceSummary(builder, issue);
        AppendIssueGitSummary(builder, issue);
        AppendIssueLinks(builder, issue);
        return builder.ToString();
    }

    private static void AppendIssueHeader(StringBuilder builder, SoftwareIssue issue)
    {
        builder.AppendLine($"#{issue.Id} - {ValueOrDash(issue.Title)}");
        AppendLine(builder, "شرح کوتاه", Truncate(issue.Description, 300));
    }

    private static void AppendIssueOperationalSummary(StringBuilder builder, SoftwareIssue issue)
    {
        builder.AppendLine("اطلاعات پیگیری:");
        AppendLine(builder, "وضعیت", ValueOrDash(issue.Status));
        AppendLine(builder, "مسئول", UserDisplayName(issue.AssignedToEntity));
        AppendLine(builder, "ثبت‌کننده", UserDisplayName(issue.ReporterEntity));
        AppendLine(builder, "اولویت", DisplayPriority(issue.Priority));
        AppendLine(builder, "شدت", DisplaySeverity(issue.Severity));
        AppendLine(builder, "موعد", issue.DueDate?.ToString("yyyy/MM/dd") ?? "-");
        AppendLine(builder, "محیط", ValueOrDash(issue.Environment));
        builder.AppendLine();
    }

    private static void AppendIssueSourceSummary(StringBuilder builder, SoftwareIssue issue)
    {
        builder.AppendLine("اطلاعات درخواست:");
        AppendLine(builder, "نرم‌افزار", ValueOrDash(issue.SoftwareName));
        AppendLine(builder, "نوع", ValueOrDash(issue.IssueType));
        AppendLine(builder, "مشتری", ValueOrDash(issue.CustomerName));
        AppendLine(builder, "کانال گزارش", DisplayChannel(issue.ReportedChannel));
        AppendLine(builder, "لینک کانال", ValueOrDash(issue.ReportedChannelUrl));
        builder.AppendLine();
    }

    private static void AppendIssueGitSummary(StringBuilder builder, SoftwareIssue issue)
    {
        var hasGitData =
            HasValue(issue.WorkItemType) ||
            HasValue(issue.GitStatus) ||
            HasValue(issue.BranchName) ||
            HasValue(issue.RepositoryUrl) ||
            HasValue(issue.CommitUrl);

        if (!hasGitData)
            return;

        builder.AppendLine("اطلاعات گیت:");
        AppendLine(builder, "نوع کار", ValueOrDash(issue.WorkItemType));
        AppendLine(builder, "وضعیت گیت", ValueOrDash(issue.GitStatus));
        AppendLine(builder, "Branch", ValueOrDash(issue.BranchName));
        AppendLine(builder, "Repository", ValueOrDash(issue.RepositoryUrl));
        AppendLine(builder, "Commit", ValueOrDash(issue.CommitUrl));
        builder.AppendLine();
    }

    private void AppendIssueLinks(StringBuilder builder, SoftwareIssue issue)
    {
        AppendLine(builder, "لینک جزئیات", DetailsUrl(issue.Id));
    }

    private static void AppendLine(StringBuilder builder, string label, string? value)
    {
        builder.AppendLine($"{label}: {ValueOrDash(value)}");
    }

    private static bool HasValue(string? value) => !string.IsNullOrWhiteSpace(value);

    private static string ValueOrDash(string? value) => string.IsNullOrWhiteSpace(value) ? "-" : value.Trim();

    private static string UserDisplayName(User? user)
    {
        if (user == null)
            return "-";

        var fullName = $"{user.FirstName} {user.LastName}".Trim();
        return string.IsNullOrWhiteSpace(fullName) ? "-" : fullName;
    }

    private static string DisplayPriority(string? priority) => priority switch
    {
        "Low" => "کم",
        "Medium" => "متوسط",
        "High" => "زیاد",
        "Urgent" => "فوری",
        _ => ValueOrDash(priority)
    };

    private static string DisplaySeverity(string? severity) => severity switch
    {
        "Minor" => "جزئی",
        "Major" => "مهم",
        "Critical" => "بحرانی",
        _ => ValueOrDash(severity)
    };

    private static string DisplayChannel(string? channel) => channel switch
    {
        "Phone" => "تلفن",
        "Telegram" => "تلگرام",
        "Email" => "ایمیل",
        "In Person" => "حضوری",
        "System" => "سیستم",
        _ => ValueOrDash(channel)
    };

    private static string Truncate(string? value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
            return "-";

        var normalized = value.Trim();
        return normalized.Length <= maxLength ? normalized : normalized[..maxLength] + "...";
    }

    private async Task SendAsync(string text, CancellationToken cancellationToken)
    {
        try
        {
            var url = $"{TelegramApiBase}/bot{BotToken}/sendMessage";
            var body = new
            {
                chat_id = ChatId,
                text = Truncate(text, TelegramMessageLimit),
                disable_web_page_preview = true
            };

            var client = _httpClientFactory.CreateClient("Telegram");
            var response = await client.PostAsJsonAsync(url, body, JsonOptions, cancellationToken).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
                _logger.LogWarning("Telegram API error: {StatusCode} - {Body}", response.StatusCode, errorBody);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "خطا در ارسال نوتیفیکیشن تلگرام");
        }
    }
}
