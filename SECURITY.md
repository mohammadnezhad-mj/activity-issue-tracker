# راهنمای امنیتی - سیستم مدیریت فعالیت‌ها

این سند شامل نکات امنیتی پیاده‌سازی شده در سیستم است.

## ✅ موارد امنیتی پیاده‌سازی شده

### 1. **HTTPS Enforcement**
- تمام ارتباطات در محیط Production از HTTPS استفاده می‌کنند
- HSTS (HTTP Strict Transport Security) فعال است
- Redirect خودکار از HTTP به HTTPS

### 2. **Security Headers**
- `X-Frame-Options: DENY` - جلوگیری از Clickjacking
- `X-Content-Type-Options: nosniff` - جلوگیری از MIME sniffing
- `X-XSS-Protection: 1; mode=block` - محافظت در برابر XSS
- `Content-Security-Policy` - محدود کردن منابع قابل اجرا
- `Strict-Transport-Security` - اجباری کردن HTTPS
- `Referrer-Policy` - کنترل اطلاعات ارسالی در Referrer

### 3. **Session Security**
- Cookie های Session با `HttpOnly` flag (جلوگیری از دسترسی JavaScript)
- Cookie های Session با `Secure` flag (فقط HTTPS)
- Cookie های Session با `SameSite=Strict` (محافظت در برابر CSRF)
- Timeout خودکار Session بعد از 8 ساعت عدم فعالیت
- نام سفارشی برای Cookie Session

### 4. **Rate Limiting**
- محدودیت 5 درخواست ورود در هر 15 دقیقه برای هر IP
- جلوگیری از Brute Force attacks
- پیام مناسب برای کاربر در صورت محدودیت

### 5. **Input Validation & Sanitization**
- بررسی طول ورودی‌ها
- HTML Encoding برای جلوگیری از XSS
- Validation در سمت سرور برای تمام ورودی‌ها
- Whitelist validation برای dropdown ها

### 6. **Password Security**
- Hash کردن رمز عبور با SHA256 و Salt منحصر به فرد
- Salt بر اساس نام و نام خانوادگی کاربر
- Backward compatibility با رمزهای عبور قدیمی
- عدم ذخیره رمز عبور به صورت Plain Text

### 7. **CSRF Protection**
- استفاده از `ValidateAntiForgeryToken` در تمام فرم‌های POST
- Token validation خودکار توسط ASP.NET Core

### 8. **Authorization & Access Control**
- بررسی دسترسی در تمام Controller ها
- Session-based authentication
- Role-based access control (Admin, Regular User)
- Permission-based access control برای بخش‌های مختلف

### 9. **SQL Injection Protection**
- استفاده از Entity Framework Core (Parameterized Queries)
- عدم استفاده از Raw SQL Queries
- Type-safe queries

### 10. **Error Handling**
- عدم نمایش جزئیات خطا به کاربر در Production
- Logging خطاها برای بررسی توسط مدیر
- پیام‌های خطای عمومی و کاربرپسند

### 11. **Audit Logging**
- ثبت تمام عملیات CRUD
- ثبت تلاش‌های ناموفق ورود
- ذخیره اطلاعات کاربر، زمان و نوع عملیات
- امکان ردیابی تغییرات

### 12. **XSS Protection**
- HTML Encoding در تمام خروجی‌های View
- استفاده از Razor syntax که به صورت خودکار encode می‌کند
- Sanitization ورودی‌ها قبل از ذخیره

## 🔒 توصیه‌های امنیتی برای استقرار

### 1. **SSL/TLS Certificate**
- استفاده از گواهینامه SSL معتبر
- تنظیمات صحیح در IIS یا Web Server

### 2. **Firewall**
- محدود کردن دسترسی به پورت‌های غیرضروری
- Whitelist IP addresses در صورت نیاز

### 3. **Database Security**
- محافظت از فایل SQLite
- Backup منظم
- محدود کردن دسترسی به فایل دیتابیس

### 4. **Configuration Security**
- محافظت از فایل `config.json`
- عدم قرار دادن در public directory
- استفاده از Environment Variables برای اطلاعات حساس

### 5. **Monitoring**
- بررسی لاگ‌ها به صورت منظم
- مانیتورینگ تلاش‌های ناموفق ورود
- Alert در صورت فعالیت مشکوک

### 6. **Updates**
- به‌روزرسانی منظم .NET Runtime
- به‌روزرسانی پکیج‌های NuGet
- بررسی Security Advisories

### 7. **Backup**
- Backup منظم دیتابیس
- Backup فایل `config.json`
- تست Restore به صورت دوره‌ای

## ⚠️ نکات مهم

1. **رمز عبور مدیر سیستم**: از رمز عبور قوی استفاده کنید
2. **Session Timeout**: در صورت نیاز می‌توانید timeout را کاهش دهید
3. **Rate Limiting**: در صورت نیاز می‌توانید محدودیت را تنظیم کنید
4. **Log Retention**: لاگ‌ها را به صورت دوره‌ای پاک کنید تا فضای دیتابیس پر نشود

## 📝 چک‌لیست قبل از استقرار

- [ ] SSL Certificate نصب شده
- [ ] HTTPS فعال است
- [ ] Security Headers بررسی شده
- [ ] Rate Limiting فعال است
- [ ] Session Security تنظیم شده
- [ ] Database Backup انجام شده
- [ ] config.json محافظت شده
- [ ] Firewall تنظیم شده
- [ ] Monitoring فعال است
- [ ] Error Logging بررسی شده

