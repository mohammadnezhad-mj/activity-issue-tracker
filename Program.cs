using Microsoft.EntityFrameworkCore;
using RayanTask.Data;
using RayanTask.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

// Add SQLite Database
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
if (string.IsNullOrWhiteSpace(connectionString))
{
    // استفاده از مسیر کامل برای IIS
    var contentRoot = builder.Environment.ContentRootPath;
    var dbPath = Path.Combine(contentRoot, "Data", "rayantask.db");
    connectionString = $"Data Source={dbPath}";
}
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite(connectionString));

builder.Services.AddHttpClient();
// کلاینت مخصوص تلگرام: پشتیبانی از ApiBaseUrl (پروکسی معکوس) و پروکسی HTTP برای دسترسی از ایران
builder.Services.AddHttpClient("Telegram")
    .ConfigurePrimaryHttpMessageHandler(sp =>
    {
        var config = sp.GetRequiredService<IConfiguration>();
        var proxyUrl = config["Telegram:ProxyUrl"]?.Trim();
        if (string.IsNullOrWhiteSpace(proxyUrl))
            return new HttpClientHandler();
        var proxy = new System.Net.WebProxy(new Uri(proxyUrl));
        var user = config["Telegram:ProxyUser"]?.Trim();
        var pass = config["Telegram:ProxyPassword"];
        if (!string.IsNullOrWhiteSpace(user))
            proxy.Credentials = new System.Net.NetworkCredential(user, pass ?? "");
        return new HttpClientHandler { Proxy = proxy, UseProxy = true };
    });
builder.Services.AddScoped<DataService>();
builder.Services.AddScoped<LogService>();
builder.Services.AddScoped<ConfigMigrationService>();
builder.Services.AddScoped<ITelegramNotificationService, TelegramNotificationService>();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromHours(8);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest; // HTTP و HTTPS
    options.Cookie.SameSite = SameSiteMode.Strict; // محافظت در برابر CSRF
    options.Cookie.Name = ".RayanTask.Session"; // نام سفارشی
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler(errorApp =>
    {
        errorApp.Run(context =>
        {
            context.Response.StatusCode = 500;
            context.Response.ContentType = "text/html; charset=utf-8";

            var exceptionHandlerPathFeature = context.Features.Get<Microsoft.AspNetCore.Diagnostics.IExceptionHandlerPathFeature>();
            var exception = exceptionHandlerPathFeature?.Error;

            var logger = context.RequestServices.GetRequiredService<ILogger<Program>>();
            logger.LogError(exception, "خطای غیرمنتظره در پردازش درخواست: {Path}", context.Request.Path);

            // Redirect to Error page with exception details
            context.Response.Redirect($"/Home/Error?message={Uri.EscapeDataString(exception?.Message ?? "خطای نامشخص")}");
            return Task.CompletedTask;
        });
    });
    app.UseHsts();
}
else
{
    app.UseDeveloperExceptionPage();
}

// Security Headers Middleware
app.UseMiddleware<RayanTask.Middleware.SecurityHeadersMiddleware>();

// Rate Limiting Middleware (با دسترسی به ServiceProvider)
app.UseMiddleware<RayanTask.Middleware.RateLimitMiddleware>(app.Services);

// HTTPS Redirection (اختیاری - فقط در Development)
// در Production می‌توانید این بخش را فعال کنید اگر HTTPS دارید
// app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseSession();
app.UseAuthorization();

