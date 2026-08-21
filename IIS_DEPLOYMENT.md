# راهنمای استقرار روی IIS

## پیش‌نیازها

1. **نصب .NET 8.0 Runtime و Hosting Bundle**
   - از [microsoft.com](https://dotnet.microsoft.com/download/dotnet/8.0) دانلود کنید
   - فایل `dotnet-hosting-8.0.x-win.exe` را نصب کنید
   - IIS را Restart کنید

2. **فعال‌سازی IIS Features**
   - Windows Features را باز کنید
   - موارد زیر را فعال کنید:
     - Internet Information Services
     - ASP.NET Core Module V2
     - .NET Extensibility

## مراحل استقرار

### 1. انتشار پروژه

```bash
dotnet publish -c Release -o ./publish
```

این دستور فایل‌های مورد نیاز را در پوشه `publish` ایجاد می‌کند.

### 2. کپی فایل‌ها به سرور

پوشه `publish` را به سرور IIS کپی کنید (مثلاً `C:\inetpub\wwwroot\RayanTask`)

### 3. تنظیم Application Pool

1. IIS Manager را باز کنید
2. Application Pools را باز کنید
3. Add Application Pool را کلیک کنید:
   - Name: `RayanTaskAppPool`
   - .NET CLR Version: **No Managed Code**
   - Managed Pipeline Mode: **Integrated**
4. روی Application Pool کلیک راست → Advanced Settings:
   - Identity: می‌توانید از ApplicationPoolIdentity استفاده کنید یا یک User خاص تعریف کنید

### 4. ایجاد Website یا Application

#### گزینه 1: ایجاد Website جدید

1. Sites → Add Website
2. تنظیمات:
   - Site name: `RayanTask`
   - Application pool: `RayanTaskAppPool`
   - Physical path: مسیر پوشه `publish` (مثلاً `C:\inetpub\wwwroot\RayanTask`)
   - Binding:
     - Type: http
     - IP address: All Unassigned یا IP خاص
     - Port: 80 یا پورت مورد نظر
     - Host name: (اختیاری) دامنه شما

#### گزینه 2: ایجاد Application در Website موجود

1. روی Website موجود کلیک راست → Add Application
2. تنظیمات:
   - Alias: `RayanTask`
   - Application pool: `RayanTaskAppPool`
   - Physical path: مسیر پوشه `publish`

### 5. تنظیمات دسترسی فایل

پوشه `Data` باید دسترسی نوشتن داشته باشد:

1. روی پوشه `publish/Data` کلیک راست → Properties
2. Security tab → Edit
3. Add → Application Pool Identity را اضافه کنید
4. Permissions: Modify و Write را فعال کنید

یا می‌توانید از یک User خاص استفاده کنید و آن را به Application Pool اختصاص دهید.

### 6. تست

1. IIS Manager → Sites → Website شما
2. Browse Website را کلیک کنید
3. یا مستقیماً به آدرس `http://localhost/RayanTask` مراجعه کنید

## عیب‌یابی

### خطای 500.30

- مطمئن شوید ASP.NET Core Hosting Bundle نصب شده است
- IIS را Restart کنید

### خطای دسترسی به فایل

- دسترسی‌های پوشه `Data` را بررسی کنید
- Application Pool Identity باید دسترسی Write داشته باشد

### لاگ‌ها

لاگ‌های ASP.NET Core در مسیر زیر قرار دارند:
```
C:\inetpub\logs\LogFiles\
```

یا می‌توانید در `web.config` stdout logging را فعال کنید:
```xml
<aspNetCore ... stdoutLogEnabled="true" stdoutLogFile=".\logs\stdout" />
```

## تنظیمات پیشرفته

### استفاده از HTTPS

1. یک Certificate در IIS ایجاد یا Import کنید
2. در Binding، Type را به https تغییر دهید
3. Port را به 443 تغییر دهید

### تنظیمات Performance

در `web.config` می‌توانید تنظیمات زیر را اضافه کنید:
```xml
<aspNetCore 
  processPath="dotnet" 
  arguments=".\RayanTask.dll" 
  stdoutLogEnabled="false" 
  stdoutLogFile=".\logs\stdout" 
  hostingModel="inprocess"
  requestTimeout="00:20:00" />
```

## به‌روزرسانی

برای به‌روزرسانی:

1. پروژه را دوباره Publish کنید
2. فایل‌های جدید را در پوشه `publish` جایگزین کنید
3. Application Pool را Recycle کنید:
   - IIS Manager → Application Pools → RayanTaskAppPool → Recycle

