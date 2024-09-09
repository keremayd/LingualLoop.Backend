using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Postgres.Models;

[Table("user_scores")]
public class UserScore
{
    [Key]
    [Column("user_score_id")]
    public int UserScoreId { get; set; }

    [Column("score")]
    public int Score { get; set; }

    // Foreign Keys
    [ForeignKey("UserId")]
    [Column("user_id")]
    public int UserId { get; set; }

    // [Column("answer_id")]
    // public int AnswerId { get; set; }

    // // Navigation Properties
    // [ForeignKey("UserId")]
    // public User? User { get; set; }

    // [ForeignKey("AnswerId")]
    // public Answer? Answer { get; set; }
}
