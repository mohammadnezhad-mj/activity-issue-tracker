<#
.SYNOPSIS
    Create or reset a default Admin user in the RayanTask database.

.DESCRIPTION
    This script writes directly to the app's SQLite database file
    (Data\rayantask.db) and creates a user with full administrative
    access (or, if the user already exists, resets its password and
    permissions). The password is hashed using the exact same algorithm
    as Helpers\PasswordHelper.cs (SHA256 + salt derived from the user's
    database Id), so the app can accept it directly.

.PARAMETER FirstName
    First name of the admin user. Default: admin

.PARAMETER LastName
    Last name of the admin user. Default: system

.PARAMETER Password
    Password for the admin user. If not provided, a random password is
    generated and displayed.

.PARAMETER DbPath
    Path to the SQLite database file. Default: Data\rayantask.db under
    the project root.

.EXAMPLE
    .\Scripts\create-admin-user.ps1 -FirstName "John" -LastName "Doe" -Password "P@ssw0rd123"

.EXAMPLE
    .\Scripts\create-admin-user.ps1
    (uses the default names and a randomly generated password)
#>

param(
    [string]$FirstName = "admin",
    [string]$LastName = "system",
    [string]$Password = "",
    [string]$DbPath = ""
)

$ErrorActionPreference = "Stop"

# Project root = parent folder of the Scripts folder
$projectRoot = Split-Path -Parent $PSScriptRoot
if ([string]::IsNullOrWhiteSpace($DbPath)) {
    $DbPath = Join-Path $projectRoot "Data\rayantask.db"
}

if (-not (Test-Path $DbPath)) {
    Write-Error "Database file not found at '$DbPath'. Run the app once (dotnet run) first so the database and tables get created, or specify the correct path with -DbPath."
    exit 1
}

$sqlite3 = Get-Command sqlite3 -ErrorAction SilentlyContinue
if (-not $sqlite3) {
    Write-Error "The 'sqlite3' command-line tool was not found. Install it and add it to PATH (winget install sqlite.sqlite)."
    exit 1
}

if ([string]::IsNullOrWhiteSpace($Password)) {
    # Generate a secure random password when none was provided
    $bytes = New-Object byte[] 12
    [System.Security.Cryptography.RandomNumberGenerator]::Fill($bytes)
    $Password = [Convert]::ToBase64String($bytes) -replace '[/+=]', 'x'
    Write-Host "No password provided; a random password was generated." -ForegroundColor Yellow
}

function Get-Sha256Hex([string]$text) {
    $sha256 = [System.Security.Cryptography.SHA256]::Create()
    try {
        $bytes = [System.Text.Encoding]::UTF8.GetBytes($text)
        $hashBytes = $sha256.ComputeHash($bytes)
        return ([System.BitConverter]::ToString($hashBytes) -replace '-', '')
    }
    finally {
        $sha256.Dispose()
    }
}

# Exactly mirrors PasswordHelper.HashPassword in Helpers\PasswordHelper.cs.
# The salt is derived from the user's database Id (NOT their name), so a
# later name change never invalidates the password.
function Get-HashedPassword([string]$password, [int]$userId) {
    $saltInput = "RayanTask_User${userId}_Salt2024"
    $salt = Get-Sha256Hex $saltInput
    $saltedPassword = "${password}_${salt}"
    return Get-Sha256Hex $saltedPassword
}

function Escape-Sql([string]$value) {
    return $value -replace "'", "''"
}

$firstNameEsc = Escape-Sql $FirstName
$lastNameEsc = Escape-Sql $LastName

# Check whether a user with this first/last name already exists
$existingId = & $sqlite3.Source $DbPath "SELECT Id FROM Users WHERE FirstName = '$firstNameEsc' AND LastName = '$lastNameEsc' LIMIT 1;"

if ($LASTEXITCODE -ne 0) {
    Write-Error "Failed to read from the database. Make sure the migrations have been applied."
    exit 1
}

if (-not [string]::IsNullOrWhiteSpace($existingId)) {
    Write-Host "A user named '$FirstName $LastName' already exists (Id=$existingId). Updating its password and permissions..." -ForegroundColor Cyan

    $hashedPassword = Get-HashedPassword -password $Password -userId ([int]$existingId)
    $hashEsc = Escape-Sql $hashedPassword

    $updateSql = @"
UPDATE Users SET
    Password = '$hashEsc',
    IsAdmin = 1,
    IsActive = 1,
    CanEdit = 1,
    CanDelete = 1,
    CanSelectAnyDate = 1,
    CanManageUsers = 1,
    CanManageActivityTypes = 1,
    CanManageResults = 1,
    CanViewReports = 1,
    CanViewLogs = 1,
    CanViewActivityList = 1,
    CanViewIssueDetails = 1,
    CanEditIssues = 1,
    CanDeleteIssues = 1,
    CanDeleteReports = 1,
    UpdatedAt = strftime('%Y-%m-%d %H:%M:%f', 'now')
WHERE Id = $existingId;
"@

    & $sqlite3.Source $DbPath $updateSql
    if ($LASTEXITCODE -ne 0) {
        Write-Error "Failed to update the user."
        exit 1
    }
    Write-Host "Admin user updated successfully." -ForegroundColor Green
}
else {
    Write-Host "Creating new admin user '$FirstName $LastName'..." -ForegroundColor Cyan

    # Insert first (with an empty password) so we get the auto-generated Id,
    # since the password hash's salt depends on that Id.
    $insertSql = @"
INSERT INTO Users (
    FirstName, LastName, Password, IsAdmin, IsActive,
    CanEdit, CanDelete, CanSelectAnyDate,
    CanManageUsers, CanManageActivityTypes, CanManageResults,
    CanViewReports, CanViewLogs, CanViewActivityList, CanViewIssueDetails,
    CanEditIssues, CanDeleteIssues, CanDeleteReports,
    CreatedAt
) VALUES (
    '$firstNameEsc', '$lastNameEsc', '', 1, 1,
    1, 1, 1,
    1, 1, 1,
    1, 1, 1, 1,
    1, 1, 1,
    strftime('%Y-%m-%d %H:%M:%f', 'now')
);
"@

    & $sqlite3.Source $DbPath $insertSql
    if ($LASTEXITCODE -ne 0) {
        Write-Error "Failed to create the user."
        exit 1
    }

    $newId = & $sqlite3.Source $DbPath "SELECT Id FROM Users WHERE FirstName = '$firstNameEsc' AND LastName = '$lastNameEsc' LIMIT 1;"
    if ($LASTEXITCODE -ne 0 -or [string]::IsNullOrWhiteSpace($newId)) {
        Write-Error "User was created but its Id could not be looked up, so the password could not be set. Set it manually with update-user-password.ps1."
        exit 1
    }

    $hashedPassword = Get-HashedPassword -password $Password -userId ([int]$newId)
    $hashEsc = Escape-Sql $hashedPassword
    & $sqlite3.Source $DbPath "UPDATE Users SET Password = '$hashEsc' WHERE Id = $newId;"
    if ($LASTEXITCODE -ne 0) {
        Write-Error "User was created but setting the password failed. Set it manually with update-user-password.ps1."
        exit 1
    }

    Write-Host "Admin user created successfully." -ForegroundColor Green
}

Write-Host ""
Write-Host "=============================================="
Write-Host "  Username (First / Last name): $FirstName / $LastName"
Write-Host "  Password: $Password"
Write-Host "=============================================="
Write-Host "Please change this password after your first login." -ForegroundColor Yellow

