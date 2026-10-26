using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace Techies.Web.Infrastructure;

public static partial class Slug
{
    // "Điện thoại Samsung" -> "dien-thoai-samsung"
    public static string From(string text)
    {
        // đ/Đ is a separate letter, not d + a combining mark, so normalization alone misses it.
        var decomposed = text.Replace('đ', 'd').Replace('Đ', 'D').Normalize(NormalizationForm.FormD);
        var plain = new StringBuilder(decomposed.Length);
        foreach (var c in decomposed)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
                plain.Append(c);
        }
        return NonAlphanumeric().Replace(plain.ToString().ToLowerInvariant(), "-").Trim('-');
    }

    [GeneratedRegex("[^a-z0-9]+")]
    private static partial Regex NonAlphanumeric();
}
