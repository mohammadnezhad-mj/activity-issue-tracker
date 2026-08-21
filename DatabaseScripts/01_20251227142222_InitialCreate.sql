-- Migration: 20251227142222_InitialCreate
-- Description: ایجاد جدول ActivityRecords
-- Date: 2025-12-27

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



