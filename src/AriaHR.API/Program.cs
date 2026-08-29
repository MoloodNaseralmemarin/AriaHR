using System.Text;
using AriaHR.Modules.Attendance.Infrastructure;
using AriaHR.Modules.Identity.API;
using AriaHR.Modules.Identity.Infrastructure;
using AriaHR.Modules.Organization.API;
using AriaHR.Modules.Organization.Infrastructure;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddIdentityApi();
builder.Services.AddOrganizationApi();

// OpenAPI & Bearer Authentication Configuration for Scalar API Reference
builder.Services.AddOpenApi(options =>
{
    options.AddDocumentTransformer((document, context, cancellationToken) =>
    {
        var scheme = new OpenApiSecurityScheme
        {
            Type = SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT",
            Description = "Enter JWT Bearer token"
        };

        var components = document.Components ??= new OpenApiComponents();
        components.SecuritySchemes ??= new Dictionary<string, IOpenApiSecurityScheme>();
        components.SecuritySchemes["Bearer"] = scheme;

        return Task.CompletedTask;
    });

    options.AddOperationTransformer((operation, context, cancellationToken) =>
    {
        var metadata = context.Description.ActionDescriptor.EndpointMetadata;
        var hasAuthorize = metadata.OfType<IAuthorizeData>().Any();
        var hasAllowAnonymous = metadata.OfType<IAllowAnonymous>().Any();

        if (hasAuthorize && !hasAllowAnonymous)
        {
            operation.Security ??= new List<OpenApiSecurityRequirement>();
            var securityRequirement = new OpenApiSecurityRequirement();
            var schemeRef = new OpenApiSecuritySchemeReference("Bearer", hostDocument: null, externalResource: null);
            securityRequirement.Add(schemeRef, new List<string>());

            operation.Security.Add(securityRequirement);
        }

        return Task.CompletedTask;
    });
});

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

app.UseCors("Frontend");

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

await app.SeedIdentityAsync();

app.Run();
