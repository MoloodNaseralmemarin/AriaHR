using System.Text.Json.Serialization;
using AriaHR.Modules.Requests.API.Controllers;
using Microsoft.Extensions.DependencyInjection;

namespace AriaHR.Modules.Requests.API;

public static class DependencyInjection
{
    public static IServiceCollection AddRequestsApi(this IServiceCollection services)
    {
        services.AddControllers()
            .AddJsonOptions(options =>
            {
                options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
            })
            .AddApplicationPart(typeof(LeaveRequestsController).Assembly);

        return services;
    }
}
