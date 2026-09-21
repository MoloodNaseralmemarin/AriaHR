using AriaHR.Modules.Identity.Infrastructure.Persistence;
using AriaHR.Modules.Organization.Infrastructure.Persistence;

namespace AriaHR.API.DataRepair;

public static class DataRepairExtensions
{
    public static async Task<IHost> RepairUnlinkedCenterManagersAsync(this IHost host)
    {
        ArgumentNullException.ThrowIfNull(host);

        using var scope = host.Services.CreateScope();
        var identityDb = scope.ServiceProvider.GetService<IdentityDbContext>();
        var orgDb = scope.ServiceProvider.GetService<OrganizationDbContext>();
        var loggerFactory = scope.ServiceProvider.GetService<ILoggerFactory>();
        var logger = loggerFactory?.CreateLogger("DataRepair");

        if (identityDb != null && orgDb != null)
        {
            await IdentityOrganizationDataRepair.RepairUnlinkedCenterManagersAsync(identityDb, orgDb, logger);
        }

        return host;
    }
}
