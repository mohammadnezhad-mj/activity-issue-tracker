-- Migration: 20251229090103_AddCustomerNameToSoftwareIssues
-- Description: اضافه کردن فیلد CustomerName به جدول SoftwareIssues
-- Date: 2025-12-29

ALTER TABLE "SoftwareIssues" ADD COLUMN "CustomerName" TEXT;



