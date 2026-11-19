using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Postgres.Models;

/// <summary>
/// Kartın bir ana dildeki anlamı: "Tür" → tr "kapı", sq "derë". Kartın geri
/// kalanı (görsel, Almanca kelime, artikel, telaffuz) bütün diller için
/// ortak; dile bağlı tek içerik bu.
///
/// Türkçe dahil bütün diller bu tabloda: kartta hiç anlam kolonu olmadı.
/// Kart silinince satırları da silinir (<c>ON DELETE CASCADE</c>).
/// </summary>
[Table("karty_translation")]
[PrimaryKey(nameof(KartyId), nameof(Locale))]
public class KartyTranslation
{
    [Column("karty_id")]
    public int KartyId { get; set; }

    /// <summary>ISO 639-1 ("tr", "sq"); bkz. <c>LocaleRules</c>.</summary>
    [Column("locale")]
    public string Locale { get; set; } = string.Empty;

    [Column("meaning")]
    public string Meaning { get; set; } = string.Empty;

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; }
}
