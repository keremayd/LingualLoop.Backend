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

    [Column("created_date")]
    public DateTime CreatedDate { get; set; }
}
