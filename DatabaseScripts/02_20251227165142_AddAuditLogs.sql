-- Migration: 20251227165142_AddAuditLogs
-- Description: ایجاد جدول AuditLogs
-- Date: 2025-12-27

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



