using AriaHR.Modules.Attendance.Application.Options;
using AriaHR.Modules.Attendance.Application.Repositories;
using AriaHR.Modules.Attendance.Application.UseCases;
using AriaHR.Modules.Attendance.Infrastructure.Persistence;
using AriaHR.Modules.Attendance.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AriaHR.Modules.Attendance.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddAttendanceModule(
        this IServiceCollection services,
        IConfiguration configuration,
        string? connectionString = null)
    {
        connectionString ??= configuration.GetConnectionString("DefaultConnection");

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "Database connection string 'DefaultConnection' is not configured.");
        }

        services.AddDbContext<AttendanceDbContext>(options =>
        {
            options.UseSqlServer(connectionString, sqlOptions =>
            {
                sqlOptions.MigrationsHistoryTable("__EFMigrationsHistory_Attendance");
            });
        });

        services.Configure<GpsValidationOptions>(configuration.GetSection(GpsValidationOptions.SectionName));
        services.Configure<AttendancePolicyOptions>(configuration.GetSection(AttendancePolicyOptions.SectionName));

        services.AddScoped<IAttendanceRecordRepository, AttendanceRecordRepository>();
        services.AddScoped<ICheckInUseCase, CheckInUseCase>();
        services.AddScoped<ICheckOutUseCase, CheckOutUseCase>();
        services.AddScoped<IGetMyAttendanceStatusUseCase, GetMyAttendanceStatusUseCase>();
        services.AddScoped<IGetMyAttendanceHistoryUseCase, GetMyAttendanceHistoryUseCase>();
        services.AddScoped<IGetOrganizationAttendanceUseCase, GetOrganizationAttendanceUseCase>();
        services.AddScoped<IUpdateAttendanceStatusUseCase, UpdateAttendanceStatusUseCase>();
        services.AddScoped<IScanAttendanceUseCase, ScanAttendanceUseCase>();

        return services;
    }
}
