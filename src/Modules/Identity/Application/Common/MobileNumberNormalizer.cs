using System.Text;

namespace AriaHR.Modules.Identity.Application.Common;

public static class MobileNumberNormalizer
{
    public static string Normalize(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return string.Empty;
        }

        string trimmed = input.Trim();

        // Convert Persian/Arabic digits to English digits
        var sb = new StringBuilder();
        foreach (char c in trimmed)
        {
            if (c >= '۰' && c <= '۹')
            {
                sb.Append((char)('0' + (c - '۰')));
            }
            else if (c >= '٠' && c <= '٩')
            {
                sb.Append((char)('0' + (c - '٠')));
            }
            else
            {
                sb.Append(c);
            }
        }

        string normalized = sb.ToString();

        // Remove non-digit characters except leading '+' if present initially
        if (normalized.StartsWith("+98"))
        {
            normalized = "0" + normalized.Substring(3);
        }
        else if (normalized.StartsWith("0098"))
        {
            normalized = "0" + normalized.Substring(4);
        }
        else if (normalized.StartsWith("98") && normalized.Length == 12)
        {
            normalized = "0" + normalized.Substring(2);
        }

        // Keep only digits
        string digitsOnly = new string(normalized.Where(char.IsDigit).ToArray());

        return digitsOnly;
    }
}