// Ensure database is created and migrate from JSON if needed
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    
    // بررسی وجود جدول Migration History
    try
    {
        var connection = dbContext.Database.GetDbConnection();
        await connection.OpenAsync();
        using var command = connection.CreateCommand();
        
        // بررسی وجود جدول __EFMigrationsHistory
        command.CommandText = "SELECT name FROM sqlite_master WHERE type='table' AND name='__EFMigrationsHistory';";
        var migrationTableExists = await command.ExecuteScalarAsync() != null;
        
        // بررسی وجود جدول ActivityRecords (برای تشخیص دیتابیس قدیمی)
        command.CommandText = "SELECT name FROM sqlite_master WHERE type='table' AND name='ActivityRecords';";
        var activityTableExists = await command.ExecuteScalarAsync() != null;
        
        // بررسی وجود جدول Statuses (برای تشخیص اینکه آیا Migration جدید اعمال شده است)
        command.CommandText = "SELECT name FROM sqlite_master WHERE type='table' AND name='Statuses';";
        var statusesTableExists = await command.ExecuteScalarAsync() != null;
        
        // بررسی وجود جدول SoftwareIssues با ساختار قدیمی (با فیلدهای متنی)
        command.CommandText = "SELECT sql FROM sqlite_master WHERE type='table' AND name='SoftwareIssues';";
        var softwareIssuesSqlResult = await command.ExecuteScalarAsync();
        var softwareIssuesSql = softwareIssuesSqlResult?.ToString() ?? "";
        var hasOldSoftwareIssuesStructure = softwareIssuesSql.Contains("SoftwareName TEXT") && !softwareIssuesSql.Contains("SoftwareNameId");
        
        await connection.CloseAsync();
        
        // اگر جدول ActivityRecords وجود دارد اما Migration History وجود ندارد
        // یا اگر جدول Statuses وجود ندارد (یعنی Migration جدید اعمال نشده)
        // یعنی دیتابیس قدیمی است و باید Migration History را ایجاد یا به‌روزرسانی کنیم
        if (activityTableExists && (!migrationTableExists || !statusesTableExists || hasOldSoftwareIssuesStructure))
        {
            logger.LogWarning("دیتابیس قدیمی شناسایی شد. در حال ایجاد/به‌روزرسانی جدول Migration History...");
            
            // ایجاد جدول Migration History در صورت عدم وجود
            if (!migrationTableExists)
            {
                await connection.OpenAsync();
                command.CommandText = @"
                    CREATE TABLE IF NOT EXISTS ""__EFMigrationsHistory"" (
                        ""MigrationId"" TEXT NOT NULL CONSTRAINT ""PK___EFMigrationsHistory"" PRIMARY KEY,
                        ""ProductVersion"" TEXT NOT NULL
                    );
                ";
                await command.ExecuteNonQueryAsync();
                await connection.CloseAsync();
            }
            
            // خواندن Migrationهای موجود
            await connection.OpenAsync();
            command.CommandText = "SELECT MigrationId FROM __EFMigrationsHistory;";
            var existingMigrations = new HashSet<string>();
            using (var reader = await command.ExecuteReaderAsync())
            {
                while (await reader.ReadAsync())
                {
                    existingMigrations.Add(reader.GetString(0));
                }
            }
            
            // تعیین Migrationهایی که باید ثبت شوند
            var allMigrations = new[]
            {
                ("20251227142222_InitialCreate", "8.0.0"),
                ("20251227165142_AddAuditLogs", "8.0.0"),
                ("20251228090631_AddSoftwareIssues", "8.0.0"),
                ("20251228133317_AddIssueAttachments", "8.0.0"),
                ("20251228141756_AddCommentAttachments", "8.0.0"),
                ("20251228144831_AddConfigTables", "8.0.0"),
                ("20251228165547_ConvertToRelations", "8.0.0")
            };
            
            // اگر جدول Statuses وجود ندارد، Migration جدید را اضافه نمی‌کنیم (باید اعمال شود)
            var newMigrations = new List<(string, string)>();
            if (statusesTableExists)
            {
                newMigrations.Add(("20251229080628_ConvertSoftwareIssuesToRelations", "8.0.0"));
                
                // بررسی وجود فیلد CustomerName
                command.CommandText = "PRAGMA table_info(SoftwareIssues);";
                var hasCustomerName = false;
                using (var reader = await command.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        if (reader.GetString(1) == "CustomerName")
                        {
                            hasCustomerName = true;
                            break;
                        }
                    }
                }
                
                if (hasCustomerName)
                {
                    newMigrations.Add(("20251229090103_AddCustomerNameToSoftwareIssues", "8.0.0"));
                }
            }
            
            // اضافه کردن Migrationهای موجود که ثبت نشده‌اند
            var migrationsToAdd = allMigrations.Concat(newMigrations)
                .Where(m => !existingMigrations.Contains(m.Item1))
                .ToList();
            
            foreach (var (migrationId, productVersion) in migrationsToAdd)
            {
                command.CommandText = @"
                    INSERT OR IGNORE INTO ""__EFMigrationsHistory"" (""MigrationId"", ""ProductVersion"")
                    VALUES ($migrationId, $productVersion);
                ";
                var param1 = command.CreateParameter();
                param1.ParameterName = "$migrationId";
                param1.Value = migrationId;
                command.Parameters.Add(param1);
                
                var param2 = command.CreateParameter();
                param2.ParameterName = "$productVersion";
                param2.Value = productVersion;
                command.Parameters.Add(param2);
                
                await command.ExecuteNonQueryAsync();
                command.Parameters.Clear();
            }
            
            await connection.CloseAsync();
            logger.LogInformation($"Migration History به‌روزرسانی شد. {migrationsToAdd.Count} Migration جدید ثبت شد.");
        }
        
        // اعمال Migrationهای pending
        try
        {
            dbContext.Database.Migrate();
            logger.LogInformation("Migrationها با موفقیت اعمال شدند.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "خطا در اعمال Migrationها. ممکن است دیتابیس از قبل به‌روز باشد.");
            // ادامه می‌دهیم چون ممکن است Migrationها قبلاً اعمال شده باشند
        }
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "خطا در بررسی و اعمال Migrationها");
        // ادامه می‌دهیم
    }
    
    // ایجاد جدول SoftwareIssues در صورت عدم وجود
    try
    {
        var connection = dbContext.Database.GetDbConnection();
        await connection.OpenAsync();
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT name FROM sqlite_master WHERE type='table' AND name='SoftwareIssues';";
        var result = await command.ExecuteScalarAsync();
        
        if (result == null)
        {
            // ایجاد جدول SoftwareIssues
            command.CommandText = @"
                CREATE TABLE SoftwareIssues (
                    Id INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
                    Title TEXT NOT NULL,
                    Description TEXT NOT NULL,
                    SoftwareName TEXT NOT NULL,
                    IssueType TEXT NOT NULL,
                    ReporterFirstName TEXT NOT NULL,
                    ReporterLastName TEXT NOT NULL,
                    AssignedToFirstName TEXT,
                    AssignedToLastName TEXT,
                    Status TEXT NOT NULL,
                    CreatedAt TEXT NOT NULL,
                    StartedAt TEXT,
                    CompletedAt TEXT
                );
            ";
            await command.ExecuteNonQueryAsync();
            
            // ایجاد جدول IssueComments
            command.CommandText = @"
                CREATE TABLE IssueComments (
                    Id INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
                    IssueId INTEGER NOT NULL,
                    CommentText TEXT NOT NULL,
                    CommenterFirstName TEXT NOT NULL,
                    CommenterLastName TEXT NOT NULL,
                    CommentedAt TEXT NOT NULL
                );
            ";
            await command.ExecuteNonQueryAsync();
        }
        else
        {
            // بررسی وجود فیلد IssueType
            command.CommandText = "PRAGMA table_info(SoftwareIssues);";
            using var reader = await command.ExecuteReaderAsync();
            var hasIssueType = false;
            while (await reader.ReadAsync())
            {
                var columnName = reader.GetString(1);
                if (columnName == "IssueType")
                {
                    hasIssueType = true;
                    break;
                }
            }
            await reader.CloseAsync();
            
            if (!hasIssueType)
            {
                // اضافه کردن فیلد IssueType
                command.CommandText = "ALTER TABLE SoftwareIssues ADD COLUMN IssueType TEXT NOT NULL DEFAULT '';";
                await command.ExecuteNonQueryAsync();
            }
            
            // بررسی وجود جدول IssueComments
            command.CommandText = "SELECT name FROM sqlite_master WHERE type='table' AND name='IssueComments';";
            var commentsTableExists = await command.ExecuteScalarAsync();
            
            if (commentsTableExists == null)
            {
                // ایجاد جدول IssueComments
                command.CommandText = @"
                    CREATE TABLE IssueComments (
                        Id INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
                        IssueId INTEGER NOT NULL,
                        CommentText TEXT NOT NULL,
                        CommenterFirstName TEXT NOT NULL,
                        CommenterLastName TEXT NOT NULL,
                        CommentedAt TEXT NOT NULL
                    );
                ";
                await command.ExecuteNonQueryAsync();
            }
        }
        
        await connection.CloseAsync();
    }
    catch (Exception ex)
    {
        // در صورت خطا، لاگ می‌کنیم اما ادامه می‌دهد
        logger.LogWarning(ex, "خطا در ایجاد جدول SoftwareIssues");
    }
    
    // Migration از JSON به SQLite (فقط یک بار)
    var migrationService = new MigrationService(
        dbContext, 
        scope.ServiceProvider.GetRequiredService<IWebHostEnvironment>());
    await migrationService.MigrateFromJsonToDatabase();
    
    // Migration تنظیمات از JSON به دیتابیس
    try
    {
        var configMigrationService = scope.ServiceProvider.GetRequiredService<ConfigMigrationService>();
        await configMigrationService.MigrateFromJsonAsync();
    }
    catch (Exception ex)
    {
        logger.LogWarning(ex, "خطا در Migration تنظیمات از JSON");
    }
}

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
