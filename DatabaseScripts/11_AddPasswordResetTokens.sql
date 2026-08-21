-- Migration 11: AddPasswordResetTokens
-- جدول توکن‌های بازیابی رمز عبور
-- Date: 2026-02-02

CREATE TABLE IF NOT EXISTS "PasswordResetTokens" (
    "Id" INTEGER NOT NULL CONSTRAINT "PK_PasswordResetTokens" PRIMARY KEY AUTOINCREMENT,
    "UserId" INTEGER NOT NULL,
    "Token" TEXT NOT NULL,
    "ExpiresAt" TEXT NOT NULL,
    "CreatedAt" TEXT NOT NULL,
    CONSTRAINT "FK_PasswordResetTokens_Users_UserId" FOREIGN KEY ("UserId") REFERENCES "Users" ("Id") ON DELETE CASCADE
);

CREATE UNIQUE INDEX IF NOT EXISTS "IX_PasswordResetTokens_Token" ON "PasswordResetTokens" ("Token");
CREATE INDEX IF NOT EXISTS "IX_PasswordResetTokens_UserId" ON "PasswordResetTokens" ("UserId");

INSERT OR IGNORE INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion") VALUES ('20260202152345_AddPasswordResetTokens', '8.0.0');
