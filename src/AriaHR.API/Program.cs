using System.Text;
using AriaHR.Modules.Attendance.Infrastructure;
using AriaHR.Modules.Identity.Infrastructure;
using AriaHR.Modules.Notification.Infrastructure;
using AriaHR.Modules.Organization.API;
using AriaHR.Modules.Organization.Infrastructure;
using AriaHR.Modules.Payroll.Infrastructure;
using AriaHR.Modules.Reporting.Infrastructure;
using AriaHR.Modules.Requests.Infrastructure;
using AriaHR.Modules.Scheduling.Infrastructure;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddIdentityApi();
builder.Services.AddOrganizationApi();
builder.Services.AddOpenApi();

// Authentication & Authorization
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    var jwtKey = builder.Configuration["Jwt:SecretKey"] ?? builder.Configuration["Jwt:Key"];
    if (string.IsNullOrWhiteSpace(jwtKey))
    {
        if (builder.Environment.IsDevelopment())
        {
            jwtKey = "YOUR_DEV_SECRET_KEY_MUST_BE_AT_LEAST_256_BITS_LONG_FOR_SECURITY_CHANGEME";
        }
        else
        {
            throw new InvalidOperationException("JWT Key 'Jwt:SecretKey' is not configured.");
        }
    }

    var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "AriaHR.API";
    var jwtAudience = builder.Configuration["Jwt:Audience"] ?? "AriaHR.Clients";

    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtIssuer,
        ValidAudience = jwtAudience,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
    };
});

builder.Services.AddCors(options =>
{
    options.AddPolicy("Frontend", policy =>
    {
        policy
            .WithOrigins("http://localhost:4200")
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("SystemAdminPolicy", policy => policy.RequireRole("SystemAdmin"));
    options.AddPolicy("CenterManagerPolicy", policy => policy.RequireRole("CenterManager", "SystemAdmin"));
    options.AddPolicy("EmployeePolicy", policy => policy.RequireRole("Employee", "CenterManager", "SystemAdmin"));
});

// AriaHR Modules
builder.Services.AddIdentityModule(builder.Configuration);

builder.Services.AddOrganizationModule(builder.Configuration);

builder.Services.AddAttendanceModule(builder.Configuration);

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference(options =>
    {
        options
            .WithTitle("AriaHR API")
            .WithTheme(ScalarTheme.Default);
    });
}

app.UseHttpsRedirection();

app.UseCors("Frontend");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
