namespace AriaHR.Modules.Attendance.Application.DTOs;

public class CheckOutRequest
{
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public double? Accuracy { get; set; }
    public string? DeviceInfo { get; set; }
    public string? IPAddress { get; set; }
    public string? Notes { get; set; }
}
