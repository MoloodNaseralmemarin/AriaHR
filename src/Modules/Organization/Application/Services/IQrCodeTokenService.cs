using AriaHR.Modules.Organization.Domain.Enums;

namespace AriaHR.Modules.Organization.Application.Services;

public record QrCodeTokenPayload(
    Guid WorkLocationId,
    string Identifier,
    QrCodeMode Mode,
    DateTime? ExpiresAtUtc
);

public record IssuedQrToken(
    string Token,
    QrCodeMode Mode,
    DateTime? ExpiresAtUtc,
    DateTime? RefreshAfterUtc
);

public interface IQrCodeTokenService
{
    /// <summary>
    /// Issues a signed, tamper-proof QR token for a work location. Dynamic tokens expire at the end of the
    /// current rotation window and are produced without any database write.
    /// </summary>
    IssuedQrToken Issue(
        Guid workLocationId,
        string identifier,
        QrCodeMode mode,
        int rotationIntervalMinutes,
        DateTime nowUtc);

    /// <summary>
    /// Verifies the signature of a QR token and returns its payload. Signature verification only —
    /// database state (enabled flag, identifier, mode) is validated by the work location lookup.
    /// </summary>
    bool TryParse(string? token, out QrCodeTokenPayload? payload);
}
