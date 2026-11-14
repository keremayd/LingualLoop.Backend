namespace Service.Helpers;

/// <summary>
/// Premium üyeliğin ve seri korumasının tek doğruluk kaynağı.
///
/// Premium bir bayrak değil bir **tarihtir**: abonelik biter, bayrak bitmez.
/// `premium_until` geçmişte kaldıysa kullanıcı artık premium değildir; ayrıca
/// bir iptal işlemi gerekmez.
/// </summary>
public static class PremiumRules
{
    /// <summary>Premium üyenin aynı anda tutabileceği en fazla seri koruma.</summary>
    public const int StreakFreezeCap = 2;

    /// <summary>
    /// Ücretsiz kullanıcının tutabileceği en fazla seri koruma (14 Kas 2026,
    /// kullanıcı kararı). Ücretsiz koruma emekle gelir: günün bütün görevleri
    /// bitince görevler ekranında toplanır (`QuestFreezeBonus`). Premium'un
    /// farkı emeksiz ve iki katı: her hafta kendiliğinden 2'ye dolar.
    /// </summary>
    public const int FreeStreakFreezeCap = 1;

    public static int FreezeCap(bool isPremium) =>
        isPremium ? StreakFreezeCap : FreeStreakFreezeCap;

    /// <summary>Premium üyenin korumalarının tavana dolma aralığı (gün).</summary>
    public const int FreezeRefillIntervalDays = 7;

    public static bool IsPremium(DateTime? premiumUntil, DateTime utcNow)
    {
        return premiumUntil.HasValue && premiumUntil.Value > utcNow;
    }

    /// <summary>
    /// Premium üyenin korumaları tavana dolmalı mı. Ayrı bir zamanlanmış iş
    /// gerekmez: dolum, kullanıcı zaten her açılışta çağırdığı günlük giriş
    /// akışında tembel olarak yapılır.
    /// </summary>
    public static bool ShouldRefill(DateOnly? lastRefill, DateOnly today)
    {
        if (lastRefill is null) return true;
        return today.DayNumber - lastRefill.Value.DayNumber >= FreezeRefillIntervalDays;
    }
}
