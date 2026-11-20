using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Postgres.Models;

/// Bir kullanıcının engellediği kullanıcı. Engel iki yönlü işler: ikisi de
/// birbirini sıralamada görmez (App Store kuralı 1.2).
[Table("user_block")]
public class UserBlock
{
    [Key]
    [Column("user_block_id")]
    public int UserBlockId { get; set; }

    [Column("blocker_id")]
    public string BlockerId { get; set; } = string.Empty;

    [Column("blocked_id")]
    public string BlockedId { get; set; } = string.Empty;

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }
}
