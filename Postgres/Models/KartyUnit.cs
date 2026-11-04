using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Postgres.Models;

/// <summary>
/// Karty Yolu'nun ünitesi: konulu bir kelime grubu ("Evde", "Yolculuk").
/// Kelimeler <see cref="Karty.UnitId"/> ve <see cref="Karty.UnitOrder"/> ile
/// bağlanır; sıradaki ünite, bir öncekinin kelimeleri pekişince açılır
/// (<c>KartyUnitRules</c>).
/// </summary>
[Table("karty_unit")]
public class KartyUnit
{
    [Key]
    [Column("karty_unit_id")]
    public int KartyUnitId { get; set; }

    /// <summary>Patikadaki sırası; 1'den başlar, benzersiz.</summary>
    [Column("sequence")]
    public int Sequence { get; set; }

    [Column("title")]
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Ünite başlığındaki 3B görsel olarak kullanılan kart. Ayrı bir görsel
    /// yüklemek gerekmesin diye ünitenin kendi kartlarından biri seçilir.
    /// </summary>
    [Column("emblem_karty_id")]
    public int? EmblemKartyId { get; set; }

    [Column("created_date")]
    public DateTime CreatedDate { get; set; }
}
