using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace Common.Extensions;

public static class StringExtensions
{
    /// <summary>
    /// Verilen ismi önce ASCII-only, alfasayısal bir "slug"a çevirir; 
    /// sonra sonuna <paramref name="numericSuffixLength"/> uzunluğunda rastgele rakam ekler.
    /// </summary>
    /// <param name="source">Örneğin "Kerem Aydın"</param>
    /// <param name="numericSuffixLength">Eklenecek rakam uzunluğu (default 5)</param>
    /// <returns>örneğin: "keremaydin12345678"</returns>
    public static string ToSluggedUsername(this string source, int numericSuffixLength = 5)
    {
        if (string.IsNullOrWhiteSpace(source))
            source = "user";

        // 0) Türkçe karakter eşlemeleri
        source = source
            .Replace('ğ', 'g')
            .Replace('Ğ', 'G')
            .Replace('ü', 'u')
            .Replace('Ü', 'U')
            .Replace('ş', 's')
            .Replace('Ş', 'S')
            .Replace('ı', 'i')   // BURASI kritik: dotless i -> i
            .Replace('İ', 'I')
            .Replace('ö', 'o')
            .Replace('Ö', 'O')
            .Replace('ç', 'c')
            .Replace('Ç', 'C');

        // 1) Normalize edip diakritikleri ayır
        var normalized = source.Normalize(NormalizationForm.FormKD);

        // 2) Non-spacing marks (diakritikler) çıkar
        var sb = new StringBuilder();
        foreach (var ch in normalized)
        {
            var uc = CharUnicodeInfo.GetUnicodeCategory(ch);
            if (uc != UnicodeCategory.NonSpacingMark)
                sb.Append(ch);
        }

        // 3) ASCII-only bırak, harf+rakam dışındakileri at, küçült
        var ascii = sb
            .ToString()
            .Normalize(NormalizationForm.FormC);

        // sadece a–z, A–Z, 0–9
        var slug = Regex.Replace(ascii, @"[^a-zA-Z0-9]", "")
            .ToLowerInvariant();

        if (string.IsNullOrEmpty(slug))
            slug = "user";

        // 4) Sonuna rastgele rakamlar ekle
        var rnd = new Random();
        var max = (int)Math.Pow(10, numericSuffixLength);
        var suffix = rnd.Next(0, max)
            .ToString($"D{numericSuffixLength}");

        return slug + suffix;
    }
}