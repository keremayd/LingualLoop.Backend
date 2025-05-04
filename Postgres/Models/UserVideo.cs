using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Postgres.Models;

[Table("user_videos")]
public class UserVideo
{
    [Key]
    [Column("user_video_id")]
    public int UserVideoId { get; set; }

    [ForeignKey("User")]
    [Column("user_id")]
    public string UserId { get; set; }

    [ForeignKey("Video")]
    [Column("video_id")]
    public int VideoId { get; set; }

    [Column("saved_date")]
    public DateTime SavedDate { get; set; }

    // One-to-Many: Her kullanıcı birçok video kayıt edebilir.
    public User? User { get; set; }

    // One-to-Many: Bir video birçok kullanıcı tarafından kayıt edebilebilir.
    public Video? Video { get; set; }
}