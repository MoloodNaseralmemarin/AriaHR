# AriaHR Database Connection and Hosting Configuration Guide

This guide describes how database connections and execution environments are configured in AriaHR for both **Development** and **Production** environments, including IIS hosting instructions, environment variable setup, and troubleshooting procedures.

---

## 1. Overview and Security Principles

AriaHR connects to different databases depending on the active execution environment:
- **Development:** Uses the local development database specified in `appsettings.Development.json`.
- **Production:** Uses the production SQL Server database configured on the hosting server via environment variables or IIS configuration.
- **Fail-Safe Security:**
  - `appsettings.json` contains no connection strings or credentials.
  - `appsettings.Production.json` omits `DefaultConnection` completely.
  - Production database credentials must **never** be committed to Git.
  - If `DefaultConnection` is not provided in Production, the application fails immediately at startup with:
    `InvalidOperationException: Database connection string 'DefaultConnection' is not configured.`
  - The application will **never** fall back to a local development database when running in Production.

---

## 2. Configuration Structure

The configuration files follow the standard ASP.NET Core layered configuration:

1. **`appsettings.json`**
   Contains general, non-sensitive configuration shared across all environments (Identity admins, JWT structure, OTP rules, etc.). It does **not** contain any connection string.

2. **`appsettings.Development.json`**
   Contains the local development connection string:
   ```json
   {
     "ConnectionStrings": {
       "DefaultConnection": "Server=DESKTOP-4NSD20E;Database=AriaHRDB;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=True"
     },
     "Logging": {
       "LogLevel": {
         "Default": "Information",
         "Microsoft.AspNetCore": "Warning"
       }
     }
   }
   ```

3. **`appsettings.Production.json`**
   Contains production-specific overrides (e.g. log levels). It intentionally does **not** declare `DefaultConnection`:
   ```json
   {
     "Logging": {
       "LogLevel": {
         "Default": "Warning",
         "Microsoft.AspNetCore": "Warning"
       }
     }
   }
   ```

---

## 3. Environment Detection

The active environment is controlled by the standard ASP.NET Core environment variable:

```text
ASPNETCORE_ENVIRONMENT
```

Values:
- `Development` &rarr; Loads `appsettings.json` then `appsettings.Development.json` (uses local database).
- `Production` &rarr; Loads `appsettings.json` then `appsettings.Production.json`, and resolves `DefaultConnection` strictly from environment variables or IIS server configuration.

---

## 4. Production Database Configuration

In Production, the connection string is provided via the standard ASP.NET Core environment variable format:

```text
ConnectionStrings__DefaultConnection
```

### Production Connection String Template

Use standard SQL Server connection parameters:

```text
Server=tcp:YOUR_PROD_SQL_SERVER,1433;Database=AriaHRDB_Prod;User ID=YOUR_DB_USER;Password=YOUR_DB_PASSWORD;Encrypt=True;TrustServerCertificate=False;MultipleActiveResultSets=True
```

*Replace `YOUR_PROD_SQL_SERVER`, `AriaHRDB_Prod`, `YOUR_DB_USER`, and `YOUR_DB_PASSWORD` with the actual production values.*

---

## 5. IIS Hosting Configuration

When deploying AriaHR to an IIS server (e.g. Windows Server or shared hosting control panels), configure the environment variables using one of the following methods.

### Method A: `web.config` `<environmentVariables>` (Recommended for IIS ASP.NET Core Module)

Within the site's `web.config` file inside the publish directory, define the variables under `<aspNetCore>`:

```xml
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <location path="." inheritInChildApplications="false">
    <system.webServer>
      <handlers>
        <add name="aspNetCore" path="*" verb="*" modules="AspNetCoreModuleV2" resourceType="Unspecified" />
      </handlers>
      <aspNetCore processPath="dotnet"
                  arguments=".\AriaHR.API.dll"
                  stdoutLogEnabled="false"
                  stdoutLogFile=".\logs\stdout"
                  hostingModel="inprocess">
        <environmentVariables>
          <environmentVariable name="ASPNETCORE_ENVIRONMENT" value="Production" />
          <environmentVariable name="ConnectionStrings__DefaultConnection"
                               value="Server=tcp:YOUR_PROD_SQL_SERVER,1433;Database=AriaHRDB_Prod;User ID=YOUR_DB_USER;Password=YOUR_DB_PASSWORD;Encrypt=True;TrustServerCertificate=False;MultipleActiveResultSets=True" />
        </environmentVariables>
      </aspNetCore>
    </system.webServer>
  </location>
</configuration>
```

