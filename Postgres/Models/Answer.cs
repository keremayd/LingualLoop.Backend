using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Postgres.Models;

[Table("answers")]
public class Answer
{
    [Key]
    [Column("answer_id")]
    public int AnswerId { get; set; }

    [Column("answer_text")]
    public string AnswerText { get; set; } = string.Empty;

    [Column("is_correct")]
    public bool IsCorrect { get; set; }

    // Foreign Key
    [Column("question_id")]
    public int QuestionId { get; set; }

    // Navigation Property: Many Answers belong to one Question
    // [ForeignKey("QuestionId")]
    // public Question? Question { get; set; }
}
