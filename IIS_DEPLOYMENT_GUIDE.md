# راهنمای استقرار روی IIS - نسخه SQLite

## پیش‌نیازها

### 1. نصب .NET 8.0 Hosting Bundle
- از [microsoft.com](https://dotnet.microsoft.com/download/dotnet/8.0) دانلود کنید
- فایل `dotnet-hosting-8.0.x-win.exe` را نصب کنید
- IIS را Restart کنید

### 2. فعال‌سازی IIS Features
- Windows Features را باز کنید
- موارد زیر را فعال کنید:
  - Internet Information Services
  - ASP.NET Core Module V2
  - .NET Extensibility

## مراحل استقرار

### 1. انتشار پروژه

پروژه قبلاً publish شده است. پوشه `publish` شامل تمام فایل‌های لازم است.

### 2. کپی فایل‌ها به سرور

محتوای پوشه `publish` را به سرور IIS کپی کنید (مثلاً `C:\inetpub\wwwroot\RayanTask`)

**فایل‌های مهم:**
- `RayanTask.dll` - فایل اصلی برنامه
- `web.config` - تنظیمات IIS
- `Data/` - پوشه داده (دیتابیس و config.json)
- `wwwroot/` - فایل‌های استاتیک

### 3. تنظیم Application Pool

1. IIS Manager را باز کنید
2. Application Pools → Add Application Pool:
   - **Name**: `RayanTaskAppPool`
   - **.NET CLR Version**: **No Managed Code**
   - **Managed Pipeline Mode**: **Integrated**
3. روی Application Pool کلیک راست → Advanced Settings:
   - **Identity**: `ApplicationPoolIdentity` یا یک User خاص

### 4. ایجاد Website یا Application

#### گزینه 1: ایجاد Website جدید

1. Sites → Add Website
2. تنظیمات:
   - **Site name**: `RayanTask`
   - **Application pool**: `RayanTaskAppPool`
   - **Physical path**: مسیر پوشه `publish` (مثلاً `C:\inetpub\wwwroot\RayanTask`)
   - **Binding**:
     - Type: `http` یا `https`
     - IP address: `All Unassigned` یا IP خاص
     - Port: `80` یا پورت مورد نظر
     - Host name: (اختیاری) دامنه شما

#### گزینه 2: ایجاد Application در Website موجود

1. روی Website موجود کلیک راست → Add Application
2. تنظیمات:
   - **Alias**: `RayanTask`
   - **Application pool**: `RayanTaskAppPool`
   - **Physical path**: مسیر پوشه `publish`

### 5. تنظیمات دسترسی فایل

پوشه‌های زیر باید دسترسی نوشتن داشته باشند:

#### پوشه Data
1. روی پوشه `publish/Data` کلیک راست → Properties
2. Security tab → Edit
3. Add → Application Pool Identity را اضافه کنید
4. Permissions: **Modify** و **Write** را فعال کنید

#### پوشه logs (برای لاگ‌ها)
1. پوشه `publish/logs` را ایجاد کنید
2. دسترسی **Modify** و **Write** به Application Pool Identity بدهید

### 6. تنظیمات Connection String (اختیاری)

می‌توانید Connection String را در `appsettings.json` تنظیم کنید:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=Data\\rayantask.db"
  }
}
```

یا در `web.config`:

```xml
<aspNetCore>
  <environmentVariables>
    <environmentVariable name="ConnectionStrings__DefaultConnection" value="Data Source=Data\rayantask.db" />
  </environmentVariables>
</aspNetCore>
```

### 7. تست

1. IIS Manager → Sites → Website شما
2. Browse Website را کلیک کنید
3. یا مستقیماً به آدرس `http://localhost/RayanTask` مراجعه کنید

## ساختار فایل‌ها در IIS

```
C:\inetpub\wwwroot\RayanTask\
├── RayanTask.dll
├── web.config
├── Data/
│   ├── rayantask.db          # دیتابیس SQLite
│   ├── config.json           # تنظیمات
│   └── activity_records.json # (اختیاری - برای Migration)
├── wwwroot/                  # فایل‌های استاتیک
├── logs/                     # لاگ‌ها
└── ... (سایر فایل‌های .NET)
```

## عیب‌یابی

### خطای 500.30 (ANCM In-Process Start Failure)
- مطمئن شوید ASP.NET Core Hosting Bundle 8.0 نصب شده است
- IIS را Restart کنید
- Application Pool را Recycle کنید

### خطای دسترسی به دیتابیس
- دسترسی‌های پوشه `Data` را بررسی کنید
- Application Pool Identity باید دسترسی **Modify** و **Write** داشته باشد
- مطمئن شوید فایل `rayantask.db` قابل نوشتن است

### خطای "database is locked"
- SQLite از WAL (Write-Ahead Logging) استفاده می‌کند
- این خطا معمولاً در صورت دسترسی همزمان زیاد رخ می‌دهد
- بررسی کنید که چند Instance از برنامه در حال اجرا نیست

### لاگ‌ها
لاگ‌های ASP.NET Core در مسیر زیر قرار دارند:
```
C:\inetpub\wwwroot\RayanTask\logs\stdout_*.log
```

یا در `web.config` می‌توانید مسیر را تغییر دهید:
```xml
<aspNetCore ... stdoutLogFile=".\logs\stdout" />
```

## تنظیمات پیشرفته

### استفاده از HTTPS
1. یک Certificate در IIS ایجاد یا Import کنید
2. در Binding، Type را به `https` تغییر دهید
3. Port را به `443` تغییر دهید

### تنظیمات Performance
در `web.config`:
```xml
<aspNetCore 
  processPath="dotnet" 
  arguments=".\RayanTask.dll" 
  stdoutLogEnabled="true" 
  stdoutLogFile=".\logs\stdout" 
  hostingModel="inprocess"
  requestTimeout="00:20:00" />
```

### Backup دیتابیس
برای Backup، فقط فایل `Data/rayantask.db` را کپی کنید:
```powershell
Copy-Item "C:\inetpub\wwwroot\RayanTask\Data\rayantask.db" "C:\Backups\rayantask_$(Get-Date -Format 'yyyyMMdd').db"
```

## به‌روزرسانی

برای به‌روزرسانی:

1. پروژه را دوباره Publish کنید:
   ```bash
   dotnet publish -c Release -o ./publish
   ```

2. فایل‌های جدید را در پوشه `publish` جایگزین کنید
   - **مهم**: فایل `Data/rayantask.db` را نگه دارید!
   - **مهم**: فایل `Data/config.json` را نگه دارید!

3. Application Pool را Recycle کنید:
   - IIS Manager → Application Pools → RayanTaskAppPool → Recycle

## نکات امنیتی

1. **دسترسی فایل**: فقط Application Pool Identity باید دسترسی Write داشته باشد
2. **HTTPS**: برای Production از HTTPS استفاده کنید
3. **Backup**: به صورت منظم از دیتابیس Backup بگیرید
4. **Logs**: لاگ‌ها را به صورت منظم بررسی کنید

## پشتیبانی SQLite در IIS

SQLite در IIS به خوبی کار می‌کند، اما:
- برای تعداد کاربران زیاد (بیش از 50 همزمان) ممکن است نیاز به SQL Server باشد
- SQLite از WAL استفاده می‌کند که برای Concurrency مناسب است
- فایل دیتابیس باید در مسیری باشد که Application Pool دسترسی Write دارد

