using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Postgres.Models;

[Table("karty")]
public class Karty
{
    [Key]
    [Column("karty_id")]
    public int KartyId { get; set; }

    [Column("question_text")]
    public string QuestionText { get; set; } = string.Empty;
    
    [Column("correct_text")]
    public string CorrectText { get; set; } = string.Empty;

    [Column("noun_text")]
    public string NounText { get; set; } = string.Empty;

    [Column("article")]
    public string Article { get; set; } = string.Empty;
    
    [Column("karty_url")]
    public string KartyUrl { get; set; } = string.Empty;
    
    [Column("is_correct")]
    public bool IsCorrect { get; set; }

    /// <summary>
    /// Eski zorluk bandı (<c>max_score</c> ile birlikte). 4 Kas 2026'dan beri
    /// okunmuyor: kelimenin ne zaman geleceğini ünitesi ve sırası belirliyor.
    /// Yeni kartlarda 0; istemci modeli alanı beklediği için yanıtta duruyor.
    /// </summary>
    [Column("min_score")] 
    public int MinScore { get; set; }

    [Column("max_score")]
    public int MaxScore { get; set; }

    /// <summary>
    /// Telaffuz sesinin S3 yolu. Ses henüz üretilmediyse <c>null</c>; o
    /// kartta telaffuz butonu gösterilmez.
    ///
    /// İstemciye imzalı adres olarak gittiği için adı `AudioUrl`; kolon da
    /// aynı adı taşıyor ki katmanlar arasında tek isim olsun.
    /// </summary>
    [Column("audio_url")]
    public string? AudioUrl { get; set; }

    /// <summary>
    /// Kartın Karty Yolu'ndaki ünitesi (<see cref="KartyUnit"/>). Yeni kelime
    /// artık puan bandından rastgele değil, ünite sırasıyla geliyor.
    /// </summary>
    [Column("unit_id")]
    public int? UnitId { get; set; }

    /// <summary>Ünite içindeki sırası; aynı sıradakiler <c>karty_id</c> ile ayrılır.</summary>
    [Column("unit_order")]
    public int? UnitOrder { get; set; }

    [Column("created_date")]
    public DateTime CreatedDate { get; set; }
}
