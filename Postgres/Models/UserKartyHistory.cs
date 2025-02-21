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
    
    // One-to-Many: Her izleme kaydı bir kullanıcıya ait.
    public User? User { get; set; }

    // One-to-Many: Her izleme kaydı bir videoya ait.
    public Karty? Karty { get; set; }
}