namespace Service.Helpers;

/// <summary>
/// Kullanıcının ana dili: arayüzün, ünite adlarının ve kart anlamlarının
/// dili. Öğrenilen dil her zaman Almanca; değişen, kelimenin hangi dilde
/// açıklandığı (16 Kas 2026: Türkçe ve Arnavutça).
///
/// İstemci dili her istekte <c>Accept-Language</c> başlığıyla bildirir.
/// Başlığı göndermeyen eski sürüm varsayılan dili (Türkçe) alır; yani mevcut
/// kullanıcı için hiçbir şey değişmez.
///
/// Dil, controller'da başlıktan okunup isteğe <c>Locale</c> olarak konur;
/// handler'lar ASP.NET'i tanımıyor (bkz. <c>CreateKartyRequest</c>).
/// </summary>
public static class LocaleRules
{
    /// <summary>
    /// Varsayılan dil. Saklamada hiçbir dil ayrıcalıklı değil: ünite adları ve
    /// kart anlamları bütün dillerde (Türkçe dahil) aynı çeviri tablolarında
    /// (<c>karty_unit_translation</c>, <c>karty_translation</c>). Varsayılan
    /// yalnız iki durumda kullanılır: istek dil bildirmiyorsa (eski sürümler
    /// yalnız Türkçe konuşuyor) ve ünitenin istenen dilde adı yoksa (sırası:
    /// istenen dil, varsayılan dil, bulunan ilk dil). Kart anlamı başka dile
    /// düşmez: anlamı olmayan dilde null döner.
    /// </summary>
    public const string Default = "tr";

    /// <summary>Desteklenen ana diller (ISO 639-1). Yeni dil buraya eklenir.</summary>
    public static readonly IReadOnlyList<string> Supported = new[] { "tr", "sq" };

    /// <summary>
    /// Desteklenen dil kodu ("sq", "SQ", "sq-XK", "sq_AL" → "sq"); değilse
    /// <c>null</c>.
    /// </summary>
    public static string? Normalize(string? code)
    {
        if (string.IsNullOrWhiteSpace(code)) return null;

        var primary = code.Trim().Split('-', '_')[0].ToLowerInvariant();
        return Supported.Contains(primary) ? primary : null;
    }

    /// <summary>
    /// <c>Accept-Language</c> başlığındaki ilk desteklenen dil
    /// ("sq-XK,sq;q=0.9,en;q=0.8" → "sq"). Başlık yoksa ya da hiçbiri
    /// desteklenmiyorsa varsayılan dil. Ağırlıklar (q) okunmuyor: istemci tek
    /// dil gönderiyor, tarayıcılar da dilleri tercih sırasıyla yazıyor.
    /// </summary>
    public static string FromAcceptLanguage(string? header)
    {
        if (string.IsNullOrWhiteSpace(header)) return Default;

        foreach (var part in header.Split(','))
        {
            var locale = Normalize(part.Split(';')[0]);
            if (locale is not null) return locale;
        }

        return Default;
    }
}
