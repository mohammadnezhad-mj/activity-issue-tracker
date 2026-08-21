# Activity Management System - Web Edition

A complete ASP.NET Core MVC project for managing and storing activities, deployable on IIS.

## Features

- ✅ ASP.NET Core MVC web application
- ✅ Data storage in a **SQLite** database (via Entity Framework Core)
- ✅ Management of users, activity types, and results through an admin panel
- ✅ Responsive UI with Bootstrap
- ✅ Input validation
- ✅ RTL (right-to-left) support for Persian

## Database

This project uses **SQLite** (not a JSON file). The default database file path is `Data/rayantask.db`, and it is automatically created and migrated on the first run of the application (`dbContext.Database.Migrate()` in [Program.cs](Program.cs)); there's no need to run SQL scripts manually, except for special cases (documented in [DatabaseScripts/README.md](DatabaseScripts/README.md)).

You can override the connection path/type by setting `ConnectionStrings:DefaultConnection` in `appsettings.json` or via an environment variable:

```bash
ConnectionStrings__DefaultConnection="Data Source=Data/rayantask.db"
```

## Data Fields

1. **First and Last Name**: loaded from the `Users` table
2. **Date**: defaults to today's date
3. **Activity Type**: loaded from the `ActivityTypes` table
4. **Description**: up to 1000 characters
5. **Duration**: in minutes
6. **Beneficiary / Client**: text input
7. **Result**: loaded from the `Results` table

## How to Run

### Local Development

```bash
dotnet restore
dotnet run
```

Then navigate to `http://localhost:5000`.

### Publishing to IIS

1. **Publish the project:**
   ```bash
   dotnet publish -c Release -o ./publish
   ```

2. **Install the ASP.NET Core Hosting Bundle:**
   - Download and install it from [microsoft.com](https://dotnet.microsoft.com/download/dotnet/8.0)
   - It includes the .NET Runtime and the ASP.NET Core Module for IIS

3. **Configure IIS:**
   - Open IIS Manager
   - Create a new Application Pool:
     - .NET CLR Version: No Managed Code
     - Managed Pipeline Mode: Integrated
   - Create a new Website or Application:
     - Physical Path: the path to the `publish` folder
     - Application Pool: the Application Pool you just created
     - Binding: the desired port and hostname

4. **Security settings:**
   - Make sure IIS_IUSRS and the Application Pool Identity have read/write access to the `Data` folder

5. **Test:**
   - Navigate to the address configured in IIS

## Project Structure

```
RayanTask/
├── Controllers/          # MVC controllers
├── Models/              # Data models
├── Services/            # Business logic services
├── Views/               # Razor pages
├── wwwroot/             # Static files
├── Data/                # SQLite database file (rayantask.db)
├── Migrations/          # EF Core migrations
├── DatabaseScripts/     # Raw SQL version of the same migrations (for manual execution)
├── Properties/          # Project settings
├── Program.cs           # Application entry point
└── web.config           # IIS configuration
```

## Editing Settings (Names, Activity Types, Results)

Names, activity types, and results are no longer read from a JSON file — they are managed in the database through the admin panel (`/Admin`). To access the admin panel you need a user with `IsAdmin = true` — see the "Creating the First Admin User" section below.

## Requirements

- .NET 8.0 SDK
- IIS 10 or later (for running on IIS)
- ASP.NET Core Hosting Bundle 8.0

## Local Configuration Setup

Configuration files and real data (including tokens) are not stored in git. Before running:

```bash
cp appsettings.example.json appsettings.json
cp Data/config.example.json Data/config.json
```

Then enter the real values (e.g. `Telegram:BotToken` and `Telegram:ChatId`) in `appsettings.json`. These values can also be set via environment variables, for example:

```bash
Telegram__BotToken=your-token
Telegram__ChatId=your-chat-id
```

## Creating the First Admin User

The system has no public registration page; users can only be created by an admin from within the panel. Therefore, the **first** admin user must be created directly in the SQLite database.

Passwords are stored in the `Users` table as `SHA256(password + "_" + SHA256("RayanTask_{FirstName}_{LastName}_Salt2024"))` (uppercase hex output) (implemented in [Helpers/PasswordHelper.cs](Helpers/PasswordHelper.cs)). You can use the following PowerShell command to generate the same hash:

```powershell
function Get-RayanTaskPasswordHash {
    param([string]$Password, [string]$FirstName, [string]$LastName)
    $sha256 = [System.Security.Cryptography.SHA256]::Create()
    $toHex = { param($bytes) -join ($bytes | ForEach-Object { $_.ToString("X2") }) }
    $salt = & $toHex $sha256.ComputeHash([Text.Encoding]::UTF8.GetBytes("RayanTask_${FirstName}_${LastName}_Salt2024"))
    & $toHex $sha256.ComputeHash([Text.Encoding]::UTF8.GetBytes("${Password}_${salt}"))
}

Get-RayanTaskPasswordHash -Password "YourStrongPassword" -FirstName "Admin" -LastName "System"
```

Then replace `<HASH>` in the command below with the resulting hash and run it against the database file (if you've run the application at least once, `Data/rayantask.db` and the `Users` table have already been created automatically by Migrate):

```bash
sqlite3 Data/rayantask.db "INSERT INTO Users
  (FirstName, LastName, Password, IsAdmin, CanEdit, CanDelete, CanSelectAnyDate,
   IsActive, CanManageUsers, CanManageActivityTypes, CanManageResults, CanViewReports,
   CanViewLogs, CanViewIssueDetails, CanEditIssues, CanDeleteIssues, CanDeleteReports,
   CanViewActivityList, CreatedAt)
VALUES
  ('Admin', 'System', '<HASH>', 1, 1, 1, 1,
   1, 1, 1, 1, 1,
   1, 1, 1, 1, 1,
   1, datetime('now'));"
```

After this, log in with the first/last name `Admin System` and the password you chose for the hash; from there you can create the rest of the users from the admin panel.

## Contributing

This project is open source and welcomes forks and pull requests. Please open an Issue before submitting large changes so we can align on the approach.

## License

This project is released under the [MIT License](LICENSE).
