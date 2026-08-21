-- Migration: 20251228133317_AddIssueAttachments
-- Description: اضافه کردن فیلد IssueType و ایجاد جداول IssueAttachments و IssueComments
-- Date: 2025-12-28

-- اضافه کردن فیلد IssueType به جدول SoftwareIssues
ALTER TABLE "SoftwareIssues" ADD COLUMN "IssueType" TEXT NOT NULL DEFAULT '';

-- ایجاد جدول IssueAttachments
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

-- ایجاد جدول IssueComments
CREATE TABLE IF NOT EXISTS "IssueComments" (
    "Id" INTEGER NOT NULL CONSTRAINT "PK_IssueComments" PRIMARY KEY AUTOINCREMENT,
    "IssueId" INTEGER NOT NULL,
    "CommentText" TEXT NOT NULL,
    "CommenterFirstName" TEXT NOT NULL,
    "CommenterLastName" TEXT NOT NULL,
    "CommentedAt" TEXT NOT NULL
);

-- ایجاد ایندکس برای IssueAttachments
CREATE INDEX IF NOT EXISTS "IX_IssueAttachments_IssueId" ON "IssueAttachments" ("IssueId");



