using Postgres.Models;

namespace Service.Helpers;

/// <summary>
/// Bilet ekonomisinin tek doğruluk kaynağı. Tavan ve yenilenme aralığı hem
/// API handler'ları hem de Hangfire yenileme işi tarafından buradan okunur;
/// iki yerde ayrı sabit tutulursa ekonomi sessizce tutarsızlaşır.
///
/// Bilet oyunun giriş bedelidir (giriş 1 bilet + her yanlış cevap 1 bilet),
/// biriktirilen para değil. Tavan, pasif yenilenmenin durduğu noktadır;
/// görev ödülü tavanı aşamaz.
/// </summary>
public static class LivesRules
{
    /// <summary>
    /// Yeni kullanıcının ve mevcut satırların bilet tavanı. Premium ileride
    /// bu değeri kullanıcı bazında yükseltecek, bu yüzden tavan kodda sabit
    /// kabul edilmez; satırdaki max_lives kolonu asıldır, bu sabit yalnızca
    /// yeni satırların başlangıç değeridir.
    /// </summary>
    public const int DefaultMaxLives = 15;

    /// <summary>Bir biletin pasif yenilenme süresi.</summary>
    public static readonly TimeSpan RegenInterval = TimeSpan.FromHours(2);

    /// <summary>
    /// Bilet harcandıktan sonra yenilenme zamanlayıcısını gerekiyorsa başlatır.
    ///
    /// Zamanlayıcı yalnızca tavandan ilk düşüşte kurulur: kullanıcı zaten
    /// tavanın altındaysa saat çoktan işliyordur ve her harcamada sıfırlansa
    /// çok oynayan oyuncu hiç bilet kazanamazdı.
    /// </summary>
    public static void StartRegenTimerIfNeeded(UserLives userLives, int livesBeforeSpend, DateTime utcNow)
    {
        if (livesBeforeSpend >= userLives.MaxLives)
        {
            userLives.LastLivesResetTime = utcNow.Add(RegenInterval);
        }
    }
}
