using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Postgres.Models;

/// App Store aboneliği: Apple'ın özgün işlem kimliği hangi kullanıcıya bağlı,
/// ne zaman bitiyor. Premium'un kendisi user_premium.premium_until'de; bu tablo
/// yenileme ve iade bildirimlerini doğru kullanıcıya götürmek için.
[Table("apple_subscription")]
public class AppleSubscription
{
    [Key]
    [Column("original_transaction_id")]
    public string OriginalTransactionId { get; set; } = string.Empty;

    [Column("user_id")]
    public string UserId { get; set; } = string.Empty;

    [Column("product_id")]
    public string ProductId { get; set; } = string.Empty;

    /// Sandbox ya da Production. App Store incelemesi canlı uygulamada sandbox
    /// işlemiyle satın alır; ikisi de kabul edilir.
    [Column("environment")]
    public string Environment { get; set; } = string.Empty;

    [Column("expires_at")]
    public DateTime? ExpiresAt { get; set; }

    [Column("revoked_at")]
    public DateTime? RevokedAt { get; set; }

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; }
}
