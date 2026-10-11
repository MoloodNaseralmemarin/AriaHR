namespace AriaHR.Modules.Identity.Application.Common;

/// <summary>
/// Helper class for normalizing email addresses across the application.
/// </summary>
public static class EmailNormalizer
{
    /// <summary>
    /// Normalizes an email address by trimming whitespace, returning null if empty or whitespace-only,
    /// and converting non-empty emails to lowercase.
    /// </summary>
    /// <param name="email">The email address to normalize.</param>
    /// <returns>Normalized email in lowercase, or null if omitted/whitespace.</returns>
    public static string? Normalize(string? email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            return null;
        }

        return email.Trim().ToLowerInvariant();
    }
}
