namespace AriaHR.Modules.Organization.Application.Exceptions;

/// <summary>
/// Raised when a QR code operation cannot be completed. Carries a stable error code for API consumers.
/// </summary>
public class QrCodeOperationException : Exception
{
    public string ErrorCode { get; }
    public int StatusCode { get; }

    public QrCodeOperationException(string errorCode, string message, int statusCode = 400)
        : base(message)
    {
        ErrorCode = errorCode;
        StatusCode = statusCode;
    }
}
