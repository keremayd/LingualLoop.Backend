using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Postgres.Models;

[Table("user_daily_activity")]
public class UserDailyActivity
{
    [Key]
    [Column("user_daily_activity_id")]
    public int UserDailyActivityId { get; set; }

    [Column("user_id")]
    public string UserId { get; set; } = string.Empty;

    [Column("activity_date")]
    public DateOnly ActivityDate { get; set; }

    /// <summary>
    /// Bu gün gerçekten girilmedi, seri koruma ile kapatıldı.
    ///
    /// Ayrı bir bayrak tutuluyor çünkü "korundu" bilgisi seri uzunluğundan
    /// çıkarılamaz: seri sayacı ile aktivite satırları ayrı ayrı yazıldığı
    /// için aralarında boşluk oluşabiliyor ve boşluğun tamamı yanlışlıkla
    /// "korunmuş gün" gibi okunuyordu.
    /// </summary>
    [Column("frozen")]
    public bool Frozen { get; set; }

    /// <summary>
    /// Bu gün gerçekten **oynandı** mı: Karty, Artikel ya da Rövanş'ta en az
    /// bir cevap verildi mi.
    ///
    /// Satırın var olması yalnızca uygulamanın açıldığını söyler; seri bunun
    /// üzerine kurulamaz, yoksa "uygulamayı açmak" ile "öğrenmek" aynı şey
    /// sayılır. `checkin` görevi girişi ister (satırın varlığı), seri ise
    /// oynamayı ister (bu bayrak) — iki ayrı soru, iki ayrı alan.
    ///
    /// Oynanan gün, korunan gün gibi **tahmin edilmez, kaydedilir**:
    /// `user_karty_learning.last_correct_date` kart başına yalnız son tarihi
    /// tuttuğu için üzerine yazıldıkça geçmiş kayboluyor.
    /// </summary>
    [Column("played")]
    public bool Played { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }

    public User? User { get; set; }
}
