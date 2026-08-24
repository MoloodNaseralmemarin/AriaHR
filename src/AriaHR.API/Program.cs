using System.Text;
using AriaHR.Modules.Attendance.Infrastructure;
using AriaHR.Modules.Identity.Infrastructure;
using AriaHR.Modules.Identity.Infrastructure.Authentication;
using AriaHR.Modules.Organization.Infrastructure;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

builder.Services.AddOpenApi();


var jwtOptions =
    builder.Configuration
        .GetSection(JwtOptions.SectionName)
        .Get<JwtOptions>()
    ?? new JwtOptions();

var secretKey = string.IsNullOrWhiteSpace(jwtOptions.SecretKey)
    ? "DEFAULT_DEVELOPMENT_SECRET_KEY_FOR_LOCAL_DEV_ONLY_MIN_256_BITS"
    : jwtOptions.SecretKey;

var secretKeyBytes = Encoding.UTF8.GetBytes(secretKey);

builder.Services
    .AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme =
            JwtBearerDefaults.AuthenticationScheme;

        options.DefaultChallengeScheme =
            JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.RequireHttpsMetadata = false; // Development only
        options.SaveToken = true;

        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtOptions.Issuer,

            ValidateAudience = true,
            ValidAudience = jwtOptions.Audience,

            ValidateIssuerSigningKey = true,
            IssuerSigningKey =
                new SymmetricSecurityKey(secretKeyBytes),

            ValidateLifetime = true,

            ClockSkew = TimeSpan.FromMinutes(1)
        };
    });

builder.Services.AddAuthorization();

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

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

await app.SeedIdentityAsync();

app.Run();
