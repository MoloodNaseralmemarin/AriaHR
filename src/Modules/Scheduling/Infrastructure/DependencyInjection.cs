using AriaHR.Modules.Scheduling.Application.Repositories;
using AriaHR.Modules.Scheduling.Application.UseCases.AssignShift;
using AriaHR.Modules.Scheduling.Application.UseCases.DefineShift;
using AriaHR.Modules.Scheduling.Application.UseCases.GetActiveShifts;
using AriaHR.Modules.Scheduling.Application.UseCases.GetEmployeeShiftCalendar;
using AriaHR.Modules.Scheduling.Application.UseCases.GetShiftById;
using AriaHR.Modules.Scheduling.Application.UseCases.GetShiftCalendar;
using AriaHR.Modules.Scheduling.Infrastructure.Persistence;
using AriaHR.Modules.Scheduling.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AriaHR.Modules.Scheduling.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddSchedulingModule(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection");

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "Connection string 'DefaultConnection' was not configured.");
        }

        services.AddDbContext<SchedulingDbContext>(options =>
        {
            options.UseSqlServer(connectionString, sqlOptions =>
            {
                sqlOptions.MigrationsHistoryTable("__EFMigrationsHistory_Scheduling");
            });
        });

        return services.AddSchedulingInfrastructure();
    }

    public static IServiceCollection AddSchedulingInfrastructure(this IServiceCollection services)
    {
        services.AddScoped<IShiftRepository, ShiftRepository>();
        services.AddScoped<IShiftAssignmentRepository, ShiftAssignmentRepository>();

        services.AddScoped<IDefineShiftUseCase, DefineShiftUseCase>();
        services.AddScoped<IGetActiveShiftsUseCase, GetActiveShiftsUseCase>();
        services.AddScoped<IGetShiftByIdUseCase, GetShiftByIdUseCase>();
        services.AddScoped<IAssignShiftUseCase, AssignShiftUseCase>();
        services.AddScoped<IGetShiftCalendarUseCase, GetShiftCalendarUseCase>();
        services.AddScoped<IGetEmployeeShiftCalendarUseCase, GetEmployeeShiftCalendarUseCase>();

        return services;
    }
}
