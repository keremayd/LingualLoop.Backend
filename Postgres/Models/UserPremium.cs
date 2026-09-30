using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Postgres.Models;

/// <summary>
/// Premium üyelik. Ayrı tablo, çünkü ileride plan tipi, mağaza makbuzu ve
/// yenileme bilgisi de buraya gelecek — `AspNetUsers`'a kolon eklemek yerine
/// projedeki diğer yan tablolarla (user_lives, user_scores, user_streak) aynı
/// deseni izler.
/// </summary>
[Table("user_premium")]
public class UserPremium
{
    [Key]
    [Column("user_premium_id")]
    public int UserPremiumId { get; set; }

    [Column("user_id")]
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// Üyeliğin bittiği an. Geçmişteyse kullanıcı premium değildir; ayrı bir
    /// iptal bayrağı tutulmaz.
    /// </summary>
    [Column("premium_until")]
    public DateTime? PremiumUntil { get; set; }

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; }
}
