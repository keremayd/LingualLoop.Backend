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
    /// <summary>Aynı anda tutulabilecek en fazla seri koruma.</summary>
    public const int StreakFreezeCap = 2;

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
