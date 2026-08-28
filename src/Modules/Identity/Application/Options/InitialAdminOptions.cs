namespace AriaHR.Modules.Identity.Application.Options;

public class InitialAdminUserOptions
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string Email {  get; set; } = string.Empty;
}

public class SeedUserOptions
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public Guid? OrganizationId { get; set; }
}

public class InitialAdminOptions
{
    public const string SectionName = "Identity";

    public List<InitialAdminUserOptions> InitialAdmins { get; set; } = [];
    public List<SeedUserOptions> SeedUsers { get; set; } = [];
}
