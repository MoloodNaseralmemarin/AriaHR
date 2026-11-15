namespace AriaHR.Modules.Attendance.Application.DTOs;

public class AttendanceScanData
{
    public Guid? AttendanceId { get; set; }
    public Guid? WorkLocationId { get; set; }
    public string? WorkLocationName { get; set; }
    public string? AttendanceAction { get; set; }
    public decimal? DistanceInMeters { get; set; }
    public decimal? AllowedRadiusInMeters { get; set; }
    public double? GpsAccuracyInMeters { get; set; }
    public DateTime? RegisteredAtUtc { get; set; }
    public string? AttendanceStatus { get; set; }
    public bool? IsLate { get; set; }
    public bool? IsEarlyDeparture { get; set; }
    public string? ShiftName { get; set; }
    public bool IsReplay { get; set; }
}

/// <summary>
/// Consistent response envelope for the QR + GPS attendance scan endpoint.
/// </summary>
public class AttendanceScanResponse
{
    public bool Success { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public AttendanceScanData? Data { get; set; }

    public static AttendanceScanResponse Ok(string code, string message, AttendanceScanData? data = null)
    {
        return new AttendanceScanResponse
        {
            Success = true,
            Code = code,
            Message = message,
            Data = data
        };
    }

    public static AttendanceScanResponse Fail(string code, string message, AttendanceScanData? data = null)
    {
        return new AttendanceScanResponse
        {
            Success = false,
            Code = code,
            Message = message,
            Data = data
        };
    }
}
