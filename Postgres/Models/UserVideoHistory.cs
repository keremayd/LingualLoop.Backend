using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Postgres.Models;

[Table("user_video_history")]
public class UserVideoHistory
{
    [Key]
    [Column("user_video_history_id")]
    public int UserVideoHistoryId { get; set; }

    [ForeignKey("User")]
    [Column("user_id")]
    public int UserId { get; set; }

    [ForeignKey("Video")]
    [Column("video_id")]
    public int VideoId { get; set; }

    [Column("watched_date")]
    public DateTime WatchedDate { get; set; }  // DateTime olarak düzenlendi.

    [Column("watched_duration")]
    public int? WatchedDuration { get; set; }  // İzlenen süre saniye cinsinden olabilir.

    // One-to-Many: Her izleme kaydı bir kullanıcıya ait.
    public User? User { get; set; }

    // One-to-Many: Her izleme kaydı bir videoya ait.
    //public Video? Video { get; set; }
}
