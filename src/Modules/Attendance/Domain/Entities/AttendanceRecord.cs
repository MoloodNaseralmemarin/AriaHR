using AriaHR.Shared;

namespace AriaHR.Modules.Attendance.Domain.Entities;

/// <summary>
/// AttendanceRecord entity recording attendance transactions.
/// </summary>
public class AttendanceRecord : BaseEntity
{
    public Guid OrganizationId { get; set; }
    public Guid EmployeeId { get; set; }
    public DateTime CheckInTime { get; set; }
    public DateTime? CheckOutTime { get; set; }
    public double? CheckInLatitude { get; set; }
    public double? CheckInLongitude { get; set; }
    public double? CheckOutLatitude { get; set; }
    public double? CheckOutLongitude { get; set; }
    public double? CheckInAccuracy { get; set; }
    public double? CheckOutAccuracy { get; set; }
    public Guid? WorkLocationId { get; set; }
    public Guid? QRCodeId { get; set; }
    public string Method { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public bool IsWithinGeofence { get; set; }
    public string? DeviceInfo { get; set; }
    public string? IPAddress { get; set; }
    public string? Notes { get; set; }
    public Guid? ApprovedBy { get; set; }
    public DateTime? ApprovedAt { get; set; }
}
