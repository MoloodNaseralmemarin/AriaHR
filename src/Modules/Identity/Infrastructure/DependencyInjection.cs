using AriaHR.Modules.Identity.Application.Options;
using AriaHR.Modules.Identity.Application.Repositories;
using AriaHR.Modules.Identity.Application.Services;
using AriaHR.Modules.Identity.Application.UseCases.ForgotPassword;
using AriaHR.Modules.Identity.Application.UseCases.Login;
using AriaHR.Modules.Identity.Application.UseCases.RefreshToken;
using AriaHR.Modules.Identity.Application.UseCases.Registration;
using AriaHR.Modules.Identity.Application.UseCases.Role;
using AriaHR.Modules.Identity.Infrastructure.Authentication;
using AriaHR.Modules.Identity.Infrastructure.Persistence;
using AriaHR.Modules.Identity.Infrastructure.Repositories;
using AriaHR.Modules.Identity.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace AriaHR.Modules.Identity.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddIdentityModule(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        return services.AddIdentityInfrastructure(configuration);
    }

    public static IServiceCollection AddIdentityInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<JwtOptions>(configuration.GetSection(JwtOptions.SectionName));
        services.Configure<OtpOptions>(configuration.GetSection(OtpOptions.SectionName));
        services.AddSingleton(sp => sp.GetRequiredService<IOptions<OtpOptions>>().Value);

        var connectionString = configuration.GetConnectionString("IdentityDatabase")
            ?? configuration.GetConnectionString("DefaultConnection");

        services.AddDbContext<IdentityDbContext>(options =>
        {
            if (!string.IsNullOrEmpty(connectionString))
            {
                options.UseSqlServer(connectionString, sqlOptions =>
                {
                    sqlOptions.MigrationsHistoryTable("__EFMigrationsHistory_Identity");
                });
            }
        });

        // Repositories
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IRoleRepository, RoleRepository>();
        services.AddScoped<IUserRoleRepository, UserRoleRepository>();
        services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
        services.AddScoped<IPasswordResetRepository, PasswordResetRepository>();
        services.AddScoped<IPendingRegistrationRepository, PendingRegistrationRepository>();

        // Infrastructure Services
        services.AddScoped<ITokenService, JwtTokenService>();
        services.AddScoped<IPasswordService, PasswordService>();
        services.AddScoped<IAuthNotificationService, AuthNotificationService>();

        // Use Cases
        services.AddScoped<LoginUseCase>();
        services.AddScoped<RefreshTokenUseCase>();
        services.AddScoped<ForgotPasswordUseCase>();
        services.AddScoped<ResetPasswordUseCase>();
        services.AddScoped<InitiateRegistrationUseCase>();
        services.AddScoped<VerifyRegistrationOtpUseCase>();
        services.AddScoped<RoleUseCase>();

        return services;
    }
}
