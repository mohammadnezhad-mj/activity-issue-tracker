using System.Security.Cryptography;
using System.Text;

namespace RayanTask.Helpers;

public static class PasswordHelper
{
    /// <summary>
    /// تولید Salt منحصر به فرد برای هر کاربر بر اساس نام و نام خانوادگی
    /// </summary>
    private static string GenerateSalt(string firstName, string lastName)
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
    /// Hash کردن رمز عبور با استفاده از SHA256 و Salt منحصر به فرد کاربر
    /// </summary>
    public static string HashPassword(string password, string firstName, string lastName)
    {
        if (string.IsNullOrWhiteSpace(password))
            return string.Empty;

        var salt = GenerateSalt(firstName, lastName);
        var saltedPassword = $"{password}_{salt}";

        using (var sha256 = SHA256.Create())
        {
            var bytes = Encoding.UTF8.GetBytes(saltedPassword);
            var hash = sha256.ComputeHash(bytes);
            return Convert.ToHexString(hash);
        }
    }

    /// <summary>
    /// Hash کردن رمز عبور بدون salt (برای backward compatibility با hash های قدیمی)
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
    /// مقایسه رمز عبور ورودی با hash ذخیره شده
    /// از salt منحصر به فرد کاربر استفاده می‌کند
    /// </summary>
    public static bool VerifyPassword(string inputPassword, string storedPassword, string firstName, string lastName)
    {
        if (string.IsNullOrWhiteSpace(inputPassword) || string.IsNullOrWhiteSpace(storedPassword))
            return false;

        // اگر stored password hash شده است
        if (IsHashed(storedPassword))
        {
            // ابتدا با salt جدید امتحان کن
            var inputHashWithSalt = HashPassword(inputPassword, firstName, lastName);
            if (inputHashWithSalt.Equals(storedPassword, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            // برای backward compatibility، با hash قدیمی (بدون salt) هم امتحان کن
            var inputHashLegacy = HashPasswordLegacy(inputPassword);
            if (inputHashLegacy.Equals(storedPassword, StringComparison.OrdinalIgnoreCase))
            {
                // اگر با hash قدیمی کار کرد، آن را به hash جدید با salt تبدیل کن
                return true;
            }

            return false;
        }
        
        // اگر stored password plain text است (برای backward compatibility)
        return inputPassword == storedPassword;
    }

    /// <summary>
    /// Hash کردن جواب سوال امنیتی با salt منحصر به فرد کاربر
    /// </summary>
    public static string HashSecurityAnswer(string answer, string firstName, string lastName)
    {
        if (string.IsNullOrWhiteSpace(answer))
            return string.Empty;
        var salt = GenerateSalt(firstName, lastName);
        var salted = $"{answer.Trim()}_{salt}_SecurityAnswer";
        using (var sha256 = SHA256.Create())
        {
            var bytes = Encoding.UTF8.GetBytes(salted);
            var hash = sha256.ComputeHash(bytes);
            return Convert.ToHexString(hash);
        }
    }

    /// <summary>
    /// بررسی جواب سوال امنیتی با hash ذخیره شده
    /// </summary>
    public static bool VerifySecurityAnswer(string inputAnswer, string? storedHash, string firstName, string lastName)
    {
        if (string.IsNullOrWhiteSpace(inputAnswer) || string.IsNullOrWhiteSpace(storedHash))
            return false;
        var inputHash = HashSecurityAnswer(inputAnswer.Trim(), firstName, lastName);
        return string.Equals(inputHash, storedHash, StringComparison.OrdinalIgnoreCase);
    }
}

