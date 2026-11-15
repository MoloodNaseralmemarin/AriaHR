using AriaHR.Modules.Organization.Application.Options;

namespace AriaHR.Modules.Organization.Application.Validators;

public static class WorkLocationInputValidator
{
    public const int MaxNameLength = 200;

    public static string ValidateName(string? name)
    {
        string normalized = (name ?? string.Empty).Trim();
        if (normalized.Length == 0)
        {
            throw new ArgumentException("نام محل کار الزامی است.");
        }

        if (normalized.Length > MaxNameLength)
        {
            throw new ArgumentException($"نام محل کار نمی‌تواند بیشتر از {MaxNameLength} کاراکتر باشد.");
        }

        return normalized;
    }

    public static void ValidateCoordinates(decimal latitude, decimal longitude)
    {
        if (latitude < -90m || latitude > 90m)
        {
            throw new ArgumentException("عرض جغرافیایی باید بین ۹۰- و ۹۰ باشد.");
        }

        if (longitude < -180m || longitude > 180m)
        {
            throw new ArgumentException("طول جغرافیایی باید بین ۱۸۰- و ۱۸۰ باشد.");
        }
    }

    public static void ValidateRadius(decimal allowedRadiusInMeters, QrCodeOptions options)
    {
        if (options == null)
        {
            throw new ArgumentNullException(nameof(options));
        }

        if (allowedRadiusInMeters < options.MinAllowedRadiusMeters)
        {
            throw new ArgumentException($"شعاع محدوده باید حداقل {options.MinAllowedRadiusMeters} متر باشد.");
        }

        if (allowedRadiusInMeters > options.MaxAllowedRadiusMeters)
        {
            throw new ArgumentException($"شعاع محدوده نمی‌تواند بیشتر از {options.MaxAllowedRadiusMeters} متر باشد.");
        }
    }

    public static int ValidateRotationInterval(int? requestedMinutes, QrCodeOptions options)
    {
        if (options == null)
        {
            throw new ArgumentNullException(nameof(options));
        }

        int min = options.MinRotationIntervalMinutes > 0 ? options.MinRotationIntervalMinutes : 1;
        int max = options.MaxRotationIntervalMinutes >= min ? options.MaxRotationIntervalMinutes : min;

        if (!requestedMinutes.HasValue)
        {
            return options.ClampRotationInterval(null);
        }

        if (requestedMinutes.Value < min || requestedMinutes.Value > max)
        {
            throw new ArgumentException($"بازه چرخش کد کیوآر باید بین {min} و {max} دقیقه باشد.");
        }

        return requestedMinutes.Value;
    }
}
