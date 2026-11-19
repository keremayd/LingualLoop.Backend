using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Postgres.Models;

/// <summary>
/// Kullanıcının tercihleri. Bugün yalnız ana dil: uygulama içerik dilini her
/// istekte başlıkla bildiriyor, bu satır ise seçimin kaydı (hangi dilden kaç
/// kullanıcı geldiğini ölçmek, ileride sunucunun gönderdiği metinler).
///
/// Satırı olmayan kullanıcı varsayılan dili (Türkçe) kullanıyor demektir.
/// Hesap silinince satır da silinir (<c>AspNetUsers</c>'a CASCADE).
/// </summary>
[Table("user_settings")]
public class UserSettings
{
    [Key]
    [Column("user_id")]
    public string UserId { get; set; } = string.Empty;

    /// <summary>ISO 639-1 ("tr", "sq"); bkz. <c>LocaleRules</c>.</summary>
    [Column("native_language")]
    public string NativeLanguage { get; set; } = string.Empty;

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; }
}
