-- تمام Migrationها به ترتیب
-- این فایل شامل تمام Migrationها به ترتیب است و می‌تواند برای ایجاد دیتابیس جدید استفاده شود
-- Date: 2025-12-29

-- فعال‌سازی Foreign Keys
PRAGMA foreign_keys = ON;

-- ============================================
-- Migration 01: InitialCreate
-- ============================================
CREATE TABLE IF NOT EXISTS "ActivityRecords" (
    "Id" INTEGER NOT NULL CONSTRAINT "PK_ActivityRecords" PRIMARY KEY AUTOINCREMENT,
    "Name" TEXT NOT NULL,
    "LastName" TEXT NOT NULL,
    "Date" TEXT NOT NULL,
    "ActivityType" TEXT NOT NULL,
    "Description" TEXT NOT NULL,
    "DurationMinutes" INTEGER NOT NULL,
    "Stakeholder" TEXT NOT NULL,
    "Result" TEXT NOT NULL
);

-- ============================================
-- Migration 02: AddAuditLogs
-- ============================================
CREATE TABLE IF NOT EXISTS "AuditLogs" (
    "Id" INTEGER NOT NULL CONSTRAINT "PK_AuditLogs" PRIMARY KEY AUTOINCREMENT,
    "Timestamp" TEXT NOT NULL,
    "UserFirstName" TEXT NOT NULL,
    "UserLastName" TEXT NOT NULL,
    "Action" TEXT NOT NULL,
    "EntityType" TEXT NOT NULL,
    "EntityId" TEXT NOT NULL,
    "Description" TEXT NOT NULL,
    "OldValue" TEXT NOT NULL,
    "NewValue" TEXT NOT NULL
);

-- ============================================
-- Migration 03: AddSoftwareIssues
-- ============================================
CREATE TABLE IF NOT EXISTS "SoftwareIssues" (
    "Id" INTEGER NOT NULL CONSTRAINT "PK_SoftwareIssues" PRIMARY KEY AUTOINCREMENT,
    "Title" TEXT NOT NULL,
    "Description" TEXT NOT NULL,
    "SoftwareName" TEXT NOT NULL,
    "ReporterFirstName" TEXT NOT NULL,
    "ReporterLastName" TEXT NOT NULL,
    "AssignedToFirstName" TEXT,
    "AssignedToLastName" TEXT,
    "Status" TEXT NOT NULL,
    "CreatedAt" TEXT NOT NULL,
    "StartedAt" TEXT,
    "CompletedAt" TEXT
);

-- ============================================
-- Migration 04: AddIssueAttachments
-- ============================================
ALTER TABLE "SoftwareIssues" ADD COLUMN "IssueType" TEXT NOT NULL DEFAULT '';

CREATE TABLE IF NOT EXISTS "IssueAttachments" (
    "Id" INTEGER NOT NULL CONSTRAINT "PK_IssueAttachments" PRIMARY KEY AUTOINCREMENT,
    "IssueId" INTEGER NOT NULL,
    "FileName" TEXT NOT NULL,
    "StoredFileName" TEXT NOT NULL,
    "FilePath" TEXT NOT NULL,
    "ContentType" TEXT NOT NULL,
    "FileSize" INTEGER NOT NULL,
    "UploadedAt" TEXT NOT NULL,
    "UploadedByFirstName" TEXT NOT NULL,
    "UploadedByLastName" TEXT NOT NULL,
    CONSTRAINT "FK_IssueAttachments_SoftwareIssues_IssueId" FOREIGN KEY ("IssueId") REFERENCES "SoftwareIssues" ("Id") ON DELETE CASCADE
);

CREATE TABLE IF NOT EXISTS "IssueComments" (
    "Id" INTEGER NOT NULL CONSTRAINT "PK_IssueComments" PRIMARY KEY AUTOINCREMENT,
    "IssueId" INTEGER NOT NULL,
    "CommentText" TEXT NOT NULL,
    "CommenterFirstName" TEXT NOT NULL,
    "CommenterLastName" TEXT NOT NULL,
    "CommentedAt" TEXT NOT NULL
);

CREATE INDEX IF NOT EXISTS "IX_IssueAttachments_IssueId" ON "IssueAttachments" ("IssueId");

-- ============================================
-- Migration 05: AddCommentAttachments
-- ============================================
CREATE TABLE IF NOT EXISTS "CommentAttachments" (
    "Id" INTEGER NOT NULL CONSTRAINT "PK_CommentAttachments" PRIMARY KEY AUTOINCREMENT,
    "CommentId" INTEGER NOT NULL,
    "FileName" TEXT NOT NULL,
    "StoredFileName" TEXT NOT NULL,
    "FilePath" TEXT NOT NULL,
    "ContentType" TEXT NOT NULL,
    "FileSize" INTEGER NOT NULL,
    "UploadedAt" TEXT NOT NULL,
    "UploadedByFirstName" TEXT NOT NULL,
    "UploadedByLastName" TEXT NOT NULL,
    CONSTRAINT "FK_CommentAttachments_IssueComments_CommentId" FOREIGN KEY ("CommentId") REFERENCES "IssueComments" ("Id") ON DELETE CASCADE
);

