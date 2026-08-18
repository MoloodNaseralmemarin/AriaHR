using AriaHR.Shared;

namespace AriaHR.Modules.Identity.Domain.Entities;

/// <summary>
/// Role entity representing system access roles.
/// </summary>
public class Role : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
}
