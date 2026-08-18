using AriaHR.Modules.Attendance.Infrastructure;
using AriaHR.Modules.Identity.Infrastructure;
using AriaHR.Modules.Notification.Infrastructure;
using AriaHR.Modules.Organization.Infrastructure;
using AriaHR.Modules.Payroll.Infrastructure;
using AriaHR.Modules.Reporting.Infrastructure;
using AriaHR.Modules.Requests.Infrastructure;
using AriaHR.Modules.Scheduling.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

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

app.Run();
