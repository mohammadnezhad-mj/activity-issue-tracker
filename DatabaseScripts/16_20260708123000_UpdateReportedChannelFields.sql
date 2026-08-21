-- Migration: UpdateReportedChannelFields
-- Description: Remove SoftwareVersion/ModuleName and add ReportedChannelUrl to SoftwareIssues

PRAGMA foreign_keys=off;

CREATE TABLE "SoftwareIssues_new" (
    "Id" INTEGER NOT NULL CONSTRAINT "PK_SoftwareIssues" PRIMARY KEY AUTOINCREMENT,
    "Title" TEXT NOT NULL,
    "Description" TEXT NOT NULL,
    "SoftwareNameId" INTEGER NOT NULL,
    "IssueTypeId" INTEGER NOT NULL,
    "ReporterId" INTEGER NOT NULL,
    "AssignedToId" INTEGER NULL,
    "StatusId" INTEGER NOT NULL,
    "CustomerName" TEXT NULL,
    "BranchName" TEXT NULL,
    "RepositoryUrl" TEXT NULL,
    "CommitUrl" TEXT NULL,
    "WorkItemType" TEXT NULL,
    "GitStatus" TEXT NULL,
    "Priority" TEXT NULL,
    "Severity" TEXT NULL,
    "DueDate" TEXT NULL,
    "Environment" TEXT NULL,
    "ReportedChannel" TEXT NULL,
    "ReportedChannelUrl" TEXT NULL,
    "CreatedAt" TEXT NOT NULL,
    "StartedAt" TEXT NULL,
    "CompletedAt" TEXT NULL,
    CONSTRAINT "FK_SoftwareIssues_SoftwareNames_SoftwareNameId" FOREIGN KEY ("SoftwareNameId") REFERENCES "SoftwareNames" ("Id") ON DELETE RESTRICT,
    CONSTRAINT "FK_SoftwareIssues_IssueTypes_IssueTypeId" FOREIGN KEY ("IssueTypeId") REFERENCES "IssueTypes" ("Id") ON DELETE RESTRICT,
    CONSTRAINT "FK_SoftwareIssues_Users_ReporterId" FOREIGN KEY ("ReporterId") REFERENCES "Users" ("Id") ON DELETE RESTRICT,
    CONSTRAINT "FK_SoftwareIssues_Users_AssignedToId" FOREIGN KEY ("AssignedToId") REFERENCES "Users" ("Id") ON DELETE RESTRICT,
    CONSTRAINT "FK_SoftwareIssues_Statuses_StatusId" FOREIGN KEY ("StatusId") REFERENCES "Statuses" ("Id") ON DELETE RESTRICT
);

INSERT INTO "SoftwareIssues_new" (
    "Id", "Title", "Description", "SoftwareNameId", "IssueTypeId", "ReporterId", "AssignedToId", "StatusId",
    "CustomerName", "BranchName", "RepositoryUrl", "CommitUrl", "WorkItemType", "GitStatus", "Priority", "Severity",
    "DueDate", "Environment", "ReportedChannel", "ReportedChannelUrl", "CreatedAt", "StartedAt", "CompletedAt"
)
SELECT
    "Id", "Title", "Description", "SoftwareNameId", "IssueTypeId", "ReporterId", "AssignedToId", "StatusId",
    "CustomerName", "BranchName", "RepositoryUrl", "CommitUrl", "WorkItemType", "GitStatus", "Priority", "Severity",
    "DueDate", "Environment", "ReportedChannel", NULL, "CreatedAt", "StartedAt", "CompletedAt"
FROM "SoftwareIssues";

DROP TABLE "SoftwareIssues";
ALTER TABLE "SoftwareIssues_new" RENAME TO "SoftwareIssues";

CREATE INDEX "IX_SoftwareIssues_AssignedToId" ON "SoftwareIssues" ("AssignedToId");
CREATE INDEX "IX_SoftwareIssues_IssueTypeId" ON "SoftwareIssues" ("IssueTypeId");
CREATE INDEX "IX_SoftwareIssues_ReporterId" ON "SoftwareIssues" ("ReporterId");
CREATE INDEX "IX_SoftwareIssues_SoftwareNameId" ON "SoftwareIssues" ("SoftwareNameId");
CREATE INDEX "IX_SoftwareIssues_StatusId" ON "SoftwareIssues" ("StatusId");

PRAGMA foreign_keys=on;
