using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Postgres.Models;

[Table("badges")]
public class Badge
{
    [Key]
    [Column("badge_id")]
    public int? BadgeId { get; set; }

    [Column("badge_url")]
    public string BadgeUrl { get; set; } = string.Empty;

    [Column("badge_title")]
    public string BadgeTitle { get; set; } = string.Empty;

    [Column("badge_description")]
    public string BadgeDescription { get; set; } = string.Empty;

    [Column("created_date")]
    public DateTime CreatedDate { get; set; }
}