# اسکریپت حذف دیتابیس SQLite
Write-Host "در حال حذف فایل‌های دیتابیس..." -ForegroundColor Yellow

$dbFiles = @(
    "Data\rayantask.db",
    "Data\rayantask.db-shm",
    "Data\rayantask.db-wal",
    "publish\Data\rayantask.db",
    "publish\Data\rayantask.db-shm",
    "publish\Data\rayantask.db-wal"
)

$deleted = 0
$failed = 0

foreach ($file in $dbFiles) {
    if (Test-Path $file) {
        try {
            Remove-Item $file -Force -ErrorAction Stop
            Write-Host "✓ حذف شد: $file" -ForegroundColor Green
            $deleted++
        }
        catch {
            Write-Host "✗ خطا در حذف: $file - $($_.Exception.Message)" -ForegroundColor Red
            $failed++
        }
    }
}

Write-Host "`nخلاصه:" -ForegroundColor Cyan
Write-Host "  حذف شده: $deleted" -ForegroundColor Green
Write-Host "  خطا: $failed" -ForegroundColor $(if ($failed -gt 0) { "Red" } else { "Green" })

if ($failed -gt 0) {
    Write-Host "`nنکته: اگر خطایی رخ داد، لطفاً برنامه را متوقف کنید و دوباره اجرا کنید." -ForegroundColor Yellow
}

