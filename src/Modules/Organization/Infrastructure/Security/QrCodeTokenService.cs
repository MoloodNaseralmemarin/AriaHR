using System.Security.Cryptography;
using System.Text;
using AriaHR.Modules.Organization.Application.Options;
using AriaHR.Modules.Organization.Application.Services;
using AriaHR.Modules.Organization.Domain.Enums;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

namespace AriaHR.Modules.Organization.Infrastructure.Security;

/// <summary>
/// Stateless HMAC-SHA256 QR token engine.
/// Token format: base64url(payload) + "." + base64url(signature).
/// Payload format: v1|{workLocationId:N}|{identifier}|{s|d}|{expiresAtUnixSeconds}
/// A static token has an expiry of 0 and stays valid while the identifier and settings stay unchanged.
/// </summary>
public class QrCodeTokenService : IQrCodeTokenService
{
    private const string PayloadVersion = "v1";
    private const int MaxIdentifierLength = 64;
    private const int MaxTokenLength = 512;

    private readonly byte[] _signingKey;
    private readonly QrCodeOptions _options;

    public QrCodeTokenService(IOptions<QrCodeOptions> options, IConfiguration configuration)
    {
        _options = options?.Value ?? new QrCodeOptions();

        string? rootKey = _options.SigningKey;
        if (string.IsNullOrWhiteSpace(rootKey))
        {
            rootKey = configuration["Jwt:SecretKey"] ?? configuration["Jwt:Key"];
        }

        if (string.IsNullOrWhiteSpace(rootKey))
        {
            rootKey = "AriaHR-QR-DEVELOPMENT-SIGNING-KEY-CHANGE-ME";
        }

        _signingKey = SHA256.HashData(Encoding.UTF8.GetBytes($"{PayloadVersion}:AriaHR.QrCode:{rootKey}"));
    }

    public IssuedQrToken Issue(
        Guid workLocationId,
        string identifier,
        QrCodeMode mode,
        int rotationIntervalMinutes,
        DateTime nowUtc)
    {
        if (workLocationId == Guid.Empty)
        {
            throw new ArgumentException("شناسه محل کار برای تولید کد کیوآر الزامی است.", nameof(workLocationId));
        }

        if (string.IsNullOrWhiteSpace(identifier))
        {
            throw new ArgumentException("شناسه امنیتی کد کیوآر تنظیم نشده است.", nameof(identifier));
        }

        DateTime now = NormalizeUtc(nowUtc);
        DateTime? expiresAtUtc = null;
        DateTime? refreshAfterUtc = null;

        if (mode == QrCodeMode.Dynamic)
        {
            int intervalMinutes = _options.ClampRotationInterval(rotationIntervalMinutes);
            long intervalSeconds = intervalMinutes * 60L;
            long nowUnix = new DateTimeOffset(now).ToUnixTimeSeconds();
            long windowStart = nowUnix / intervalSeconds * intervalSeconds;
            long windowEnd = windowStart + intervalSeconds;

            expiresAtUtc = DateTimeOffset.FromUnixTimeSeconds(windowEnd).UtcDateTime;
            refreshAfterUtc = expiresAtUtc;
        }

        long expiryUnix = expiresAtUtc.HasValue
            ? new DateTimeOffset(expiresAtUtc.Value).ToUnixTimeSeconds()
            : 0;

        string payload = string.Join(
            '|',
            PayloadVersion,
            workLocationId.ToString("N"),
            identifier,
            mode == QrCodeMode.Dynamic ? "d" : "s",
            expiryUnix.ToString());

        string token = $"{Base64UrlEncode(Encoding.UTF8.GetBytes(payload))}.{Base64UrlEncode(Sign(payload))}";

        return new IssuedQrToken(token, mode, expiresAtUtc, refreshAfterUtc);
    }

    public bool TryParse(string? token, out QrCodeTokenPayload? payload)
    {
        payload = null;

        if (string.IsNullOrWhiteSpace(token) || token.Length > MaxTokenLength)
        {
            return false;
        }

        int separator = token.IndexOf('.');
        if (separator <= 0 || separator != token.LastIndexOf('.') || separator == token.Length - 1)
        {
            return false;
        }

        string payloadPart = token[..separator];
        string signaturePart = token[(separator + 1)..];

        byte[] payloadBytes;
        byte[] providedSignature;
        try
        {
            payloadBytes = Base64UrlDecode(payloadPart);
            providedSignature = Base64UrlDecode(signaturePart);
        }
        catch (FormatException)
        {
            return false;
        }

        string payloadText;
        try
        {
            payloadText = Encoding.UTF8.GetString(payloadBytes);
        }
        catch (ArgumentException)
        {
            return false;
        }

        byte[] expectedSignature = Sign(payloadText);
        if (providedSignature.Length != expectedSignature.Length ||
            !CryptographicOperations.FixedTimeEquals(providedSignature, expectedSignature))
        {
            return false;
        }

        string[] segments = payloadText.Split('|');
        if (segments.Length != 5 || segments[0] != PayloadVersion)
        {
            return false;
        }

        if (!Guid.TryParseExact(segments[1], "N", out var workLocationId) || workLocationId == Guid.Empty)
        {
            return false;
        }

        string identifier = segments[2];
        if (string.IsNullOrWhiteSpace(identifier) || identifier.Length > MaxIdentifierLength)
        {
            return false;
        }

        QrCodeMode mode;
        if (segments[3] == "d")
        {
            mode = QrCodeMode.Dynamic;
        }
        else if (segments[3] == "s")
        {
            mode = QrCodeMode.Static;
        }
        else
        {
            return false;
        }

        if (!long.TryParse(segments[4], out long expiryUnix) || expiryUnix < 0)
        {
            return false;
        }

        DateTime? expiresAtUtc = expiryUnix == 0
            ? null
            : DateTimeOffset.FromUnixTimeSeconds(expiryUnix).UtcDateTime;

        payload = new QrCodeTokenPayload(workLocationId, identifier, mode, expiresAtUtc);
        return true;
    }

    private byte[] Sign(string payload)
    {
        using var hmac = new HMACSHA256(_signingKey);
        return hmac.ComputeHash(Encoding.UTF8.GetBytes(payload));
    }

    private static DateTime NormalizeUtc(DateTime value)
    {
        return value.Kind switch
        {
            DateTimeKind.Utc => value,
            DateTimeKind.Local => value.ToUniversalTime(),
            _ => DateTime.SpecifyKind(value, DateTimeKind.Utc)
        };
    }

    private static string Base64UrlEncode(byte[] data)
    {
        return Convert.ToBase64String(data)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
    }

    private static byte[] Base64UrlDecode(string value)
    {
        string normalized = value.Replace('-', '+').Replace('_', '/');
        switch (normalized.Length % 4)
        {
            case 2:
                normalized += "==";
                break;
            case 3:
                normalized += "=";
                break;
            case 1:
                throw new FormatException("Invalid base64url value.");
        }

        return Convert.FromBase64String(normalized);
    }
}