> **Note:** Ensure that any `web.config` containing actual production passwords is kept outside public Git repositories and configured during deployment or server provisioning.

---

### Method B: IIS Manager Configuration Editor (Server-Level / App Pool)

To avoid storing credentials in `web.config` on disk:

1. Open **IIS Manager**.
2. Select your server or site, and double-click **Configuration Editor**.
3. In the **Section** dropdown, navigate to:
   `system.webServer/aspNetCore`
4. Find **environmentVariables** and click the `...` button to open the Collection Editor.
5. Click **Add**:
   - `name`: `ASPNETCORE_ENVIRONMENT`
   - `value`: `Production`
6. Click **Add**:
   - `name`: `ConnectionStrings__DefaultConnection`
   - `value`: `Server=tcp:YOUR_PROD_SQL_SERVER,1433;Database=AriaHRDB_Prod;User ID=YOUR_DB_USER;Password=YOUR_DB_PASSWORD;Encrypt=True;TrustServerCertificate=False;MultipleActiveResultSets=True`
7. Click **Apply** in the Actions pane and recycle the Application Pool.

---

### Method C: Hosting Provider Control Panel (Plesk / cPanel / Windows Cloud)

If your hosting provider offers a web control panel:
1. Navigate to **Websites & Domains** &rarr; select the AriaHR site.
2. Go to **Hosting & DNS** &rarr; **Environment Variables** (or **ASP.NET Core Settings**).
3. Add:
   - `ASPNETCORE_ENVIRONMENT`: `Production`
   - `ConnectionStrings__DefaultConnection`: `<Your-Production-Connection-String>`
4. Save and restart the website application pool.

---

## 6. Centralized Resolution in Application Startup

In `src/AriaHR.API/Program.cs`, the connection string is resolved and validated early before any module services are registered:

```csharp
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

if (string.IsNullOrWhiteSpace(connectionString))
{
    throw new InvalidOperationException("Database connection string 'DefaultConnection' is not configured.");
}

// Pass validated connection string to modules:
builder.Services.AddIdentityModule(builder.Configuration, connectionString);
builder.Services.AddOrganizationModule(builder.Configuration, connectionString);
builder.Services.AddAttendanceModule(builder.Configuration, connectionString);
builder.Services.AddSchedulingModule(builder.Configuration, connectionString);
builder.Services.AddRequestsModule(builder.Configuration, connectionString);
builder.Services.AddNotificationModule(builder.Configuration, connectionString);
```

All modules configure their EF Core `DbContext` instances (`AttendanceDbContext`, `IdentityDbContext`, `NotificationDbContext`, `OrganizationDbContext`, `RequestsDbContext`, `SchedulingDbContext`) with SQL Server and isolation tables (e.g. `__EFMigrationsHistory_Attendance`, `__EFMigrationsHistory_Identity`, etc.) using this exact connection string.

---

## 7. Verification and Troubleshooting

### Verifying Environment and Connection

1. **Verify Startup:**
   - When running in Production without `ConnectionStrings__DefaultConnection`, check `stdout` logs or Windows Event Viewer. You should see:
     `System.InvalidOperationException: Database connection string 'DefaultConnection' is not configured.`
   - When properly configured, the API starts and responds on its health/root endpoints without error.

2. **Verify Database Connectivity:**
   - Ensure the hosting server has outbound network access to the SQL Server port (`1433` by default).
   - Test connectivity from PowerShell if available:
     ```powershell
     Test-NetConnection -ComputerName YOUR_PROD_SQL_SERVER -Port 1433
     ```
   - Ensure SQL Server has mixed-mode authentication enabled if using SQL authentication (`User ID` and `Password`).
   - If using Azure SQL or modern SQL Server with SSL encryption, ensure `Encrypt=True` and configure trust certificates appropriately.

3. **EF Core Migrations:**
   - To apply migrations to the production database:
     ```bash
     dotnet ef database update --project src/Modules/Identity/Infrastructure --startup-project src/AriaHR.API --connection "Server=...;Database=...;..."
     ```
   - Or apply generated SQL migration scripts during your CI/CD deployment pipeline.

4. **Security Checklist:**
   - [x] No production passwords in `appsettings.json` or `appsettings.Production.json`.
   - [x] `.gitignore` ignores local files (`appsettings.Development.json` is template only; `appsettings.Local.json`, `appsettings.*.local.json`, and `.env` files are excluded).
   - [x] Application fails fast if `DefaultConnection` is absent in Production.
   - [x] Production environment cannot accidentally fall back to local development database.
