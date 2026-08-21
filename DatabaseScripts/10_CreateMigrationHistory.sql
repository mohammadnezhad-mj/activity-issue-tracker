-- Create Migration History Table
-- این Script برای ثبت تاریخچه Migrationها استفاده می‌شود
-- Date: 2025-12-29

CREATE TABLE IF NOT EXISTS "__EFMigrationsHistory" (
    "MigrationId" TEXT NOT NULL CONSTRAINT "PK___EFMigrationsHistory" PRIMARY KEY,
    "ProductVersion" TEXT NOT NULL
);

-- ثبت تمام Migrationهای اعمال شده
INSERT OR IGNORE INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion") VALUES
('20251227142222_InitialCreate', '8.0.0'),
('20251227165142_AddAuditLogs', '8.0.0'),
('20251228090631_AddSoftwareIssues', '8.0.0'),
('20251228133317_AddIssueAttachments', '8.0.0'),
('20251228141756_AddCommentAttachments', '8.0.0'),
('20251228144831_AddConfigTables', '8.0.0'),
('20251228165547_ConvertToRelations', '8.0.0'),
('20251229080628_ConvertSoftwareIssuesToRelations', '8.0.0'),
('20251229090103_AddCustomerNameToSoftwareIssues', '8.0.0');



