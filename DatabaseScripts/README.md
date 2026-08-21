# Database Migration Scripts

این پوشه شامل SQL Scriptهای تمام Migrationهای دیتابیس به ترتیب زمانی است.

## نحوه استفاده

برای بروزرسانی دستی دیتابیس به آخرین نسخه، Scriptها را به ترتیب شماره اجرا کنید:

1. `01_20251227142222_InitialCreate.sql` - ایجاد جدول ActivityRecords
2. `02_20251227165142_AddAuditLogs.sql` - ایجاد جدول AuditLogs
3. `03_20251228090631_AddSoftwareIssues.sql` - ایجاد جدول SoftwareIssues
4. `04_20251228133317_AddIssueAttachments.sql` - اضافه کردن IssueType و ایجاد جداول IssueAttachments و IssueComments
5. `05_20251228141756_AddCommentAttachments.sql` - ایجاد جدول CommentAttachments
6. `06_20251228144831_AddConfigTables.sql` - ایجاد جداول پیکربندی (ActivityTypes, IssueTypes, Results, SoftwareNames, Users)
7. `07_20251228165547_ConvertToRelations.sql` - تبدیل فیلدهای متنی ActivityRecords به Foreign Key
8. `08_20251229080628_ConvertSoftwareIssuesToRelations.sql` - تبدیل فیلدهای متنی SoftwareIssues به Foreign Key و ایجاد جدول Statuses
9. `09_20251229090103_AddCustomerNameToSoftwareIssues.sql` - اضافه کردن فیلد CustomerName

## اجرای Scriptها

### روش 1: اجرای همه Migrationها به صورت یکجا (توصیه می‌شود)

برای ایجاد دیتابیس جدید یا بروزرسانی کامل، از فایل `ALL_MIGRATIONS.sql` استفاده کنید:

```bash
sqlite3 rayanpersis.db < DatabaseScripts/ALL_MIGRATIONS.sql
```

این فایل شامل تمام Migrationها به ترتیب است و Migration History را نیز ایجاد می‌کند.

### روش 2: اجرای Scriptها به صورت جداگانه

اگر می‌خواهید Migrationها را به صورت جداگانه اجرا کنید:

```bash
# فعال‌سازی Foreign Keys
sqlite3 rayanpersis.db < DatabaseScripts/00_EnableForeignKeys.sql

# اجرای Migrationها به ترتیب
sqlite3 rayanpersis.db < DatabaseScripts/01_20251227142222_InitialCreate.sql
sqlite3 rayanpersis.db < DatabaseScripts/02_20251227165142_AddAuditLogs.sql
sqlite3 rayanpersis.db < DatabaseScripts/03_20251228090631_AddSoftwareIssues.sql
sqlite3 rayanpersis.db < DatabaseScripts/04_20251228133317_AddIssueAttachments.sql
sqlite3 rayanpersis.db < DatabaseScripts/05_20251228141756_AddCommentAttachments.sql
sqlite3 rayanpersis.db < DatabaseScripts/06_20251228144831_AddConfigTables.sql
sqlite3 rayanpersis.db < DatabaseScripts/07_20251228165547_ConvertToRelations.sql
sqlite3 rayanpersis.db < DatabaseScripts/08_20251229080628_ConvertSoftwareIssuesToRelations.sql
sqlite3 rayanpersis.db < DatabaseScripts/09_20251229090103_AddCustomerNameToSoftwareIssues.sql

# ایجاد Migration History
sqlite3 rayanpersis.db < DatabaseScripts/10_CreateMigrationHistory.sql
```

### یا اجرای همه Scriptها به صورت یکجا با حلقه:

```bash
for file in DatabaseScripts/0*.sql DatabaseScripts/1*.sql; do
    sqlite3 rayanpersis.db < "$file"
done
```

### استفاده از SQLite Browser یا ابزارهای دیگر:

1. فایل دیتابیس را در SQLite Browser باز کنید
2. هر Script را به ترتیب شماره باز کنید
3. محتوای Script را کپی کرده و در تب "Execute SQL" اجرا کنید

## نکات مهم

1. **پشتیبان‌گیری**: قبل از اجرای هر Script، از دیتابیس خود پشتیبان بگیرید.

2. **ترتیب اجرا**: Scriptها باید به ترتیب شماره اجرا شوند. اجرای Scriptها به ترتیب اشتباه ممکن است باعث خطا شود.

3. **SQLite Limitations**: 
   - SQLite از `DROP COLUMN` پشتیبانی نمی‌کند. در Migration 07 و 08، فیلدهای قدیمی باید به صورت دستی حذف شوند یا از روش ایجاد جدول جدید استفاده شود.
   - SQLite از `ALTER COLUMN` برای تغییر نوع یا NULL بودن فیلد پشتیبانی نمی‌کند.

4. **Foreign Keys**: برای فعال‌سازی Foreign Key Constraints در SQLite، باید `PRAGMA foreign_keys = ON;` را قبل از اجرای Scriptها اجرا کنید.

5. **Migration History**: اگر می‌خواهید Migration History را نیز ثبت کنید، باید جدول `__EFMigrationsHistory` را ایجاد کرده و رکوردهای مربوط به هر Migration را اضافه کنید:

```sql
CREATE TABLE IF NOT EXISTS "__EFMigrationsHistory" (
    "MigrationId" TEXT NOT NULL CONSTRAINT "PK___EFMigrationsHistory" PRIMARY KEY,
    "ProductVersion" TEXT NOT NULL
);

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion") VALUES
('20251227142222_InitialCreate', '8.0.0'),
('20251227165142_AddAuditLogs', '8.0.0'),
('20251228090631_AddSoftwareIssues', '8.0.0'),
('20251228133317_AddIssueAttachments', '8.0.0'),
('20251228141756_AddCommentAttachments', '8.0.0'),
('20251228144831_AddConfigTables', '8.0.0'),
('20251228165547_ConvertToRelations', '8.0.0'),
('20251229080628_ConvertSoftwareIssuesToRelations', '8.0.0'),
('20251229090103_AddCustomerNameToSoftwareIssues', '8.0.0');
```

## ساختار فایل‌ها

هر فایل SQL شامل:
- کامنت‌های توضیحی در ابتدای فایل
- دستورات SQL مربوط به Migration
- کامنت‌های راهنما برای عملیات پیچیده

## بروزرسانی

هر بار که Migration جدیدی اضافه می‌شود، یک فایل SQL جدید در این پوشه ایجاد می‌شود و این README به‌روزرسانی می‌شود.

