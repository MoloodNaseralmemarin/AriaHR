using Microsoft.AspNetCore.Http;

namespace AriaHR.Modules.Organization.Application.Helpers;

public static class ProfileImageValidator
{
    private const long MaxFileSizeInBytes = 2 * 1024 * 1024; // 2 MB

    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".jpg",
        ".jpeg",
        ".png",
        ".webp"
    };

    private static readonly HashSet<string> AllowedContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "image/jpeg",
        "image/pjpeg",
        "image/png",
        "image/webp"
    };

    public static async Task ValidateAsync(IFormFile? file, CancellationToken cancellationToken = default)
    {
        if (file == null || file.Length == 0)
        {
            return;
        }

        if (file.Length > MaxFileSizeInBytes)
        {
            throw new ArgumentException("حجم تصویر پروفایل نباید بیشتر از ۲ مگابایت باشد.", nameof(file));
        }

        string extension = Path.GetExtension(file.FileName);
        if (string.IsNullOrWhiteSpace(extension) || !AllowedExtensions.Contains(extension))
        {
            throw new ArgumentException("فرمت فایل تصویری پشتیبانی نمی‌شود. فرمت‌های مجاز: JPEG, PNG, WebP.", nameof(file));
        }

        if (string.IsNullOrWhiteSpace(file.ContentType) || !AllowedContentTypes.Contains(file.ContentType))
        {
            throw new ArgumentException("نوع محتوای فایل تصویری معتبر نمی‌باشد.", nameof(file));
        }

        // Validate magic bytes / signatures
        using var stream = file.OpenReadStream();
        byte[] header = new byte[12];
        int bytesRead = await stream.ReadAsync(header, 0, header.Length, cancellationToken);

        if (!IsValidImageHeader(header, bytesRead, extension))
        {
            throw new ArgumentException("محتوای فایل ارسالی با فرمت تصویر مطابقت ندارد.", nameof(file));
        }
    }

    private static bool IsValidImageHeader(byte[] header, int bytesRead, string extension)
    {
        if (bytesRead < 4)
        {
            return false;
        }

        string extLower = extension.ToLowerInvariant();

        // JPEG: FF D8 FF
        if (extLower == ".jpg" || extLower == ".jpeg")
        {
            return header[0] == 0xFF && header[1] == 0xD8 && header[2] == 0xFF;
        }

        // PNG: 89 50 4E 47 (89 'P' 'N' 'G')
        if (extLower == ".png")
        {
            return header[0] == 0x89 && header[1] == 0x50 && header[2] == 0x4E && header[3] == 0x47;
        }

        // WebP: RIFF ... WEBP (bytes 0-3 = "RIFF", bytes 8-11 = "WEBP")
        if (extLower == ".webp")
        {
            if (bytesRead < 12)
            {
                return false;
            }

            bool isRiff = header[0] == (byte)'R' && header[1] == (byte)'I' && header[2] == (byte)'F' && header[3] == (byte)'F';
            bool isWebp = header[8] == (byte)'W' && header[9] == (byte)'E' && header[10] == (byte)'B' && header[11] == (byte)'P';

            return isRiff && isWebp;
        }

        return false;
    }
}
