-- Migration: AddGitFieldsToSoftwareIssues
-- Description: Add optional GitHub/GitLab-related fields to SoftwareIssues

ALTER TABLE "SoftwareIssues" ADD COLUMN "BranchName" TEXT NULL;
ALTER TABLE "SoftwareIssues" ADD COLUMN "RepositoryUrl" TEXT NULL;
ALTER TABLE "SoftwareIssues" ADD COLUMN "CommitUrl" TEXT NULL;
ALTER TABLE "SoftwareIssues" ADD COLUMN "WorkItemType" TEXT NULL;
ALTER TABLE "SoftwareIssues" ADD COLUMN "GitStatus" TEXT NULL;
