using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Postgres.Models;

[Table("user_league_progress")]
public class UserLeagueProgress
{
    [Key]
    [Column("user_league_progress_id")]
    public int UserLeagueProgressId { get; set; }

    [Column("user_id")]
    public string UserId { get; set; } = string.Empty;

    [Column("season_key")]
    public int SeasonKey { get; set; }

    [Column("points")]
    public int Points { get; set; }

    /// <summary>
    /// Kullanıcıya son kez gösterilmiş lig seviyesi. Mevcut puanın seviyesi
    /// bunun üstündeyse mobilde bekleyen bir "lig atladın" sonucu vardır.
    /// </summary>
    [Column("announced_league_rank")]
    public int AnnouncedLeagueRank { get; set; }

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; }

    public User? User { get; set; }
}
