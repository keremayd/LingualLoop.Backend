using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Postgres.Models;

[Table("user_streak")]
public class UserStreak
{
    [Key]
    [Column("user_streak_id")]
    public int UserStreakId { get; set; }

    [Column("user_id")]
    public string UserId { get; set; } = string.Empty;

    [Column("current_streak")]
    public int CurrentStreak { get; set; }

    [Column("longest_streak")]
    public int LongestStreak { get; set; }

    [Column("last_active_date")]
    public DateOnly LastActiveDate { get; set; }

    /// <summary>
    /// Elde tutulan seri koruma sayısı. Gün kaçırıldığında kaçırılan her gün
    /// için biri harcanır ve seri kaldığı yerden devam eder.
    /// </summary>
    [Column("freeze_count")]
    public int FreezeCount { get; set; }

    /// <summary>Korumaların en son tavana doldurulduğu gün.</summary>
    [Column("freeze_refilled_at")]
    public DateOnly? FreezeRefilledAt { get; set; }

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; }

    public User? User { get; set; }
}
