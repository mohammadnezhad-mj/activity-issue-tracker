-- Migration: 20251228141756_AddCommentAttachments
-- Description: ایجاد جدول CommentAttachments
-- Date: 2025-12-28

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

-- ایجاد ایندکس برای CommentAttachments
CREATE INDEX IF NOT EXISTS "IX_CommentAttachments_CommentId" ON "CommentAttachments" ("CommentId");



