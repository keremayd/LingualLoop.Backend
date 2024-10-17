using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Identity;

namespace Postgres.Models;

[Table("users")]
public class User : IdentityUser
{
    // [Key]
    // [Column("user_id")]
    // public int UserId { get; set; }

    [Column("user_nickname")]
    public string UserNickname { get; set; } = string.Empty;

    // One-to-Many: One User can have many UserScore
    public UserScore UserScore { get; set; } = new UserScore();

    public ICollection<UserVideoHistory>? VideoHistory { get; set; }
}
