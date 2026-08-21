# سیستم مدیریت فعالیت‌ها - نسخه تحت وب

پروژه کامل ASP.NET Core MVC برای مدیریت و ذخیره‌سازی فعالیت‌ها که روی IIS قابل اجرا است.

## ویژگی‌ها

- ✅ برنامه تحت وب ASP.NET Core MVC
- ✅ ذخیره‌سازی داده‌ها در پایگاه داده **SQLite** (از طریق Entity Framework Core)
- ✅ مدیریت کاربران، نوع فعالیت و نتایج از طریق پنل ادمین
- ✅ رابط کاربری Responsive با Bootstrap
- ✅ اعتبارسنجی ورودی‌ها
- ✅ پشتیبانی از RTL (راست به چپ) برای فارسی

## پایگاه داده

این پروژه از **SQLite** استفاده می‌کند (نه فایل JSON). مسیر پیش‌فرض فایل دیتابیس `Data/rayantask.db` است و در اولین اجرای برنامه (`dbContext.Database.Migrate()` در [Program.cs](Program.cs)) به‌صورت خودکار ساخته و Migrationهای لازم روی آن اعمال می‌شود؛ نیازی به اجرای دستی اسکریپت SQL نیست، مگر برای موارد خاص (مستندات آن در [DatabaseScripts/README.md](DatabaseScripts/README.md)).

می‌توانید مسیر/نوع اتصال را با تنظیم `ConnectionStrings:DefaultConnection` در `appsettings.json` یا متغیر محیطی override کنید:

```bash
ConnectionStrings__DefaultConnection="Data Source=Data/rayantask.db"
```

## فیلدهای داده

1. **نام و نام خانوادگی**: از جدول `Users` بارگذاری می‌شود
2. **تاریخ**: پیش‌فرض تاریخ امروز است
3. **نوع فعالیت**: از جدول `ActivityTypes` بارگذاری می‌شود
4. **شرح**: حداکثر 1000 کاراکتر
5. **مدت زمان**: بر حسب دقیقه
6. **ذی‌نفع / مشتری**: ورودی متنی
7. **نتیجه**: از جدول `Results` بارگذاری می‌شود

## نحوه اجرا

### اجرای محلی (Development)

```bash
dotnet restore
dotnet run
```

سپس به آدرس `http://localhost:5000` مراجعه کنید.

### انتشار برای IIS

1. **Publish کردن پروژه:**
   ```bash
   dotnet publish -c Release -o ./publish
   ```

