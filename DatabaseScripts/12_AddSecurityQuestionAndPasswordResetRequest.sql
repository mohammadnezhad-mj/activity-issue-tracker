-- Migration 12: AddSecurityQuestionAndPasswordResetRequest
-- سوال امنیتی برای کاربران و جدول درخواست بازیابی (مرحله پاسخ به سوال)
-- Date: 2026-02-02

-- سوال و جواب امنیتی در Users (برای بازیابی رمز)
ALTER TABLE "Users" ADD COLUMN "SecurityQuestion" TEXT;
ALTER TABLE "Users" ADD COLUMN "SecurityAnswerHash" TEXT;

CREATE TABLE IF NOT EXISTS "PasswordResetRequests" (
    "Id" INTEGER NOT NULL CONSTRAINT "PK_PasswordResetRequests" PRIMARY KEY AUTOINCREMENT,
    "UserId" INTEGER NOT NULL,
    "RequestToken" TEXT NOT NULL,
    "ExpiresAt" TEXT NOT NULL,
    "CreatedAt" TEXT NOT NULL,
    CONSTRAINT "FK_PasswordResetRequests_Users_UserId" FOREIGN KEY ("UserId") REFERENCES "Users" ("Id") ON DELETE CASCADE
);

CREATE UNIQUE INDEX IF NOT EXISTS "IX_PasswordResetRequests_RequestToken" ON "PasswordResetRequests" ("RequestToken");
CREATE INDEX IF NOT EXISTS "IX_PasswordResetRequests_UserId" ON "PasswordResetRequests" ("UserId");

INSERT OR IGNORE INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion") VALUES ('20260202153443_AddSecurityQuestionAndPasswordResetRequest', '8.0.0');
