using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace AriaHR.Modules.Organization.Application.Services;

/// <summary>
/// Represents the server-side time window a dynamic QR code token belongs to.
/// </summary>
public sealed record QrCodeWindow(long Index, DateTime StartsAtUtc, DateTime EndsAtUtc);

public interface IQrCodeTokenService
{
    string CreateReference();
    string CreateSecret();
    string CreateStaticToken();
    QrCodeWindow GetWindow(DateTime utcNow, int rotationMinutes);
    string CreateDynamicToken(string codeReference, string secret, QrCodeWindow window);
    bool TryParseDynamicToken(string token, out string codeReference, out long windowIndex);
    bool TryValidateDynamicToken(
        string token,
        string codeReference,
        string secret,
        DateTime utcNow,
        int rotationMinutes,
        out QrCodeWindow window);
}

/// <summary>
/// Creates and validates cryptographically secure QR tokens.
/// Static tokens are opaque random values; dynamic tokens are HMAC signed values bound to
/// an opaque work location reference and a UTC aligned time window.
/// </summary>
public sealed class QrCodeTokenService : IQrCodeTokenService
{
    private const string TokenVersion = "v1";
    private const int ReferenceBytes = 16;
    private const int SecretBytes = 32;
    private const int StaticTokenBytes = 32;
    private const int ReferenceHexLength = ReferenceBytes * 2;

    public string CreateReference()
    {
        return Convert.ToHexStringLower(RandomNumberGenerator.GetBytes(ReferenceBytes));
    }

    public string CreateSecret()
    {
        return Convert.ToHexStringLower(RandomNumberGenerator.GetBytes(SecretBytes));
    }

    public string CreateStaticToken()
    {
        return Convert.ToHexStringLower(RandomNumberGenerator.GetBytes(StaticTokenBytes));
    }

    public QrCodeWindow GetWindow(DateTime utcNow, int rotationMinutes)
    {
        int interval = rotationMinutes < 1 ? 1 : rotationMinutes;

        long totalMinutes = (utcNow - DateTime.UnixEpoch).Ticks / TimeSpan.TicksPerMinute;
        long index = totalMinutes >= 0
            ? totalMinutes / interval
            : (totalMinutes - interval + 1) / interval;

        DateTime startsAtUtc = DateTime.UnixEpoch.AddTicks(index * interval * TimeSpan.TicksPerMinute);
        DateTime endsAtUtc = startsAtUtc.AddMinutes(interval);

        return new QrCodeWindow(index, startsAtUtc, endsAtUtc);
    }

    public string CreateDynamicToken(string codeReference, string secret, QrCodeWindow window)
    {
        if (string.IsNullOrWhiteSpace(codeReference))
        {
            throw new ArgumentException("مرجع امنیتی محل کار الزامی است.", nameof(codeReference));
        }

        if (string.IsNullOrWhiteSpace(secret))
        {
            throw new ArgumentException("کلید امنیتی محل کار الزامی است.", nameof(secret));
        }

        string normalizedReference = codeReference.Trim().ToLowerInvariant();
        byte[] signature = ComputeSignature(secret, normalizedReference, window.Index);

        return string.Join(
            '.',
            TokenVersion,
            normalizedReference,
            window.Index.ToString(CultureInfo.InvariantCulture),
            ToBase64Url(signature));
    }

    public bool TryParseDynamicToken(string token, out string codeReference, out long windowIndex)
    {
        codeReference = string.Empty;
        windowIndex = 0;

        if (string.IsNullOrWhiteSpace(token))
        {
            return false;
        }

        string[] parts = token.Trim().Split('.');
        if (parts.Length != 4)
        {
            return false;
        }

        if (!string.Equals(parts[0], TokenVersion, StringComparison.Ordinal))
        {
            return false;
        }

        if (parts[1].Length != ReferenceHexLength || !IsHex(parts[1]))
        {
            return false;
        }

        if (!long.TryParse(parts[2], NumberStyles.None, CultureInfo.InvariantCulture, out windowIndex))
        {
            return false;
        }

        if (string.IsNullOrEmpty(parts[3]))
        {
            return false;
        }

        codeReference = parts[1].ToLowerInvariant();
        return true;
    }

    public bool TryValidateDynamicToken(
        string token,
        string codeReference,
        string secret,
        DateTime utcNow,
        int rotationMinutes,
        out QrCodeWindow window)
    {
        window = GetWindow(utcNow, rotationMinutes);

        if (string.IsNullOrWhiteSpace(secret) || string.IsNullOrWhiteSpace(codeReference))
        {
            return false;
        }

        if (!TryParseDynamicToken(token, out string tokenReference, out long tokenWindowIndex))
        {
            return false;
        }

        if (!string.Equals(tokenReference, codeReference.Trim().ToLowerInvariant(), StringComparison.Ordinal))
        {
            return false;
        }

        if (tokenWindowIndex != window.Index)
        {
            return false;
        }

        if (utcNow < window.StartsAtUtc || utcNow >= window.EndsAtUtc)
        {
            return false;
        }

        string[] parts = token.Trim().Split('.');
        if (!TryFromBase64Url(parts[3], out byte[] providedSignature))
        {
            return false;
        }

        byte[] expectedSignature = ComputeSignature(secret, codeReference.Trim().ToLowerInvariant(), tokenWindowIndex);

        return CryptographicOperations.FixedTimeEquals(providedSignature, expectedSignature);
    }

    private static byte[] ComputeSignature(string secret, string codeReference, long windowIndex)
    {
        string payload = string.Join(
            ':',
            TokenVersion,
            codeReference,
            windowIndex.ToString(CultureInfo.InvariantCulture));

        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
        return hmac.ComputeHash(Encoding.UTF8.GetBytes(payload));
    }

    private static string ToBase64Url(byte[] value)
    {
        return Convert.ToBase64String(value)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
    }

    private static bool TryFromBase64Url(string value, out byte[] bytes)
    {
        bytes = Array.Empty<byte>();

        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        string base64 = value.Replace('-', '+').Replace('_', '/');
        int padding = (4 - base64.Length % 4) % 4;
        base64 = base64.PadRight(base64.Length + padding, '=');

        try
        {
            bytes = Convert.FromBase64String(base64);
            return true;
        }
        catch (FormatException)
        {
            return false;
        }
    }

    private static bool IsHex(string value)
    {
        foreach (char character in value)
        {
            if (!Uri.IsHexDigit(character))
            {
                return false;
            }
        }

        return true;
    }
}
