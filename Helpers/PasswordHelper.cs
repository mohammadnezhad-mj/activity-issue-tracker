using System.Security.Cryptography;
using System.Text;

namespace RayanTask.Helpers;

public static class PasswordHelper
{
    /// <summary>
    /// تولید Salt منحصر به فرد برای هر کاربر بر اساس شناسه‌ی ثابت کاربر (تغییر نام/نام‌خانوادگی روی آن اثر ندارد)
    /// </summary>
    private static string GenerateSalt(int userId)
    {
        var saltInput = $"RayanTask_User{userId}_Salt2024";
        using (var sha256 = SHA256.Create())
        {
            var bytes = Encoding.UTF8.GetBytes(saltInput);
            var hash = sha256.ComputeHash(bytes);
            return Convert.ToHexString(hash);
        }
    }

    /// <summary>
    /// نسخه‌ی قدیمی Salt که بر اساس نام و نام‌خانوادگی ساخته می‌شد.
    /// فقط برای سازگاری با هش‌های ذخیره‌شده‌ی قدیمی نگه داشته شده؛ در VerifyPassword به عنوان fallback استفاده می‌شود
    /// و در صورت موفقیت، هش بلافاصله به نسخه‌ی مبتنی بر شناسه ارتقا پیدا می‌کند.
    /// </summary>
    private static string GenerateSaltLegacyByName(string firstName, string lastName)
    {
        var saltInput = $"RayanTask_{firstName}_{lastName}_Salt2024";
        using (var sha256 = SHA256.Create())
        {
            var bytes = Encoding.UTF8.GetBytes(saltInput);
            var hash = sha256.ComputeHash(bytes);
            return Convert.ToHexString(hash);
        }
    }

    /// <summary>
    /// Hash کردن رمز عبور با استفاده از SHA256 و Salt منحصر به فرد کاربر (بر اساس شناسه‌ی کاربر)
    /// </summary>
    public static string HashPassword(string password, int userId)
    {
        if (string.IsNullOrWhiteSpace(password))
            return string.Empty;

        var salt = GenerateSalt(userId);
        var saltedPassword = $"{password}_{salt}";

        using (var sha256 = SHA256.Create())
        {
            var bytes = Encoding.UTF8.GetBytes(saltedPassword);
            var hash = sha256.ComputeHash(bytes);
            return Convert.ToHexString(hash);
        }
    }

    private static string HashPasswordLegacyByName(string password, string firstName, string lastName)
    {
        var salt = GenerateSaltLegacyByName(firstName, lastName);
        var saltedPassword = $"{password}_{salt}";

        using (var sha256 = SHA256.Create())
        {
            var bytes = Encoding.UTF8.GetBytes(saltedPassword);
            var hash = sha256.ComputeHash(bytes);
            return Convert.ToHexString(hash);
        }
    }

    /// <summary>
    /// Hash کردن رمز عبور بدون salt (برای backward compatibility با hash های خیلی قدیمی)
    /// </summary>
    public static string HashPasswordLegacy(string password)
    {
        if (string.IsNullOrWhiteSpace(password))
            return string.Empty;

        using (var sha256 = SHA256.Create())
        {
            var bytes = Encoding.UTF8.GetBytes(password);
            var hash = sha256.ComputeHash(bytes);
            return Convert.ToHexString(hash);
        }
    }

    /// <summary>
    /// بررسی اینکه آیا رشته داده شده یک hash است یا plain text
    /// Hash SHA256 همیشه 64 کاراکتر hex است
    /// </summary>
    public static bool IsHashed(string password)
    {
        if (string.IsNullOrWhiteSpace(password))
            return false;

        // SHA256 hash همیشه 64 کاراکتر hex است
        return password.Length == 64 &&
               password.All(c => (c >= '0' && c <= '9') || (c >= 'A' && c <= 'F') || (c >= 'a' && c <= 'f'));
    }

    /// <summary>
    /// مقایسه رمز عبور ورودی با hash ذخیره شده.
    /// ابتدا با salt مبتنی بر شناسه‌ی کاربر (که با تغییر نام/نام‌خانوادگی عوض نمی‌شود) بررسی می‌شود،
    /// سپس برای سازگاری با داده‌های قدیمی‌تر، با salt مبتنی بر نام فعلی و در نهایت بدون salt نیز بررسی می‌شود.
    /// </summary>
    public static bool VerifyPassword(string inputPassword, string storedPassword, int userId, string firstName, string lastName)
    {
        if (string.IsNullOrWhiteSpace(inputPassword) || string.IsNullOrWhiteSpace(storedPassword))
            return false;

        if (IsHashed(storedPassword))
        {
            if (HashPassword(inputPassword, userId).Equals(storedPassword, StringComparison.OrdinalIgnoreCase))
                return true;

            if (HashPasswordLegacyByName(inputPassword, firstName, lastName).Equals(storedPassword, StringComparison.OrdinalIgnoreCase))
                return true;

            if (HashPasswordLegacy(inputPassword).Equals(storedPassword, StringComparison.OrdinalIgnoreCase))
                return true;

            return false;
        }

        // اگر stored password plain text است (برای backward compatibility)
        return inputPassword == storedPassword;
    }

    /// <summary>
    /// Hash کردن جواب سوال امنیتی با salt منحصر به فرد کاربر (بر اساس شناسه‌ی کاربر)
    /// </summary>
    public static string HashSecurityAnswer(string answer, int userId)
    {
        if (string.IsNullOrWhiteSpace(answer))
            return string.Empty;
        var salt = GenerateSalt(userId);
        var salted = $"{answer.Trim()}_{salt}_SecurityAnswer";
        using (var sha256 = SHA256.Create())
        {
            var bytes = Encoding.UTF8.GetBytes(salted);
            var hash = sha256.ComputeHash(bytes);
            return Convert.ToHexString(hash);
        }
    }

    private static string HashSecurityAnswerLegacyByName(string answer, string firstName, string lastName)
    {
        var salt = GenerateSaltLegacyByName(firstName, lastName);
        var salted = $"{answer.Trim()}_{salt}_SecurityAnswer";
        using (var sha256 = SHA256.Create())
        {
            var bytes = Encoding.UTF8.GetBytes(salted);
            var hash = sha256.ComputeHash(bytes);
            return Convert.ToHexString(hash);
        }
    }

    /// <summary>
    /// بررسی جواب سوال امنیتی با hash ذخیره شده (با fallback به نسخه‌ی قدیمی مبتنی بر نام)
    /// </summary>
    public static bool VerifySecurityAnswer(string inputAnswer, string? storedHash, int userId, string firstName, string lastName)
    {
        if (string.IsNullOrWhiteSpace(inputAnswer) || string.IsNullOrWhiteSpace(storedHash))
            return false;

        if (HashSecurityAnswer(inputAnswer.Trim(), userId).Equals(storedHash, StringComparison.OrdinalIgnoreCase))
            return true;

        if (HashSecurityAnswerLegacyByName(inputAnswer.Trim(), firstName, lastName).Equals(storedHash, StringComparison.OrdinalIgnoreCase))
            return true;

        return false;
    }
}
