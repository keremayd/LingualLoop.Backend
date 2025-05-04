using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Postgres.Models;

[Table("user_badges")]
public class UserBadge
{
    [Key]
    [Column("user_badge_id")]
    public int UserBadgeId { get; set; }

    [ForeignKey("User")]
    [Column("user_id")]
    public string UserId { get; set; }

    [ForeignKey("Badge")]
    [Column("badge_id")]
    public int BadgeId { get; set; }

    [Column("earned_date")]
    public DateTime EarnedDate { get; set; }

    // One-to-Many: Her kullanıcı birçok rozet kazanabilir.
    public User? User { get; set; }

    // One-to-Many: Bir rozet birçok kullanıcı tarafından kazanılabilir.
    public Badge? Badge { get; set; }
}