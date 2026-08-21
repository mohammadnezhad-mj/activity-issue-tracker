using RayanTask.Models;

namespace RayanTask.Services;

/// <summary>
/// سرویس ارسال نوتیفیکیشن به تلگرام برای رویدادهای مربوط به Issues
/// </summary>
public interface ITelegramNotificationService
{
    /// <summary>
    /// ارسال نوتیفیکیشن ثبت مشکل جدید
    /// </summary>
    Task NotifyIssueCreatedAsync(SoftwareIssue issue, CancellationToken cancellationToken = default);

    /// <summary>
    /// ارسال نوتیفیکیشن به‌روزرسانی مشکل
    /// </summary>
    Task NotifyIssueUpdatedAsync(SoftwareIssue issue, string changedBy, IEnumerable<string> changes, CancellationToken cancellationToken = default);

    /// <summary>
    /// ارسال نوتیفیکیشن ثبت کامنت جدید روی مشکل
    /// </summary>
    Task NotifyCommentAddedAsync(SoftwareIssue issue, string commenterName, string commentText, CancellationToken cancellationToken = default);

    Task NotifyIssueDeletedAsync(SoftwareIssue issue, string deletedBy, CancellationToken cancellationToken = default);
}
