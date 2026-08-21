namespace RayanTask.Models;

public class IssueAttachment
{
    public int Id { get; set; }
    public int IssueId { get; set; } // Foreign key to SoftwareIssue
    public string FileName { get; set; } = string.Empty; // نام اصلی فایل
    public string StoredFileName { get; set; } = string.Empty; // نام فایل ذخیره شده در سرور
    public string FilePath { get; set; } = string.Empty; // مسیر کامل فایل
    public string ContentType { get; set; } = string.Empty; // نوع فایل (image/jpeg, video/mp4, etc.)
    public long FileSize { get; set; } // اندازه فایل به بایت
    public DateTime UploadedAt { get; set; } = DateTime.Now; // زمان آپلود
    public string UploadedByFirstName { get; set; } = string.Empty; // کسی که فایل را آپلود کرده
    public string UploadedByLastName { get; set; } = string.Empty;
    
    // Navigation property
    public SoftwareIssue? Issue { get; set; }
    
    // برای نمایش اندازه فایل به صورت خوانا
    public string GetFileSizeDisplay()
    {
        if (FileSize < 1024)
            return $"{FileSize} بایت";
        
        if (FileSize < 1024 * 1024)
            return $"{FileSize / 1024.0:F2} کیلوبایت";
        
        if (FileSize < 1024 * 1024 * 1024)
            return $"{FileSize / (1024.0 * 1024.0):F2} مگابایت";
        
        return $"{FileSize / (1024.0 * 1024.0 * 1024.0):F2} گیگابایت";
    }
    
    // بررسی اینکه آیا فایل تصویر است
    public bool IsImage()
    {
        var extension = Path.GetExtension(FileName);
        return ContentType.StartsWith("image/") ||
            extension.Equals(".jpg", StringComparison.OrdinalIgnoreCase) ||
            extension.Equals(".jpeg", StringComparison.OrdinalIgnoreCase) ||
            extension.Equals(".png", StringComparison.OrdinalIgnoreCase) ||
            extension.Equals(".gif", StringComparison.OrdinalIgnoreCase) ||
            extension.Equals(".webp", StringComparison.OrdinalIgnoreCase) ||
            extension.Equals(".bmp", StringComparison.OrdinalIgnoreCase);
    }
    
    // بررسی اینکه آیا فایل ویدیو است
    public bool IsVideo()
    {
        var extension = Path.GetExtension(FileName);
        return ContentType.StartsWith("video/") ||
            extension.Equals(".mp4", StringComparison.OrdinalIgnoreCase) ||
            extension.Equals(".webm", StringComparison.OrdinalIgnoreCase) ||
            extension.Equals(".mov", StringComparison.OrdinalIgnoreCase) ||
            extension.Equals(".m4v", StringComparison.OrdinalIgnoreCase) ||
            extension.Equals(".avi", StringComparison.OrdinalIgnoreCase) ||
            extension.Equals(".mkv", StringComparison.OrdinalIgnoreCase);
    }
}



