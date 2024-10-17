using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Postgres.Models;

[Table("videos")]
public class Video
{
    [Key]
    [Column("video_id")]
    public int VideoId { get; set; }

    [Column("video_url")]
    public string VideoUrl { get; set; } = string.Empty;
    
    [Column("video_title")]
    public string VideoTitle { get; set; } = string.Empty;
    
    [Column("video_description")]
    public string VideoDescription { get; set; } = string.Empty;

    // One-to-Many: One Video can have many Questions
    public ICollection<Question>? Questions { get; set; }

    public ICollection<UserVideoHistory>? UserVideoHistories { get; set; }
}
