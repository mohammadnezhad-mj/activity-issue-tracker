-- Migration: 20251228144831_AddConfigTables
-- Description: ایجاد جداول پیکربندی (ActivityTypes, IssueTypes, Results, SoftwareNames, Users)
-- Date: 2025-12-28

-- ایجاد جدول ActivityTypes
CREATE TABLE IF NOT EXISTS "ActivityTypes" (
    "Id" INTEGER NOT NULL CONSTRAINT "PK_ActivityTypes" PRIMARY KEY AUTOINCREMENT,
    "Value" TEXT NOT NULL,
    "IsActive" INTEGER NOT NULL,
    "CreatedAt" TEXT NOT NULL,
    "UpdatedAt" TEXT
);

-- ایجاد جدول IssueTypes
CREATE TABLE IF NOT EXISTS "IssueTypes" (
    "Id" INTEGER NOT NULL CONSTRAINT "PK_IssueTypes" PRIMARY KEY AUTOINCREMENT,
    "Value" TEXT NOT NULL,
    "IsActive" INTEGER NOT NULL,
    "CreatedAt" TEXT NOT NULL,
    "UpdatedAt" TEXT
);

-- ایجاد جدول Results
CREATE TABLE IF NOT EXISTS "Results" (
    "Id" INTEGER NOT NULL CONSTRAINT "PK_Results" PRIMARY KEY AUTOINCREMENT,
    "Value" TEXT NOT NULL,
    "IsActive" INTEGER NOT NULL,
    "CreatedAt" TEXT NOT NULL,
    "UpdatedAt" TEXT
);

-- ایجاد جدول SoftwareNames
CREATE TABLE IF NOT EXISTS "SoftwareNames" (
    "Id" INTEGER NOT NULL CONSTRAINT "PK_SoftwareNames" PRIMARY KEY AUTOINCREMENT,
    "Value" TEXT NOT NULL,
    "IsActive" INTEGER NOT NULL,
    "CreatedAt" TEXT NOT NULL,
    "UpdatedAt" TEXT
);

-- ایجاد جدول Users
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

-- ایجاد ایندکس‌های یکتا
CREATE UNIQUE INDEX IF NOT EXISTS "IX_ActivityTypes_Value" ON "ActivityTypes" ("Value");
CREATE UNIQUE INDEX IF NOT EXISTS "IX_IssueTypes_Value" ON "IssueTypes" ("Value");
CREATE UNIQUE INDEX IF NOT EXISTS "IX_Results_Value" ON "Results" ("Value");
CREATE UNIQUE INDEX IF NOT EXISTS "IX_SoftwareNames_Value" ON "SoftwareNames" ("Value");
CREATE UNIQUE INDEX IF NOT EXISTS "IX_Users_FirstName_LastName" ON "Users" ("FirstName", "LastName");



