using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace TravelAI.Services;

public static class AIText
{
    public static string Normalize(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return "";
        var result = new StringBuilder();
        foreach (var c in value.ToLowerInvariant().Normalize(NormalizationForm.FormD))
            if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
                result.Append(c == 'đ' ? 'd' : c);
        return Regex.Replace(result.ToString(), @"[^a-z0-9]+", " ").Trim();
    }
    public static string Money(decimal value) => value.ToString("N0", CultureInfo.GetCultureInfo("vi-VN")) + " ₫";
    public static string Short(string? value, int limit = 350) =>
        string.IsNullOrWhiteSpace(value) ? "Chưa có mô tả" : value.Length <= limit ? value : value[..limit] + "…";
    public static string Markdown(string? value) => (value ?? "").Replace("\\", "\\\\")
        .Replace("[", "\\[").Replace("]", "\\]").Replace("*", "\\*").Replace("|", "\\|")
        .Replace("\r", " ").Replace("\n", " ");
}
