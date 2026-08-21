-- Migration: AddCanViewActivityListPermission
-- Description: Add permission for viewing the user's activity list

ALTER TABLE "Users" ADD COLUMN "CanViewActivityList" INTEGER NOT NULL DEFAULT 1;
