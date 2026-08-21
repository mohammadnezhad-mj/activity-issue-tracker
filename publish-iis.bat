@echo off
chcp 65001 >nul
setlocal

:: ============================================
:: RayanTask - Publish for IIS
:: ============================================

set "PROJECT_DIR=%~dp0"
set "PUBLISH_DIR=%PROJECT_DIR%publish"

echo.
echo [1/4] پاکسازی خروجی قبلی...
if exist "%PUBLISH_DIR%" (
    rmdir /s /q "%PUBLISH_DIR%"
)

echo [2/4] Restore پکیج‌ها...
dotnet restore "%PROJECT_DIR%RayanTask.csproj"
if errorlevel 1 (
    echo خطا در Restore.
    pause
    exit /b 1
)

echo [3/4] Build در حالت Release...
dotnet build "%PROJECT_DIR%RayanTask.csproj" -c Release --no-restore
if errorlevel 1 (
    echo خطا در Build.
    pause
    exit /b 1
)

echo [4/4] Publish برای IIS...
dotnet publish "%PROJECT_DIR%RayanTask.csproj" -c Release -o "%PUBLISH_DIR%" --no-build
if errorlevel 1 (
    echo خطا در Publish.
    pause
    exit /b 1
)

echo.
echo ============================================
echo   Publish با موفقیت انجام شد.
echo   خروجی: %PUBLISH_DIR%
echo ============================================
echo.
echo برای استقرار روی IIS:
echo   1. محتوای پوشه publish را به مسیر سایت IIS کپی کنید
echo      (مثلاً C:\inetpub\wwwroot\RayanTask)
echo   2. در به‌روزرسانی: فایل‌های Data\rayantask.db و Data\config.json را نگه دارید.
echo   3. Application Pool را Recycle کنید.
echo.
pause
