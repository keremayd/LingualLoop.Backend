using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Postgres.Models;

/// Bir kullanıcının başka bir kullanıcıyı bildirmesi (uygunsuz ad, fotoğraf).
/// Elle incelenir; incelenince resolved_at yazılır.
[Table("user_report")]
public class UserReport
{
    [Key]
    [Column("user_report_id")]
    public int UserReportId { get; set; }

    [Column("reporter_id")]
    public string ReporterId { get; set; } = string.Empty;

    [Column("reported_id")]
    public string ReportedId { get; set; } = string.Empty;

    [Column("reason")]
    public string Reason { get; set; } = string.Empty;

    [Column("context")]
    public string Context { get; set; } = string.Empty;

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }

    [Column("resolved_at")]
    public DateTime? ResolvedAt { get; set; }
}
