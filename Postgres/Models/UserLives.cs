using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Postgres.Models;

[Table("user_lives")]
public class UserLives
{
    [Key]
    [Column("user_lives_id")]
    public int UserLivesId { get; set; }

    [ForeignKey("UserId")]
    [Column("user_id")]
    public string UserId { get; set; }

    public User User { get; set; }

    [Column("lives")]
    public int Lives { get; set; }

    [Column("max_lives")]
    public int MaxLives { get; set; }

    [Column("last_lives_reset_time")]
    public DateTime LastLivesResetTime { get; set; }
}