CREATE INDEX IF NOT EXISTS "IX_CommentAttachments_CommentId" ON "CommentAttachments" ("CommentId");

-- ============================================
-- Migration 06: AddConfigTables
-- ============================================
CREATE TABLE IF NOT EXISTS "ActivityTypes" (
    "Id" INTEGER NOT NULL CONSTRAINT "PK_ActivityTypes" PRIMARY KEY AUTOINCREMENT,
    "Value" TEXT NOT NULL,
    "IsActive" INTEGER NOT NULL,
    "CreatedAt" TEXT NOT NULL,
    "UpdatedAt" TEXT
);

CREATE TABLE IF NOT EXISTS "IssueTypes" (
    "Id" INTEGER NOT NULL CONSTRAINT "PK_IssueTypes" PRIMARY KEY AUTOINCREMENT,
    "Value" TEXT NOT NULL,
    "IsActive" INTEGER NOT NULL,
    "CreatedAt" TEXT NOT NULL,
    "UpdatedAt" TEXT
);

CREATE TABLE IF NOT EXISTS "Results" (
    "Id" INTEGER NOT NULL CONSTRAINT "PK_Results" PRIMARY KEY AUTOINCREMENT,
    "Value" TEXT NOT NULL,
    "IsActive" INTEGER NOT NULL,
    "CreatedAt" TEXT NOT NULL,
    "UpdatedAt" TEXT
);

CREATE TABLE IF NOT EXISTS "SoftwareNames" (
    "Id" INTEGER NOT NULL CONSTRAINT "PK_SoftwareNames" PRIMARY KEY AUTOINCREMENT,
    "Value" TEXT NOT NULL,
    "IsActive" INTEGER NOT NULL,
    "CreatedAt" TEXT NOT NULL,
    "UpdatedAt" TEXT
);

CREATE TABLE IF NOT EXISTS "Users" (
    "Id" INTEGER NOT NULL CONSTRAINT "PK_Users" PRIMARY KEY AUTOINCREMENT,
    "FirstName" TEXT NOT NULL,
    "LastName" TEXT NOT NULL,
    "Password" TEXT NOT NULL,
    "IsAdmin" INTEGER NOT NULL,
    "CanEdit" INTEGER NOT NULL,
    "CanDelete" INTEGER NOT NULL,
    "CanSelectAnyDate" INTEGER NOT NULL,
    "IsActive" INTEGER NOT NULL,
    "CanManageUsers" INTEGER NOT NULL,
    "CanManageActivityTypes" INTEGER NOT NULL,
    "CanManageResults" INTEGER NOT NULL,
    "CanViewReports" INTEGER NOT NULL,
    "CanViewLogs" INTEGER NOT NULL,
    "CanViewIssueDetails" INTEGER NOT NULL,
    "CanEditIssues" INTEGER NOT NULL,
    "CanDeleteIssues" INTEGER NOT NULL,
    "CanDeleteReports" INTEGER NOT NULL,
    "CreatedAt" TEXT NOT NULL,
    "UpdatedAt" TEXT
);

CREATE UNIQUE INDEX IF NOT EXISTS "IX_ActivityTypes_Value" ON "ActivityTypes" ("Value");
CREATE UNIQUE INDEX IF NOT EXISTS "IX_IssueTypes_Value" ON "IssueTypes" ("Value");
CREATE UNIQUE INDEX IF NOT EXISTS "IX_Results_Value" ON "Results" ("Value");
CREATE UNIQUE INDEX IF NOT EXISTS "IX_SoftwareNames_Value" ON "SoftwareNames" ("Value");
CREATE UNIQUE INDEX IF NOT EXISTS "IX_Users_FirstName_LastName" ON "Users" ("FirstName", "LastName");

-- ============================================
-- Migration 07: ConvertToRelations
-- ============================================
ALTER TABLE "ActivityRecords" ADD COLUMN "ResultId" INTEGER;
ALTER TABLE "ActivityRecords" ADD COLUMN "UserId" INTEGER;

