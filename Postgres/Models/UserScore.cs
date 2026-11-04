using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Postgres.Models;

[Table("user_scores")]
public class UserScore
{
    [Key]
    [Column("user_score_id")]
    public int UserScoreId { get; set; }

    /// <summary>
    /// Eski zorluk termostatı. Doğru cevapta artıp yanlışta azalıyor ve
    /// kartın hangi zorluk bandından geleceğini seçiyordu. 4 Kas 2026'dan
    /// beri **ne yazılıyor ne okunuyor**: içerik Karty Yolu üniteleriyle
    /// açılıyor (<c>KartyUnitRules</c>). Kolon, API yanıtlarındaki alan ve
    /// eski satırlar yüzünden duruyor; yeni bir işe bağlanmamalı.
    /// </summary>
    [Column("score")]
    public int Score { get; set; }

    /// <summary>
    /// İlerleme sayacı. Yalnızca doğru cevapta artar, yanlışta hiç değişmez.
    /// Kullanıcıya gösterilen sayı budur: kazanılan bir şey geri alınmaz.
    ///
    /// Skor tek başınayken iki işi birden yapıyordu — zorluk seçmek için
    /// düşmesi, ilerlemeyi göstermek için düşmemesi gerekiyordu. İkisi
    /// ayrıldı.
    /// </summary>
    [Column("experience")]
    public int Experience { get; set; }

    // Foreign Keys
    [ForeignKey("UserId")]
    [Column("user_id")]
    public string UserId { get; set; }

    //public User? User { get; set; }
    // [Column("answer_id")]
    // public int AnswerId { get; set; }

    // Navigation Properties
    // [ForeignKey("UserId")]
    // public User? User { get; set; }

    // [ForeignKey("AnswerId")]
    // public Answer? Answer { get; set; }
}
