namespace AriaHR.Modules.Organization.Application.Options;

public class QrCodeOptions
{
    public const string SectionName = "QrCode";

    public int ExpirationMinutes { get; set; } = 5;
}
