using AriaHR.Shared;

namespace AriaHR.Modules.Identity.Domain.Entities;

/// <summary>
/// User entity representing the authentication identity of a person.
/// </summary>
public class User : BaseEntity
{
    /// <summary>
    /// User's first name.
    /// </summary>
    public string FirstName { get; set; } = string.Empty;

    /// <summary>
    /// User's last name.
    /// </summary>
    public string LastName { get; set; } = string.Empty;

    /// <summary>
    /// Unique mobile number used for authentication.
    /// </summary>
    public string PhoneNumber { get; set; } = string.Empty;

    /// <summary>
    /// Optional email address.
    /// </summary>
    public string? Email { get; set; }

    /// <summary>
    /// Indicates whether the user is allowed to authenticate.
    /// </summary>
    public bool IsActive { get; set; }

    /// <summary>
    /// Last successful login time.
    /// </summary>
    public DateTime? LastLoginAt { get; set; }
}