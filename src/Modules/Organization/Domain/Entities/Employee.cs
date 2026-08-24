using AriaHR.Shared;

namespace AriaHR.Modules.Organization.Domain.Entities;

/// <summary>
/// Employee entity belonging to the Organization module.
/// </summary>
public class Employee : BaseEntity
{
    /// <summary>
    /// References the authentication identity in the Identity module.
    /// No EF navigation property should exist across module boundaries.
    /// </summary>
    public Guid UserId { get; set; }

    public Guid OrganizationId { get; set; }
    public Organization? Organization { get; set; }

    public Guid BranchId { get; set; }
    public Branch? Branch { get; set; }

    public string PersonnelCode { get; set; } = string.Empty;

    public string NationalCode { get; set; } = string.Empty;

    public DateOnly BirthDate { get; set; }

    public string? Gender { get; set; }

    public DateOnly HireDate { get; set; }

    public Guid DepartmentId { get; set; }
    public Department? Department { get; set; }

    public Guid PositionId { get; set; }
    public Position? Position { get; set; }

    public Guid? ManagerId { get; set; }
    public Employee? Manager { get; set; }

    public bool IsActive { get; set; }

    public string? ProfileImagePath { get; set; }
}