CREATE TABLE IF NOT EXISTS "ActivityRecordActivityType" (
    "ActivityRecordId" INTEGER NOT NULL,
    "ActivityTypeId" INTEGER NOT NULL,
    CONSTRAINT "PK_ActivityRecordActivityType" PRIMARY KEY ("ActivityRecordId", "ActivityTypeId"),
    CONSTRAINT "FK_ActivityRecordActivityType_ActivityRecords_ActivityRecordId" FOREIGN KEY ("ActivityRecordId") REFERENCES "ActivityRecords" ("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_ActivityRecordActivityType_ActivityTypes_ActivityTypeId" FOREIGN KEY ("ActivityTypeId") REFERENCES "ActivityTypes" ("Id") ON DELETE CASCADE
);

UPDATE "ActivityRecords"
SET "UserId" = (
    SELECT "Id" FROM "Users" 
    WHERE "Users"."FirstName" = "ActivityRecords"."Name" 
    AND "Users"."LastName" = "ActivityRecords"."LastName"
    LIMIT 1
)
WHERE "UserId" IS NULL;

UPDATE "ActivityRecords"
SET "ResultId" = (
    SELECT "Id" FROM "Results" 
    WHERE "Results"."Value" = "ActivityRecords"."Result"
    LIMIT 1
)
WHERE "ResultId" IS NULL;

INSERT INTO "ActivityRecordActivityType" ("ActivityRecordId", "ActivityTypeId")
SELECT ar."Id", at."Id"
FROM "ActivityRecords" ar
CROSS JOIN "ActivityTypes" at
WHERE at."Value" = ar."ActivityType"
AND NOT EXISTS (
    SELECT 1 FROM "ActivityRecordActivityType" arat
    WHERE arat."ActivityRecordId" = ar."Id" AND arat."ActivityTypeId" = at."Id"
);

UPDATE "ActivityRecords"
SET "UserId" = (SELECT "Id" FROM "Users" LIMIT 1)
WHERE "UserId" IS NULL;

UPDATE "ActivityRecords"
SET "ResultId" = (SELECT "Id" FROM "Results" LIMIT 1)
WHERE "ResultId" IS NULL;

CREATE INDEX IF NOT EXISTS "IX_ActivityRecords_ResultId" ON "ActivityRecords" ("ResultId");
CREATE INDEX IF NOT EXISTS "IX_ActivityRecords_UserId" ON "ActivityRecords" ("UserId");
CREATE INDEX IF NOT EXISTS "IX_ActivityRecordActivityType_ActivityTypeId" ON "ActivityRecordActivityType" ("ActivityTypeId");

-- ============================================
-- Migration 08: ConvertSoftwareIssuesToRelations
-- ============================================
ALTER TABLE "SoftwareIssues" ADD COLUMN "AssignedToId" INTEGER;
ALTER TABLE "SoftwareIssues" ADD COLUMN "IssueTypeId" INTEGER;
ALTER TABLE "SoftwareIssues" ADD COLUMN "ReporterId" INTEGER;
ALTER TABLE "SoftwareIssues" ADD COLUMN "SoftwareNameId" INTEGER;
ALTER TABLE "SoftwareIssues" ADD COLUMN "StatusId" INTEGER;

CREATE TABLE IF NOT EXISTS "Statuses" (
    "Id" INTEGER NOT NULL CONSTRAINT "PK_Statuses" PRIMARY KEY AUTOINCREMENT,
    "Value" TEXT NOT NULL,
    "IsActive" INTEGER NOT NULL,
    "CreatedAt" TEXT NOT NULL,
    "UpdatedAt" TEXT
);

CREATE UNIQUE INDEX IF NOT EXISTS "IX_Statuses_Value" ON "Statuses" ("Value");

INSERT OR IGNORE INTO "Statuses" ("Value", "IsActive", "CreatedAt")
VALUES 
    ('ثبت شده', 1, datetime('now')),
    ('در حال بررسی', 1, datetime('now')),
    ('در حال انجام', 1, datetime('now')),
    ('تکمیل شده', 1, datetime('now')),
    ('لغو شده', 1, datetime('now'));

UPDATE "SoftwareIssues"
SET "SoftwareNameId" = (
    SELECT "Id" FROM "SoftwareNames" 
    WHERE "SoftwareNames"."Value" = "SoftwareIssues"."SoftwareName" 
    LIMIT 1
)
WHERE "SoftwareNameId" IS NULL;

UPDATE "SoftwareIssues"
SET "IssueTypeId" = (
    SELECT "Id" FROM "IssueTypes" 
    WHERE "IssueTypes"."Value" = "SoftwareIssues"."IssueType" 
    LIMIT 1
)
WHERE "IssueTypeId" IS NULL;

UPDATE "SoftwareIssues"
SET "StatusId" = (
    SELECT "Id" FROM "Statuses" 
    WHERE "Statuses"."Value" = "SoftwareIssues"."Status" 
    LIMIT 1
)
WHERE "StatusId" IS NULL;

UPDATE "SoftwareIssues"
SET "ReporterId" = (
    SELECT "Id" FROM "Users" 
    WHERE "Users"."FirstName" = "SoftwareIssues"."ReporterFirstName" 
    AND "Users"."LastName" = "SoftwareIssues"."ReporterLastName"
    LIMIT 1
)
WHERE "ReporterId" IS NULL;

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

CREATE INDEX IF NOT EXISTS "IX_SoftwareIssues_AssignedToId" ON "SoftwareIssues" ("AssignedToId");
CREATE INDEX IF NOT EXISTS "IX_SoftwareIssues_IssueTypeId" ON "SoftwareIssues" ("IssueTypeId");
CREATE INDEX IF NOT EXISTS "IX_SoftwareIssues_ReporterId" ON "SoftwareIssues" ("ReporterId");
CREATE INDEX IF NOT EXISTS "IX_SoftwareIssues_SoftwareNameId" ON "SoftwareIssues" ("SoftwareNameId");
CREATE INDEX IF NOT EXISTS "IX_SoftwareIssues_StatusId" ON "SoftwareIssues" ("StatusId");

-- ============================================
-- Migration 09: AddCustomerNameToSoftwareIssues
-- ============================================
ALTER TABLE "SoftwareIssues" ADD COLUMN "CustomerName" TEXT;

-- ============================================
-- Migration 11: AddPasswordResetTokens
-- ============================================
CREATE TABLE IF NOT EXISTS "PasswordResetTokens" (
    "Id" INTEGER NOT NULL CONSTRAINT "PK_PasswordResetTokens" PRIMARY KEY AUTOINCREMENT,
    "UserId" INTEGER NOT NULL,
    "Token" TEXT NOT NULL,
    "ExpiresAt" TEXT NOT NULL,
    "CreatedAt" TEXT NOT NULL,
    CONSTRAINT "FK_PasswordResetTokens_Users_UserId" FOREIGN KEY ("UserId") REFERENCES "Users" ("Id") ON DELETE CASCADE
);

CREATE UNIQUE INDEX IF NOT EXISTS "IX_PasswordResetTokens_Token" ON "PasswordResetTokens" ("Token");
CREATE INDEX IF NOT EXISTS "IX_PasswordResetTokens_UserId" ON "PasswordResetTokens" ("UserId");

-- ============================================
-- Migration 12: AddSecurityQuestionAndPasswordResetRequest
-- ============================================
ALTER TABLE "Users" ADD COLUMN "SecurityQuestion" TEXT;
ALTER TABLE "Users" ADD COLUMN "SecurityAnswerHash" TEXT;

CREATE TABLE IF NOT EXISTS "PasswordResetRequests" (
    "Id" INTEGER NOT NULL CONSTRAINT "PK_PasswordResetRequests" PRIMARY KEY AUTOINCREMENT,
    "UserId" INTEGER NOT NULL,
    "RequestToken" TEXT NOT NULL,
    "ExpiresAt" TEXT NOT NULL,
    "CreatedAt" TEXT NOT NULL,
    CONSTRAINT "FK_PasswordResetRequests_Users_UserId" FOREIGN KEY ("UserId") REFERENCES "Users" ("Id") ON DELETE CASCADE
);

CREATE UNIQUE INDEX IF NOT EXISTS "IX_PasswordResetRequests_RequestToken" ON "PasswordResetRequests" ("RequestToken");
CREATE INDEX IF NOT EXISTS "IX_PasswordResetRequests_UserId" ON "PasswordResetRequests" ("UserId");

-- ============================================
-- Create Migration History
-- ============================================
CREATE TABLE IF NOT EXISTS "__EFMigrationsHistory" (
    "MigrationId" TEXT NOT NULL CONSTRAINT "PK___EFMigrationsHistory" PRIMARY KEY,
    "ProductVersion" TEXT NOT NULL
);

INSERT OR IGNORE INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion") VALUES
('20251227142222_InitialCreate', '8.0.0'),
('20251227165142_AddAuditLogs', '8.0.0'),
('20251228090631_AddSoftwareIssues', '8.0.0'),
('20251228133317_AddIssueAttachments', '8.0.0'),
('20251228141756_AddCommentAttachments', '8.0.0'),
('20251228144831_AddConfigTables', '8.0.0'),
('20251228165547_ConvertToRelations', '8.0.0'),
('20251229080628_ConvertSoftwareIssuesToRelations', '8.0.0'),
('20251229090103_AddCustomerNameToSoftwareIssues', '8.0.0'),
('20260202152345_AddPasswordResetTokens', '8.0.0'),
('20260202153443_AddSecurityQuestionAndPasswordResetRequest', '8.0.0');



