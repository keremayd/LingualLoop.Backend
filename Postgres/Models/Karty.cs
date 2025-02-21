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
    
    [Column("karty_url")]
    public string KartyUrl { get; set; } = string.Empty;
    
    [Column("is_correct")]
    public bool IsCorrect { get; set; }

    [Column("min_score")] 
    public int MinScore { get; set; }

    [Column("max_score")]
    public int MaxScore { get; set; }

    [Column("created_date")]
    public DateTime CreatedDate { get; set; }
}