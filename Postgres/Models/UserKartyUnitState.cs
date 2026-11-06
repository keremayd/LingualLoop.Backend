using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Postgres.Models;

/// <summary>
/// Kullanıcının Karty Yolu'nda açtığı üniteler.
///
/// Açılma bir **olay** olarak kaydedilir. Yalnız türetilseydi bir boşluk
/// kalıyordu: önceki ünite %80'e ulaşınca sıradaki açılıyor, ama yeni
/// üniteden ilk kelime gelmeden yanlış cevaplar oranı hedefin altına
/// indirirse kilit geri kapanıyordu.
///
/// İki sayı, lig terfisindeki <c>announced_league_rank</c> kalıbıyla:
/// açılan en ileri ünite ve kutlaması gösterilen en ileri ünite. İkisi
/// farklıysa patika bir kutlama bekletir.
/// </summary>
[Table("user_karty_unit_state")]
public class UserKartyUnitState
{
    [Key]
    [Column("user_id")]
    public string UserId { get; set; } = string.Empty;

    /// <summary>Hedefle açılmış en ileri ünitenin sırası (<c>karty_unit.sequence</c>).</summary>
    [Column("unlocked_sequence")]
    public int UnlockedSequence { get; set; }

    /// <summary>Kutlaması gösterilmiş en ileri ünitenin sırası.</summary>
    [Column("announced_sequence")]
    public int AnnouncedSequence { get; set; }

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; }
}
