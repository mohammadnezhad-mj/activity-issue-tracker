-- Migration: AddOperationalFieldsToSoftwareIssues
-- Description: Add operational triage fields to SoftwareIssues

ALTER TABLE "SoftwareIssues" ADD COLUMN "Priority" TEXT NULL;
ALTER TABLE "SoftwareIssues" ADD COLUMN "Severity" TEXT NULL;
ALTER TABLE "SoftwareIssues" ADD COLUMN "DueDate" TEXT NULL;
ALTER TABLE "SoftwareIssues" ADD COLUMN "Environment" TEXT NULL;
ALTER TABLE "SoftwareIssues" ADD COLUMN "SoftwareVersion" TEXT NULL;
ALTER TABLE "SoftwareIssues" ADD COLUMN "ModuleName" TEXT NULL;
ALTER TABLE "SoftwareIssues" ADD COLUMN "ReportedChannel" TEXT NULL;
