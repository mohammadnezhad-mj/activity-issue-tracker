<#
.SYNOPSIS
    Reset failed-login / "too many requests" rate limiting for the RayanTask app.

.DESCRIPTION
    The login rate limiter (Middleware\RateLimitMiddleware.cs) is tracked
    entirely in the running app's memory (a static dictionary) — it is not
    stored in the database or in any file. Because of that, no database
    script can reset it; the only ways to clear it are:
      1) Call the admin endpoints the app already exposes for this
         (Admin/ResetRateLimit and Admin/ResetAllRateLimits), which is what
         this script automates over HTTP (default mode), or
      2) Restart the app process itself, which wipes the in-memory state.
         Use -AppPoolName for this if the app is hosted under IIS and the
         machine calling this script is ALSO the one that got blocked (so
         step 1 can't even log in).

    Mode 1 (HTTP, default): logs in as an existing admin user (one with the
    "CanViewLogs" permission, which the Rate Limits admin page requires),
    then calls ResetAllRateLimits (or ResetRateLimit for a single IP) the
    same way the "Rate Limits" admin page's buttons do, including the
    anti-forgery token.

    Mode 2 (-AppPoolName): skips HTTP entirely and restarts the given IIS
    application pool via the WebAdministration module. This clears ALL rate
    limits (and logs everyone out) and requires no admin credentials, but
    must be run on the IIS server itself with administrator rights.

.PARAMETER BaseUrl
    Base URL of the running app. Default: http://localhost:5000

.PARAMETER AdminFirstName
    First name of an existing admin user (must have CanViewLogs permission).

.PARAMETER AdminLastName
    Last name of that admin user.

.PARAMETER AdminPassword
    Password of that admin user.

.PARAMETER IpAddress
    If provided, resets the rate limit for only this IP address. If
    omitted, resets ALL rate limits (equivalent to the "Reset all" button).

.PARAMETER AppPoolName
    Name of the IIS application pool hosting the app. When given, the
    script skips the HTTP login flow and simply restarts this app pool
    (requires the WebAdministration module and admin rights). Use this
    when the machine running the script is itself the blocked IP, so the
    normal HTTP login would also return 429.

.EXAMPLE
    .\Scripts\reset-rate-limits.ps1 -AdminFirstName "admin" -AdminLastName "system" -AdminPassword "123456"
    Resets all rate limits over HTTP.

.EXAMPLE
    .\Scripts\reset-rate-limits.ps1 -AdminFirstName "admin" -AdminLastName "system" -AdminPassword "123456" -IpAddress "192.168.1.10"
    Resets the rate limit for a single IP only.

.EXAMPLE
    .\Scripts\reset-rate-limits.ps1 -BaseUrl "https://myserver.example.com" -AdminFirstName "admin" -AdminLastName "system" -AdminPassword "123456"

.EXAMPLE
    .\Scripts\reset-rate-limits.ps1 -AppPoolName "RayanTaskAppPool"
    Restarts the IIS app pool instead (use this if your own IP is also blocked).
#>

param(
    [string]$BaseUrl = "http://localhost:5000",

    [string]$AdminFirstName = "",
    [string]$AdminLastName = "",
    [string]$AdminPassword = "",

    [string]$IpAddress = "",
    [string]$AppPoolName = ""
)

$ErrorActionPreference = "Stop"

# ---- Mode 2: IIS app pool restart (no HTTP / login involved) ----
if (-not [string]::IsNullOrWhiteSpace($AppPoolName)) {
    Import-Module WebAdministration -ErrorAction Stop
    Write-Host "Restarting IIS application pool '$AppPoolName'..." -ForegroundColor Cyan
    Restart-WebAppPool -Name $AppPoolName
    Write-Host "App pool restarted. All in-memory rate limits (and active sessions) have been cleared." -ForegroundColor Green
    exit 0
}

# ---- Mode 1: HTTP, using the app's own admin endpoints ----
if ([string]::IsNullOrWhiteSpace($AdminFirstName) -or [string]::IsNullOrWhiteSpace($AdminLastName) -or [string]::IsNullOrWhiteSpace($AdminPassword)) {
    Write-Error "AdminFirstName, AdminLastName and AdminPassword are required unless -AppPoolName is used instead."
    exit 1
}

$BaseUrl = $BaseUrl.TrimEnd('/')

function Get-AntiForgeryToken([string]$html) {
    $match = [regex]::Match($html, 'name="__RequestVerificationToken"[^>]*value="([^"]+)"')
    if (-not $match.Success) {
        $match = [regex]::Match($html, 'value="([^"]+)"[^>]*name="__RequestVerificationToken"')
    }
    if (-not $match.Success) {
        throw "Could not find the anti-forgery token on the page. The app may not be reachable at '$BaseUrl', or the page markup changed."
    }
    return $match.Groups[1].Value
}

$session = $null

# 1) Load the login page to get the anti-forgery token + session cookie.
#    ASP.NET Core's default anti-forgery token is valid app-wide for this
#    session (it isn't tied to a specific form/page), so this same token
#    can be reused later for the reset POST as well.
Write-Host "Connecting to $BaseUrl/Account/Login ..." -ForegroundColor Cyan
$loginPage = Invoke-WebRequest -Uri "$BaseUrl/Account/Login" -SessionVariable session -UseBasicParsing
$token = Get-AntiForgeryToken $loginPage.Content

# 2) Submit the login form
Write-Host "Logging in as '$AdminFirstName $AdminLastName'..." -ForegroundColor Cyan
$loginBody = @{
    fullName                   = "$AdminFirstName $AdminLastName"
    password                   = $AdminPassword
    __RequestVerificationToken = $token
}
try {
    Invoke-WebRequest -Uri "$BaseUrl/Account/Login" -Method Post -Body $loginBody -WebSession $session -UseBasicParsing | Out-Null
}
catch {
    $statusCode = $_.Exception.Response.StatusCode.value__
    if ($statusCode -eq 429) {
        Write-Error "Login itself was blocked (429 Too Many Requests) - this machine's own IP is the one that's rate-limited, so it can't log in to reset it over HTTP. Run this script with -AppPoolName instead (on the IIS server), or simply wait for the time window to pass."
        exit 1
    }
    throw
}

# 3) Confirm the login actually worked and this user can reach the admin
#    area, by checking we land ON the Rate Limits page rather than being
#    redirected away (non-admins / CanViewLogs=false get bounced to
#    Home/Dashboard, and non-logged-in users get bounced to Account/Login).
$rateLimitsPage = Invoke-WebRequest -Uri "$BaseUrl/Admin/RateLimits" -WebSession $session -UseBasicParsing
if ($rateLimitsPage.BaseResponse.ResponseUri.AbsolutePath -ne "/Admin/RateLimits") {
    Write-Error "Login failed, or this user does not have access to the Rate Limits page (needs the 'CanViewLogs' permission). Check the first name, last name and password."
    exit 1
}

# 4) Reset
if ([string]::IsNullOrWhiteSpace($IpAddress)) {
    Write-Host "Resetting ALL rate limits..." -ForegroundColor Cyan
    $resetBody = @{
        __RequestVerificationToken = $token
    }
    Invoke-WebRequest -Uri "$BaseUrl/Admin/ResetAllRateLimits" -Method Post -Body $resetBody -WebSession $session -UseBasicParsing | Out-Null
    Write-Host "All rate limits have been reset." -ForegroundColor Green
}
else {
    Write-Host "Resetting rate limit for IP '$IpAddress'..." -ForegroundColor Cyan
    $resetBody = @{
        ipAddress                  = $IpAddress
        __RequestVerificationToken = $token
    }
    Invoke-WebRequest -Uri "$BaseUrl/Admin/ResetRateLimit" -Method Post -Body $resetBody -WebSession $session -UseBasicParsing | Out-Null
    Write-Host "Rate limit for IP '$IpAddress' has been reset." -ForegroundColor Green
}

