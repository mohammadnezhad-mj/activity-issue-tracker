-- Migration: 20251228090631_AddSoftwareIssues
-- Description: ایجاد جدول SoftwareIssues
-- Date: 2025-12-28

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



