using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Postgres.Models;

/// <summary>
/// Ünitenin bir dildeki adı: tr "Evde", sq "Në shtëpi". Her dil, Türkçe dahil,
/// bir satır; ünitenin en az bir dilde adı olur (<c>KartyTranslationWriter</c>).
/// İstenen dilde ad yoksa sıra: varsayılan dil, sonra bulunan ilk dil
/// (<c>LocaleRules.Default</c>).
/// </summary>
[Table("karty_unit_translation")]
[PrimaryKey(nameof(KartyUnitId), nameof(Locale))]
public class KartyUnitTranslation
{
    [Column("karty_unit_id")]
    public int KartyUnitId { get; set; }

    /// <summary>ISO 639-1 ("tr", "sq"); bkz. <c>LocaleRules</c>.</summary>
    [Column("locale")]
    public string Locale { get; set; } = string.Empty;

    [Column("title")]
    public string Title { get; set; } = string.Empty;

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; }
}
