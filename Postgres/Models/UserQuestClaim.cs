using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Postgres.Models;

[Table("user_quest_claim")]
public class UserQuestClaim
{
    [Key]
    [Column("user_quest_claim_id")]
    public int UserQuestClaimId { get; set; }

    [Column("user_id")]
    public string UserId { get; set; } = string.Empty;

    [Column("quest_key")]
    public string QuestKey { get; set; } = string.Empty;

    [Column("day_key")]
    public int DayKey { get; set; }

    [Column("claimed_at")]
    public DateTime ClaimedAt { get; set; }
}
