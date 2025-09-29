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
    public string UserId => Id;

    [Column("user_nickname")]
    public string UserNickname { get; set; } = string.Empty;
    
    [Column("firstname")]
    public string? FirstName { get; set; } = string.Empty;
    
    [Column("lastname")]
    public string? LastName { get; set; } = string.Empty;
    
    public string DisplayName => $"{FirstName} {LastName}";
    
    [Column("profilephoto")]
    public string? ProfilePhoto { get; set; } = string.Empty;
    
    public string RefreshToken { get; set; } = string.Empty;
    
    public DateTime RefreshTokenExpiryTime { get; set; }

    // One-to-Many: One User can have many UserScore
    public UserScore UserScore { get; set; } = new UserScore();

    public int UserRank;
        
    public ICollection<UserVideoHistory>? VideoHistory { get; set; }
    
    // One-to-Many: Bir kullanıcı birden fazla badge alabilir.
    public ICollection<UserBadge> UserBadges { get; set; } = new List<UserBadge>();
}
