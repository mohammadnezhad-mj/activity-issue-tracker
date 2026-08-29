<#
.SYNOPSIS
    Update (reset) the password of an existing user in the RayanTask database.

.DESCRIPTION
    This script writes directly to the app's SQLite database file
    (Data\rayantask.db) and updates the Password column of an existing
    user, identified by first and last name. The password is hashed
    using the exact same algorithm as Helpers\PasswordHelper.cs
    (SHA256 + salt derived from the user's database Id), so the app can
    accept it directly. It does not touch IsAdmin or any other permission
    flag.

.PARAMETER FirstName
    First name of the user whose password should be updated. Required.

.PARAMETER LastName
    Last name of the user whose password should be updated. Required.

.PARAMETER Password
    New password for the user. If not provided, a random password is
    generated and displayed.

.PARAMETER DbPath
    Path to the SQLite database file. Default: Data\rayantask.db under
    the project root.

.EXAMPLE
    .\Scripts\update-user-password.ps1 -FirstName "John" -LastName "Doe" -Password "N3wP@ss123"

.EXAMPLE
    .\Scripts\update-user-password.ps1 -FirstName "John" -LastName "Doe"
    (generates and displays a random password)
#>

param(
    [Parameter(Mandatory = $true)]
    [string]$FirstName,

    [Parameter(Mandatory = $true)]
    [string]$LastName,

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
    Write-Error "Database file not found at '$DbPath'. Specify the correct path with -DbPath."
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

function ConvertTo-SqlEscaped([string]$value) {
    return $value -replace "'", "''"
}

$firstNameEsc = ConvertTo-SqlEscaped $FirstName
$lastNameEsc = ConvertTo-SqlEscaped $LastName

# Look up the user first, so we can give a clear error if it doesn't exist
$existingId = & $sqlite3.Source $DbPath "SELECT Id FROM Users WHERE FirstName = '$firstNameEsc' AND LastName = '$lastNameEsc' LIMIT 1;"

if ($LASTEXITCODE -ne 0) {
    Write-Error "Failed to read from the database. Make sure the migrations have been applied."
    exit 1
}

if ([string]::IsNullOrWhiteSpace($existingId)) {
    Write-Error "No user named '$FirstName $LastName' was found in the database."
    exit 1
}

$hashedPassword = Get-HashedPassword -password $Password -userId ([int]$existingId)
$hashEsc = ConvertTo-SqlEscaped $hashedPassword

Write-Host "Updating password for user '$FirstName $LastName' (Id=$existingId)..." -ForegroundColor Cyan

$updateSql = @"
UPDATE Users SET
    Password = '$hashEsc',
    UpdatedAt = strftime('%Y-%m-%d %H:%M:%f', 'now')
WHERE Id = $existingId;
"@

& $sqlite3.Source $DbPath $updateSql
if ($LASTEXITCODE -ne 0) {
    Write-Error "Failed to update the password."
    exit 1
}

Write-Host "Password updated successfully." -ForegroundColor Green
Write-Host ""
Write-Host "=============================================="
Write-Host "  Username (First / Last name): $FirstName / $LastName"
Write-Host "  New password: $Password"
Write-Host "=============================================="

