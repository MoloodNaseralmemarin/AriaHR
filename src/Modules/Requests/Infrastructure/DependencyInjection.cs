using AriaHR.Modules.Requests.Application.Repositories;
using AriaHR.Modules.Requests.Application.UseCases.ApproveLeaveRequest;
using AriaHR.Modules.Requests.Application.UseCases.CancelLeaveRequest;
using AriaHR.Modules.Requests.Application.UseCases.ChangeLeaveTypeStatus;
using AriaHR.Modules.Requests.Application.UseCases.CreateLeaveRequest;
using AriaHR.Modules.Requests.Application.UseCases.CreateLeaveType;
using AriaHR.Modules.Requests.Application.UseCases.GetLeaveRequestById;
using AriaHR.Modules.Requests.Application.UseCases.GetLeaveTypeById;
using AriaHR.Modules.Requests.Application.UseCases.GetMyLeaveRequests;
using AriaHR.Modules.Requests.Application.UseCases.GetOrganizationLeaveRequests;
using AriaHR.Modules.Requests.Application.UseCases.GetOrganizationLeaveTypes;
using AriaHR.Modules.Requests.Application.UseCases.RejectLeaveRequest;
using AriaHR.Modules.Requests.Application.UseCases.UpdateLeaveType;
using AriaHR.Modules.Requests.Infrastructure.Persistence;
using AriaHR.Modules.Requests.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AriaHR.Modules.Requests.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddRequestsModule(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection");

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "Connection string 'DefaultConnection' was not configured.");
        }

        services.AddDbContext<RequestsDbContext>(options =>
        {
            options.UseSqlServer(connectionString, sqlOptions =>
            {
                sqlOptions.MigrationsHistoryTable("__EFMigrationsHistory_Requests");
            });
        });

        return services.AddRequestsInfrastructure();
    }

    public static IServiceCollection AddRequestsInfrastructure(this IServiceCollection services)
    {
        services.AddScoped<ILeaveRequestRepository, LeaveRequestRepository>();
        services.AddScoped<IMissionRequestRepository, MissionRequestRepository>();
        services.AddScoped<ILeaveTypeRepository, LeaveTypeRepository>();
        services.AddScoped<ILeaveBalanceRepository, LeaveBalanceRepository>();

        services.AddScoped<ICreateLeaveRequestUseCase, CreateLeaveRequestUseCase>();
        services.AddScoped<IGetMyLeaveRequestsUseCase, GetMyLeaveRequestsUseCase>();
        services.AddScoped<IGetLeaveRequestByIdUseCase, GetLeaveRequestByIdUseCase>();
        services.AddScoped<ICancelLeaveRequestUseCase, CancelLeaveRequestUseCase>();
        services.AddScoped<IGetOrganizationLeaveRequestsUseCase, GetOrganizationLeaveRequestsUseCase>();
        services.AddScoped<IApproveLeaveRequestUseCase, ApproveLeaveRequestUseCase>();
        services.AddScoped<IRejectLeaveRequestUseCase, RejectLeaveRequestUseCase>();

        services.AddScoped<ICreateLeaveTypeUseCase, CreateLeaveTypeUseCase>();
        services.AddScoped<IUpdateLeaveTypeUseCase, UpdateLeaveTypeUseCase>();
        services.AddScoped<IChangeLeaveTypeStatusUseCase, ChangeLeaveTypeStatusUseCase>();
        services.AddScoped<IGetLeaveTypeByIdUseCase, GetLeaveTypeByIdUseCase>();
        services.AddScoped<IGetOrganizationLeaveTypesUseCase, GetOrganizationLeaveTypesUseCase>();

        return services;
    }
}
