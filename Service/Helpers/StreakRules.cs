namespace Service.Helpers;

/// <summary>
/// Seri hesabının ortak kuralları. Gün sınırı İstanbul saatine göredir —
/// görev sıfırlanması da (`QuestRules`) aynı günü kullanır ki kullanıcı için
/// "gün" her yerde aynı şey olsun.
/// </summary>
public static class StreakRules
{
    public static DateOnly GetIstanbulDate(DateTime utcNow)
    {
        TimeZoneInfo timeZone;
        try
        {
            timeZone = TimeZoneInfo.FindSystemTimeZoneById("Europe/Istanbul");
        }
        catch (TimeZoneNotFoundException)
        {
            timeZone = TimeZoneInfo.Utc;
        }

        return DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(utcNow, timeZone));
    }

    /// <summary>Son giriş ile bugün arasında kaç gün tamamen boş geçmiş.</summary>
    public static int MissedDaysBetween(DateOnly lastActive, DateOnly today)
    {
        return Math.Max(0, today.DayNumber - lastActive.DayNumber - 1);
    }

    public static bool CanCoverWithFreezes(int missedDays, int freezeCount)
    {
        return missedDays > 0 && freezeCount >= missedDays;
    }
}
