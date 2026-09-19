using System.Security.Claims;
using Microsoft.AspNetCore.Http;

namespace AriaHR.Shared.Services;

public interface ICurrentUserService
{
    Guid UserId { get; }
    IReadOnlyList<string> Roles { get; }
    Guid? OrganizationId { get; }
    bool IsInRole(string role);
    Guid ResolveOrganizationId(Guid? requestOrgId = null);
}

public class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUserService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
    }

    private ClaimsPrincipal? User => _httpContextAccessor.HttpContext?.User;

    public Guid UserId
    {
        get
        {
            var user = User;
            if (user == null) return Guid.Empty;

            var claim = user.FindFirstValue(ClaimTypes.NameIdentifier) ?? user.FindFirstValue("sub");
            if (!string.IsNullOrEmpty(claim) && Guid.TryParse(claim, out var id))
            {
                return id;
            }

            return Guid.Empty;
        }
    }

    public IReadOnlyList<string> Roles
    {
        get
        {
            var user = User;
            if (user == null) return Array.Empty<string>();

            return user.FindAll(ClaimTypes.Role).Select(c => c.Value).ToList();
        }
    }

    public Guid? OrganizationId
    {
        get
        {
            var user = User;
            if (user == null) return null;

            var claim = user.FindFirstValue("organization_id");
            if (!string.IsNullOrEmpty(claim) && Guid.TryParse(claim, out var orgId))
            {
                return orgId;
            }

            return null;
        }
    }

    public bool IsInRole(string role)
    {
        return User?.IsInRole(role) ?? false;
    }

    public Guid ResolveOrganizationId(Guid? requestOrgId = null)
    {
        if (IsInRole("SystemAdmin"))
        {
            if (requestOrgId.HasValue && requestOrgId.Value != Guid.Empty)
            {
                return requestOrgId.Value;
            }

            return OrganizationId ?? Guid.Empty;
        }

        // For non-SystemAdmin (e.g. CenterManager):
        // If requestOrgId is supplied and differs from user's OrganizationId, reject (return Guid.Empty)
        if (requestOrgId.HasValue && requestOrgId.Value != Guid.Empty && OrganizationId.HasValue && requestOrgId.Value != OrganizationId.Value)
        {
            return Guid.Empty;
        }

        return OrganizationId ?? Guid.Empty;
    }
}
