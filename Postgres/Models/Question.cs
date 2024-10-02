using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Postgres.Models;

[Table("questions")]
public class Question
{
    [Key]
    [Column("question_id")]
    public int QuestionId { get; set; }

    [Column("question_text")]
    public string QuestionText { get; set; } = string.Empty;
    
    [Column("min_score")]
    public int MinScore { get; set; }
    
    [Column("max_score")]
    public int MaxScore { get; set; }

    // Foreign Key
    [Column("video_id")]
    public int VideoId { get; set; }

    // Navigation Property: Many Questions belong to one Video
    [ForeignKey("VideoId")]
    public Video? Video { get; set; }

    // One-to-Many: One Question can have many Answers
    public ICollection<Answer>? Answers { get; set; }
}
