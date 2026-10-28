namespace AriaHR.Modules.Attendance.Application.DTOs;

public class CheckInRequest
{
    public string Method { get; set; } = "GPS";
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public double? Accuracy { get; set; }
    public Guid? WorkLocationId { get; set; }
    public string? QRCodeToken { get; set; }
    public string? DeviceInfo { get; set; }
    public string? IPAddress { get; set; }
    public string? Notes { get; set; }
}
