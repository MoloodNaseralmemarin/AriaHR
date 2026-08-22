using System.Text;
using AriaHR.Modules.Attendance.Infrastructure;
using AriaHR.Modules.Identity.Infrastructure;
using AriaHR.Modules.Identity.Infrastructure.Authentication;
using AriaHR.Modules.Notification.Infrastructure;
using AriaHR.Modules.Organization.Infrastructure;
using AriaHR.Modules.Payroll.Infrastructure;
using AriaHR.Modules.Reporting.Infrastructure;
using AriaHR.Modules.Requests.Infrastructure;
using AriaHR.Modules.Scheduling.Infrastructure;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddOpenApi();

// JWT Authentication Configuration
var jwtOptions = builder.Configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>() ?? new JwtOptions();
var secretKeyBytes = Encoding.UTF8.GetBytes(string.IsNullOrEmpty(jwtOptions.SecretKey)
    ? "DEFAULT_DEVELOPMENT_SECRET_KEY_FOR_LOCAL_DEV_ONLY_MIN_256_BITS"
    : jwtOptions.SecretKey);

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.RequireHttpsMetadata = false; // Set true in production
    options.SaveToken = true;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidIssuer = jwtOptions.Issuer,
        ValidateAudience = true,
        ValidAudience = jwtOptions.Audience,
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(secretKeyBytes),
        ValidateLifetime = true,
        ClockSkew = TimeSpan.FromMinutes(1)
    };
});

builder.Services.AddAuthorization();

// AriaHR Modules
builder.Services.AddIdentityModule(builder.Configuration);
builder.Services.AddOrganizationModule(builder.Configuration);
builder.Services.AddSchedulingModule(builder.Configuration);
builder.Services.AddAttendanceModule(builder.Configuration);
builder.Services.AddRequestsModule(builder.Configuration);
builder.Services.AddNotificationModule(builder.Configuration);
builder.Services.AddReportingModule(builder.Configuration);
builder.Services.AddPayrollModule(builder.Configuration);

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
