using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Postgres.Models;

[Table("user_karty_history")]
public class UserKartyHistory
{
    [Key]
    [Column("user_karty_history_id")]
    public int UserKartyHistoryId { get; set; }
    
    [ForeignKey("User")]
    [Column("user_id")]
    public string UserId { get; set; }

    [ForeignKey("Karty")]
    [Column("karty_id")]
    public int KartyId { get; set; }

    [Column("wrong_count")]
    public int WrongCount { get; set; }

    [Column("last_wrong_date")]
    public DateTime LastWrongDate { get; set; }

    [Column("reviewed_date")]
    public DateTime? ReviewedDate { get; set; }
    
    public User? User { get; set; }

    public Karty? Karty { get; set; }
}
