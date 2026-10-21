namespace AriaHR.Modules.Identity.Application.Common;

public static class EmailNormalizer
{
    public static string? Normalize(string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return null;
        }

        return input.Trim().ToLowerInvariant();
    }
}
