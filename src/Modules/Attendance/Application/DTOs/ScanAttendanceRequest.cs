namespace AriaHR.Modules.Attendance.Application.DTOs;

/// <summary>
/// QR + GPS attendance scan request. The employee identity is always taken from JWT claims, never from this body.
/// </summary>
public class ScanAttendanceRequest
{
    public string? QRCodeToken { get; set; }
    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }
    public double? GpsAccuracyInMeters { get; set; }
    public DateTime? LocationTimestampUtc { get; set; }

    /// <summary>
    /// Optional explicit action: "CheckIn" or "CheckOut". When omitted the server determines it from the
    /// employee's active attendance record.
    /// </summary>
    public string? AttendanceAction { get; set; }

    public string? IdempotencyKey { get; set; }
    public string? DeviceInfo { get; set; }
    public string? Notes { get; set; }
}
