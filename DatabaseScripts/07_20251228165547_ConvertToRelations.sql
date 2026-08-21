-- Migration: 20251228165547_ConvertToRelations
-- Description: تبدیل فیلدهای متنی ActivityRecords به Foreign Key
-- Date: 2025-12-28

-- Step 1: اضافه کردن فیلدهای جدید به صورت nullable
ALTER TABLE "ActivityRecords" ADD COLUMN "ResultId" INTEGER;
ALTER TABLE "ActivityRecords" ADD COLUMN "UserId" INTEGER;

-- Step 2: ایجاد جدول ارتباطی ActivityRecordActivityType
CREATE TABLE IF NOT EXISTS "ActivityRecordActivityType" (
    "ActivityRecordId" INTEGER NOT NULL,
    "ActivityTypeId" INTEGER NOT NULL,
    CONSTRAINT "PK_ActivityRecordActivityType" PRIMARY KEY ("ActivityRecordId", "ActivityTypeId"),
    CONSTRAINT "FK_ActivityRecordActivityType_ActivityRecords_ActivityRecordId" FOREIGN KEY ("ActivityRecordId") REFERENCES "ActivityRecords" ("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_ActivityRecordActivityType_ActivityTypes_ActivityTypeId" FOREIGN KEY ("ActivityTypeId") REFERENCES "ActivityTypes" ("Id") ON DELETE CASCADE
);

-- Step 3: انتقال داده‌ها از فیلدهای قدیمی به جدید
-- انتقال Name/LastName به UserId
UPDATE "ActivityRecords"
SET "UserId" = (
    SELECT "Id" FROM "Users" 
    WHERE "Users"."FirstName" = "ActivityRecords"."Name" 
    AND "Users"."LastName" = "ActivityRecords"."LastName"
    LIMIT 1
)
WHERE "UserId" IS NULL;

-- انتقال Result به ResultId
UPDATE "ActivityRecords"
SET "ResultId" = (
    SELECT "Id" FROM "Results" 
    WHERE "Results"."Value" = "ActivityRecords"."Result"
    LIMIT 1
)
WHERE "ResultId" IS NULL;

-- انتقال ActivityType به ActivityRecordActivityType
INSERT INTO "ActivityRecordActivityType" ("ActivityRecordId", "ActivityTypeId")
SELECT ar."Id", at."Id"
FROM "ActivityRecords" ar
CROSS JOIN "ActivityTypes" at
WHERE at."Value" = ar."ActivityType"
AND NOT EXISTS (
    SELECT 1 FROM "ActivityRecordActivityType" arat
    WHERE arat."ActivityRecordId" = ar."Id" AND arat."ActivityTypeId" = at."Id"
);

-- Step 4: تنظیم مقادیر پیش‌فرض برای رکوردهایی که نتوانستند migrate شوند
UPDATE "ActivityRecords"
SET "UserId" = (SELECT "Id" FROM "Users" LIMIT 1)
WHERE "UserId" IS NULL;

UPDATE "ActivityRecords"
SET "ResultId" = (SELECT "Id" FROM "Results" LIMIT 1)
WHERE "ResultId" IS NULL;

-- Step 5: حذف فیلدهای قدیمی
-- توجه: SQLite از DROP COLUMN پشتیبانی نمی‌کند، بنابراین این فیلدها باید به صورت دستی حذف شوند
-- یا از یک جدول جدید استفاده شود. برای این Migration، فیلدهای قدیمی باقی می‌مانند
-- و باید در Migration بعدی یا به صورت دستی حذف شوند.

-- ایجاد ایندکس‌ها
CREATE INDEX IF NOT EXISTS "IX_ActivityRecords_ResultId" ON "ActivityRecords" ("ResultId");
CREATE INDEX IF NOT EXISTS "IX_ActivityRecords_UserId" ON "ActivityRecords" ("UserId");
CREATE INDEX IF NOT EXISTS "IX_ActivityRecordActivityType_ActivityTypeId" ON "ActivityRecordActivityType" ("ActivityTypeId");

-- اضافه کردن Foreign Key Constraints
-- توجه: SQLite از ADD CONSTRAINT پشتیبانی محدودی دارد
-- این Foreign Keys باید در زمان ایجاد جدول تعریف شوند یا از طریق PRAGMA foreign_keys فعال شوند



