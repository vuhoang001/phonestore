using System.Globalization;
using System.Text;

namespace PhoneStore.Application.Common;

/// <summary>Sinh slug URL-friendly từ tên tiếng Việt (bỏ dấu).</summary>
public static class SlugHelper
{
    public static string Generate(string input)
    {
        if (string.IsNullOrWhiteSpace(input)) return string.Empty;

        // Chuẩn hóa và loại bỏ dấu tiếng Việt
        var normalized = input.Trim().ToLowerInvariant()
            .Replace('đ', 'd')
            .Normalize(NormalizationForm.FormD);

        var sb = new StringBuilder();
        foreach (var c in normalized)
        {
            var category = CharUnicodeInfo.GetUnicodeCategory(c);
            if (category == UnicodeCategory.NonSpacingMark) continue;
            if (char.IsLetterOrDigit(c)) sb.Append(c);
            else if (char.IsWhiteSpace(c) || c is '-' or '_') sb.Append('-');
        }

        var slug = sb.ToString().Normalize(NormalizationForm.FormC);
        while (slug.Contains("--")) slug = slug.Replace("--", "-");
        return slug.Trim('-');
    }
}
