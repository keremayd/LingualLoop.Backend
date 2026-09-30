namespace Service.Helpers;

/// <summary>
/// Uygulamanın **gün sınırı**. Tek doğruluk kaynağı.
///
/// Bu mantık daha önce <see cref="QuestRules"/> içinde duruyordu; görevlere
/// özel değil, uygulamanın her yerinde geçerli bir tanım. Kart zamanlaması da
/// "bugün kaç yeni kelime verildi" diye sorunca ikinci bir gün tanımı
/// yazmak gerekiyordu — iki tanım er geç birbirinden ayrılır ve gece
/// yarısında farklı davranırlar.
///
/// Gün **İstanbul saatine** göre kesilir, UTC'ye göre değil: kullanıcı
/// 00:30'da oynadığında bunun hangi güne yazıldığı onun takvimine göre
/// belirlenmeli.
/// </summary>
public static class IstanbulDay
{
    private static TimeZoneInfo GetTimeZone()
    {
        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById("Europe/Istanbul");
        }
        catch (TimeZoneNotFoundException)
        {
            // Zaman dilimi veritabanı olmayan ortamlarda (bazı konteynerler)
            // çökmek yerine UTC'ye düşülür.
            return TimeZoneInfo.Utc;
        }
    }

    public static DateOnly DateOf(DateTime utcNow)
    {
        return DateOnly.FromDateTime(
            TimeZoneInfo.ConvertTimeFromUtc(utcNow, GetTimeZone()));
    }

    /// <summary>Sıralanabilir gün anahtarı: <c>20260821</c>.</summary>
    public static int KeyOf(DateTime utcNow)
    {
        var date = DateOf(utcNow);
        return date.Year * 10000 + date.Month * 100 + date.Day;
    }

    /// <summary>İstanbul gününün UTC cinsinden [başlangıç, bitiş) aralığı.</summary>
    public static (DateTime StartUtc, DateTime EndUtc) UtcRangeOf(DateTime utcNow)
    {
        var timeZone = GetTimeZone();
        var localMidnight = DateOf(utcNow).ToDateTime(TimeOnly.MinValue);
        var startUtc = TimeZoneInfo.ConvertTimeToUtc(
            DateTime.SpecifyKind(localMidnight, DateTimeKind.Unspecified), timeZone);
        return (startUtc, startUtc.AddDays(1));
    }
}