2. **نصب ASP.NET Core Hosting Bundle:**
   - از [microsoft.com](https://dotnet.microsoft.com/download/dotnet/8.0) دانلود و نصب کنید
   - شامل .NET Runtime و ASP.NET Core Module برای IIS

3. **تنظیم IIS:**
   - IIS Manager را باز کنید
   - یک Application Pool جدید ایجاد کنید:
     - .NET CLR Version: No Managed Code
     - Managed Pipeline Mode: Integrated
   - یک Website یا Application جدید ایجاد کنید:
     - Physical Path: مسیر پوشه `publish`
     - Application Pool: همان Application Pool که ایجاد کردید
     - Binding: پورت و hostname مورد نظر

4. **تنظیمات امنیتی:**
   - اطمینان حاصل کنید که IIS_IUSRS و Application Pool Identity دسترسی خواندن/نوشتن به پوشه `Data` دارند

5. **تست:**
   - به آدرس تنظیم شده در IIS مراجعه کنید

## ساختار پروژه

```
RayanTask/
├── Controllers/          # کنترلرهای MVC
├── Models/              # مدل‌های داده
├── Services/            # سرویس‌های کسب و کار
├── Views/               # صفحات Razor
├── wwwroot/             # فایل‌های استاتیک
├── Data/                # فایل دیتابیس SQLite (rayantask.db)
├── Migrations/          # Migrationهای EF Core
├── DatabaseScripts/     # نسخه‌ی SQL خام همان Migrationها (برای اجرای دستی)
├── Properties/          # تنظیمات پروژه
├── Program.cs           # نقطه ورود برنامه
└── web.config           # تنظیمات IIS
```

## ویرایش تنظیمات (نام‌ها، نوع فعالیت، نتایج)

نام‌ها، نوع فعالیت‌ها و نتایج دیگر از فایل JSON خوانده نمی‌شوند و از طریق پنل ادمین (`/Admin`) در دیتابیس مدیریت می‌شوند. برای دسترسی به پنل ادمین باید یک کاربر با `IsAdmin = true` داشته باشید — به بخش «ساخت اولین کاربر ادمین» مراجعه کنید.

## نیازمندی‌ها

- .NET 8.0 SDK
- IIS 10 یا بالاتر (برای اجرا روی IIS)
- ASP.NET Core Hosting Bundle 8.0

## راه‌اندازی تنظیمات محلی

فایل‌های تنظیمات و داده‌های واقعی (شامل توکن‌ها) در گیت قرار ندارند. قبل از اجرا:

```bash
cp appsettings.example.json appsettings.json
cp Data/config.example.json Data/config.json
```

سپس مقادیر واقعی (مثل `Telegram:BotToken` و `Telegram:ChatId`) را در `appsettings.json` وارد کنید. این مقادیر همچنین از طریق متغیرهای محیطی هم قابل تنظیم هستند، مثلاً:

```bash
Telegram__BotToken=your-token
Telegram__ChatId=your-chat-id
```

## ساخت اولین کاربر ادمین

سیستم صفحه‌ی ثبت‌نام عمومی ندارد؛ کاربران فقط توسط ادمین از داخل پنل ساخته می‌شوند. بنابراین **اولین** کاربر ادمین باید مستقیماً در دیتابیس SQLite ساخته شود.

رمز عبور در جدول `Users` به‌صورت `SHA256(password + "_" + SHA256("RayanTask_{FirstName}_{LastName}_Salt2024"))` (خروجی hex بزرگ) ذخیره می‌شود (پیاده‌سازی در [Helpers/PasswordHelper.cs](Helpers/PasswordHelper.cs)). برای تولید همین hash می‌توانید از دستور PowerShell زیر استفاده کنید:

```powershell
function Get-RayanTaskPasswordHash {
    param([string]$Password, [string]$FirstName, [string]$LastName)
    $sha256 = [System.Security.Cryptography.SHA256]::Create()
    $toHex = { param($bytes) -join ($bytes | ForEach-Object { $_.ToString("X2") }) }
    $salt = & $toHex $sha256.ComputeHash([Text.Encoding]::UTF8.GetBytes("RayanTask_${FirstName}_${LastName}_Salt2024"))
    & $toHex $sha256.ComputeHash([Text.Encoding]::UTF8.GetBytes("${Password}_${salt}"))
}

Get-RayanTaskPasswordHash -Password "YourStrongPassword" -FirstName "مدیر" -LastName "سیستم"
```

سپس hash خروجی را در دستور زیر جایگزین `<HASH>` کنید و روی فایل دیتابیس اجرا کنید (اگر برنامه را حداقل یک‌بار اجرا کرده باشید، `Data/rayantask.db` و جدول `Users` از قبل توسط Migrate خودکار ساخته شده‌اند):

```bash
sqlite3 Data/rayantask.db "INSERT INTO Users
  (FirstName, LastName, Password, IsAdmin, CanEdit, CanDelete, CanSelectAnyDate,
   IsActive, CanManageUsers, CanManageActivityTypes, CanManageResults, CanViewReports,
   CanViewLogs, CanViewIssueDetails, CanEditIssues, CanDeleteIssues, CanDeleteReports,
   CanViewActivityList, CreatedAt)
VALUES
  ('مدیر', 'سیستم', '<HASH>', 1, 1, 1, 1,
   1, 1, 1, 1, 1,
   1, 1, 1, 1, 1,
   1, datetime('now'));"
```

بعد از این، با نام و نام خانوادگی `مدیر سیستم` و رمز عبوری که برای هش انتخاب کردید وارد شوید؛ از همان‌جا می‌توانید بقیه‌ی کاربران را از پنل ادمین بسازید.

## مشارکت (Contributing)

این پروژه متن‌باز است و از Fork و Pull Request استقبال می‌شود. لطفاً پیش از ارسال تغییرات بزرگ، ابتدا یک Issue باز کنید تا درباره‌ی رویکرد هماهنگ شویم.

## لایسنس

این پروژه تحت [MIT License](LICENSE) منتشر شده است.
