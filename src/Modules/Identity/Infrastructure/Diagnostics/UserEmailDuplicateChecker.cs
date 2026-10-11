using AriaHR.Modules.Identity.Application.Common;
using AriaHR.Modules.Identity.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AriaHR.Modules.Identity.Infrastructure.Diagnostics;

public class DuplicateEmailConflict
{
    public string NormalizedEmail { get; set; } = string.Empty;
    public List<Guid> UserIds { get; set; } = new();
}

public class DuplicateEmailReport
{
    public bool HasConflicts => Conflicts.Count > 0;
    public List<DuplicateEmailConflict> Conflicts { get; set; } = new();
}

public static class UserEmailDuplicateChecker
{
    /// <summary>
    /// Scans active non-deleted users in the IdentityDbContext for duplicate email addresses
    /// after applying normalization rules.
    /// </summary>
    public static async Task<DuplicateEmailReport> CheckForDuplicateEmailsAsync(
        IdentityDbContext identityDb,
        ILogger? logger = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(identityDb);

        var usersWithEmail = await identityDb.Users
            .AsNoTracking()
            .Where(u => !u.IsDeleted && u.Email != null && u.Email != "")
            .Select(u => new { u.Id, u.Email })
            .ToListAsync(cancellationToken);

        var conflicts = usersWithEmail
            .Select(u => new { u.Id, NormalizedEmail = EmailNormalizer.Normalize(u.Email) })
            .Where(x => x.NormalizedEmail != null)
            .GroupBy(x => x.NormalizedEmail!, StringComparer.OrdinalIgnoreCase)
            .Where(g => g.Count() > 1)
            .Select(g => new DuplicateEmailConflict
            {
                NormalizedEmail = g.Key,
                UserIds = g.Select(x => x.Id).ToList()
            })
            .ToList();

        var report = new DuplicateEmailReport { Conflicts = conflicts };

        if (report.HasConflicts)
        {
            logger?.LogWarning(
                "Pre-migration check: Detected {ConflictCount} duplicate normalized email address conflicts among active users.",
                conflicts.Count);

            foreach (var conflict in conflicts)
            {
                logger?.LogWarning(
                    "Duplicate normalized email conflict: '{NormalizedEmail}' associated with User IDs: {UserIds}",
                    conflict.NormalizedEmail, string.Join(", ", conflict.UserIds));
            }
        }
        else
        {
            logger?.LogInformation("Pre-migration check: No duplicate email conflicts found among active users.");
        }

        return report;
    }
}
