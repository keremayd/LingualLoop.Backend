using System.Net;
using Common.Enums;
using Common.Exceptions;
using Common.Extensions;
using Microsoft.EntityFrameworkCore;
using Postgres;
using Postgres.Models;

namespace Service.Helpers;

/// <summary>
/// Dile bağlı içeriği yazar: kart anlamları ve ünite adları. Kart ekleme
/// ucu, toplu anlam ucu ve ünite ucu aynı kuralı paylaşsın diye tek yerde.
///
/// Kurallar: dil kodu desteklenmeli (<see cref="LocaleRules"/>), metin
/// kırpılır, **boş metin o dildeki satırı siler**. Bütün diller (Türkçe dahil)
/// aynı tablolarda; hiçbir dil ayrı kolonda değil. Kaydetmez: SaveChanges
/// çağıranın işi, ki kartla anlamı aynı işlemde yazılsın.
/// </summary>
public static class KartyTranslationWriter
{
    /// <summary>Anlam ve ünite adı için üst sınır; "çamaşır makinesi" gibi uzun karşılıklar rahat sığar.</summary>
    public const int MaxLength = 120;

    /// <summary>
    /// Gelen sözlüğü doğrular ve dil kodlarını kanonik hâle getirir
    /// ("SQ" → "sq"). Boş değer korunur (silme isteği). İstek gövdesi
    /// okunmadan önce çağrılır ki geçersiz dil kart satırı açılmadan
    /// reddedilsin.
    /// </summary>
    public static Dictionary<string, string> Normalize(IReadOnlyDictionary<string, string?>? values)
    {
        var result = new Dictionary<string, string>(StringComparer.Ordinal);
        if (values is null) return result;

        foreach (var (rawLocale, rawText) in values)
        {
            var locale = RequireLocale(rawLocale);
            var text = (rawText ?? string.Empty).Trim();
            if (text.Length > MaxLength)
            {
                throw PayloadInvalid(
                    $"{locale} metni {MaxLength} karakteri aşıyor: '{text[..30]}…'");
            }

            result[locale] = text;
        }

        return result;
    }

    public static string RequireLocale(string? code) =>
        LocaleRules.Normalize(code) ?? throw new LingualLoopException(
            ErrorCode.UnsupportedLanguage.CreateMessage(code, string.Join(", ", LocaleRules.Supported)),
            ErrorCode.UnsupportedLanguage.GetDescription(code, string.Join(", ", LocaleRules.Supported)),
            HttpStatusCode.BadRequest);

    /// <summary>
    /// Kartın verilen dillerdeki anlamlarını yazar (boş = sil). Kartın
    /// yazımdan sonraki bütün anlamlarını döner; çağıran yanıtta gösterir.
    /// </summary>
    public static async Task<Dictionary<string, string>> SetMeaningsAsync(
        LingualLoopContext context,
        int kartyId,
        IReadOnlyDictionary<string, string> meanings,
        CancellationToken cancellationToken)
    {
        var rows = await context.KartyTranslations
            .Where(row => row.KartyId == kartyId)
            .ToListAsync(cancellationToken);
        var now = DateTime.UtcNow;

        foreach (var (locale, meaning) in meanings)
        {
            var row = rows.FirstOrDefault(item => item.Locale == locale);
            if (meaning.Length == 0)
            {
                if (row is null) continue;
                context.KartyTranslations.Remove(row);
                rows.Remove(row);
            }
            else if (row is null)
            {
                row = new KartyTranslation
                {
                    KartyId = kartyId,
                    Locale = locale,
                    Meaning = meaning,
                    UpdatedAt = now,
                };
                context.KartyTranslations.Add(row);
                rows.Add(row);
            }
            else if (row.Meaning != meaning)
            {
                row.Meaning = meaning;
                row.UpdatedAt = now;
            }
        }

        return rows.ToDictionary(row => row.Locale, row => row.Meaning);
    }

    /// <summary>
    /// Ünite adlarını yazar: her dil (Türkçe dahil) <c>karty_unit_translation</c>'da
    /// bir satır, boş değer o dildeki adı siler. Ünitenin en az bir dilde adı
    /// kalmalı. Ünite kayıtlı olmalı (id gerekiyor). Ünitenin bütün dillerdeki
    /// adını döner.
    /// </summary>
    public static async Task<Dictionary<string, string>> SetUnitTitlesAsync(
        LingualLoopContext context,
        KartyUnit unit,
        IReadOnlyDictionary<string, string> titles,
        CancellationToken cancellationToken)
    {
        var rows = await context.KartyUnitTranslations
            .Where(row => row.KartyUnitId == unit.KartyUnitId)
            .ToListAsync(cancellationToken);
        var now = DateTime.UtcNow;

        foreach (var (locale, title) in titles)
        {
            var row = rows.FirstOrDefault(item => item.Locale == locale);
            if (title.Length == 0)
            {
                if (row is null) continue;
                context.KartyUnitTranslations.Remove(row);
                rows.Remove(row);
            }
            else if (row is null)
            {
                row = new KartyUnitTranslation
                {
                    KartyUnitId = unit.KartyUnitId,
                    Locale = locale,
                    Title = title,
                    UpdatedAt = now,
                };
                context.KartyUnitTranslations.Add(row);
                rows.Add(row);
            }
            else if (row.Title != title)
            {
                row.Title = title;
                row.UpdatedAt = now;
            }
        }

        if (rows.Count == 0) throw PayloadInvalid("Ünitenin en az bir dilde adı olmalı.");

        return rows.ToDictionary(row => row.Locale, row => row.Title);
    }

    private static LingualLoopException PayloadInvalid(string problem) =>
        new(
            ErrorCode.KartyPayloadInvalid.CreateMessage(problem),
            ErrorCode.KartyPayloadInvalid.GetDescription(problem),
            HttpStatusCode.BadRequest);
}
