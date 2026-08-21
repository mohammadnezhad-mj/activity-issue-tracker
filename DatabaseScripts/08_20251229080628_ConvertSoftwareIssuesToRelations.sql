-- Migration: 20251229080628_ConvertSoftwareIssuesToRelations
-- Description: تبدیل فیلدهای متنی SoftwareIssues به Foreign Key و ایجاد جدول Statuses
-- Date: 2025-12-29

-- Step 1: اضافه کردن فیلدهای جدید به صورت nullable
ALTER TABLE "SoftwareIssues" ADD COLUMN "AssignedToId" INTEGER;
ALTER TABLE "SoftwareIssues" ADD COLUMN "IssueTypeId" INTEGER;
ALTER TABLE "SoftwareIssues" ADD COLUMN "ReporterId" INTEGER;
ALTER TABLE "SoftwareIssues" ADD COLUMN "SoftwareNameId" INTEGER;
ALTER TABLE "SoftwareIssues" ADD COLUMN "StatusId" INTEGER;

-- Step 2: ایجاد جدول Statuses
CREATE TABLE IF NOT EXISTS "Statuses" (
    "Id" INTEGER NOT NULL CONSTRAINT "PK_Statuses" PRIMARY KEY AUTOINCREMENT,
    "Value" TEXT NOT NULL,
    "IsActive" INTEGER NOT NULL,
    "CreatedAt" TEXT NOT NULL,
    "UpdatedAt" TEXT
);

-- ایجاد ایندکس یکتا برای Statuses
CREATE UNIQUE INDEX IF NOT EXISTS "IX_Statuses_Value" ON "Statuses" ("Value");

-- Step 3: Seed کردن Statusهای پیش‌فرض
INSERT OR IGNORE INTO "Statuses" ("Value", "IsActive", "CreatedAt")
VALUES 
    ('ثبت شده', 1, datetime('now')),
    ('در حال بررسی', 1, datetime('now')),
    ('در حال انجام', 1, datetime('now')),
    ('تکمیل شده', 1, datetime('now')),
    ('لغو شده', 1, datetime('now'));

-- Step 4: انتقال داده‌ها از فیلدهای قدیمی به جدید
-- انتقال SoftwareName
UPDATE "SoftwareIssues"
SET "SoftwareNameId" = (
    SELECT "Id" FROM "SoftwareNames" 
    WHERE "SoftwareNames"."Value" = "SoftwareIssues"."SoftwareName" 
    LIMIT 1
)
WHERE "SoftwareNameId" IS NULL;

-- انتقال IssueType
UPDATE "SoftwareIssues"
SET "IssueTypeId" = (
    SELECT "Id" FROM "IssueTypes" 
    WHERE "IssueTypes"."Value" = "SoftwareIssues"."IssueType" 
    LIMIT 1
)
WHERE "IssueTypeId" IS NULL;

-- انتقال Status
UPDATE "SoftwareIssues"
SET "StatusId" = (
    SELECT "Id" FROM "Statuses" 
    WHERE "Statuses"."Value" = "SoftwareIssues"."Status" 
    LIMIT 1
)
WHERE "StatusId" IS NULL;

-- انتقال Reporter
UPDATE "SoftwareIssues"
SET "ReporterId" = (
    SELECT "Id" FROM "Users" 
    WHERE "Users"."FirstName" = "SoftwareIssues"."ReporterFirstName" 
    AND "Users"."LastName" = "SoftwareIssues"."ReporterLastName"
    LIMIT 1
)
WHERE "ReporterId" IS NULL;

-- انتقال AssignedTo
UPDATE "SoftwareIssues"
SET "AssignedToId" = (
    SELECT "Id" FROM "Users" 
    WHERE "Users"."FirstName" = "SoftwareIssues"."AssignedToFirstName" 
    AND "Users"."LastName" = "SoftwareIssues"."AssignedToLastName"
    LIMIT 1
)
WHERE "AssignedToId" IS NULL 
AND "AssignedToFirstName" IS NOT NULL 
AND "AssignedToLastName" IS NOT NULL;

-- Step 5: تنظیم مقادیر پیش‌فرض برای رکوردهایی که نتوانستند migrate شوند
UPDATE "SoftwareIssues"
SET "SoftwareNameId" = (SELECT "Id" FROM "SoftwareNames" LIMIT 1)
WHERE "SoftwareNameId" IS NULL;

UPDATE "SoftwareIssues"
SET "IssueTypeId" = (SELECT "Id" FROM "IssueTypes" LIMIT 1)
WHERE "IssueTypeId" IS NULL;

UPDATE "SoftwareIssues"
SET "StatusId" = (SELECT "Id" FROM "Statuses" WHERE "Value" = 'ثبت شده' LIMIT 1)
WHERE "StatusId" IS NULL;

UPDATE "SoftwareIssues"
SET "ReporterId" = (SELECT "Id" FROM "Users" LIMIT 1)
WHERE "ReporterId" IS NULL;

-- Step 6: ایجاد ایندکس‌ها
CREATE INDEX IF NOT EXISTS "IX_SoftwareIssues_AssignedToId" ON "SoftwareIssues" ("AssignedToId");
CREATE INDEX IF NOT EXISTS "IX_SoftwareIssues_IssueTypeId" ON "SoftwareIssues" ("IssueTypeId");
CREATE INDEX IF NOT EXISTS "IX_SoftwareIssues_ReporterId" ON "SoftwareIssues" ("ReporterId");
CREATE INDEX IF NOT EXISTS "IX_SoftwareIssues_SoftwareNameId" ON "SoftwareIssues" ("SoftwareNameId");
CREATE INDEX IF NOT EXISTS "IX_SoftwareIssues_StatusId" ON "SoftwareIssues" ("StatusId");

-- توجه: در SQLite نمی‌توان فیلدها را به NOT NULL تبدیل کرد یا فیلدهای قدیمی را حذف کرد
-- این کارها باید به صورت دستی انجام شوند یا از یک جدول جدید استفاده شود
-- برای حذف فیلدهای قدیمی، می‌توان از روش زیر استفاده کرد:
-- 1. ایجاد جدول جدید با ساختار جدید
-- 2. کپی داده‌ها
-- 3. حذف جدول قدیمی
-- 4. تغییر نام جدول جدید

-- فیلدهای قدیمی که باید حذف شوند (به صورت دستی):
-- "AssignedToFirstName", "AssignedToLastName", "IssueType", 
-- "ReporterFirstName", "ReporterLastName", "SoftwareName", "Status"



