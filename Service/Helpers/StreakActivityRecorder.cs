using Microsoft.EntityFrameworkCore;
using Postgres;
using Postgres.Models;

namespace Service.Helpers;

/// <summary>
/// Serinin **tek artış noktası**: kullanıcı gerçekten oynadığında çağrılır.
///
/// Önceden seri `RecordDailyActivityCommandHandler` içinde, yani uygulamanın
/// **açılışında** artıyordu. Bu, hiç oynamayan bir kullanıcının 30 günlük seri
/// biriktirmesi demekti — sayaç öğrenmeyi değil "uygulamayı açmayı"
/// ödüllendiriyordu. Artış buraya taşındı; açılış yalnızca giriş kaydı yazıyor
/// ve boşluk varsa koruma sorusunu soruyor.
///
/// "Oynadı" tanımı: Karty, Artikel ya da Rövanş'ta **en az bir cevap**
/// (doğru olması şart değil). Yalnız doğru cevap istenseydi, her şeyi yanlış
/// bilen ama gerçekten çalışan yeni kullanıcı cezalandırılırdı.
/// </summary>
public static class StreakActivityRecorder
{
    /// <summary>
    /// Bugünü "oynandı" olarak işaretler ve gerekiyorsa seriyi ilerletir.
    ///
    /// Aynı gün içinde defalarca çağrılabilir; ikinci çağrıdan sonrası
    /// etkisizdir. Çağıran tarafın ayrıca `SaveChangesAsync` çağırması
    /// gerekir — değişiklikler mevcut işleme katılsın diye burada kayıt
    /// yapılmıyor.
    /// </summary>
    public static async Task MarkPlayedAsync(
        LingualLoopContext context,
        string userId,
        DateTime utcNow,
        CancellationToken cancellationToken)
    {
        // Oyun akışı, kullanıcı hiç profil/açılış ucundan geçmeden de buraya
        // gelebilir; tablo ve `played` kolonu burada da garantilenir.
        await UserDailyActivitySchemaHelper.EnsureCreatedAsync(context, cancellationToken);

        var today = StreakRules.GetIstanbulDate(utcNow);

        await EnsurePlayedRowAsync(context, userId, today, utcNow, cancellationToken);

        var streak = await context.UserStreaks
            .FirstOrDefaultAsync(s => s.UserId == userId, cancellationToken);

        if (streak == null)
        {
            context.UserStreaks.Add(new UserStreak
            {
                UserId = userId,
                CurrentStreak = 1,
                LongestStreak = 1,
                LastActiveDate = today,
                UpdatedAt = utcNow,
            });
            return;
        }

        if (streak.LastActiveDate == today && streak.CurrentStreak > 0)
        {
            // Bugün zaten sayıldı.
            return;
        }

        if (streak.CurrentStreak == 0)
        {
            // Seri kırılmış ya da hiç başlamamış. Bu durumda `LastActiveDate`
            // yalnızca bir çıpadır, zincir anlamı taşımaz — bugün yeniden
            // birden başlar.
            streak.CurrentStreak = 1;
        }
        else if (streak.LastActiveDate == today.AddDays(-1))
        {
            streak.CurrentStreak += 1;
        }
        else
        {
            var missedDays = StreakRules.MissedDaysBetween(streak.LastActiveDate, today);

            // Koruma yetiyorsa seriye **dokunulmaz**: karar kullanıcınındır ve
            // açılışta sorulan soru hâlâ askıdadır. Oynamak, kararı kullanıcı
            // adına vermiş sayılmamalı.
            if (StreakRules.CanCoverWithFreezes(missedDays, streak.FreezeCount))
            {
                return;
            }

            streak.CurrentStreak = 1;
        }

        streak.LongestStreak = Math.Max(streak.LongestStreak, streak.CurrentStreak);
        streak.LastActiveDate = today;
        streak.UpdatedAt = utcNow;
    }

    /// <summary>
    /// Bugünün aktivite satırını bulur ya da oluşturur ve `played` işaretler.
    /// Kullanıcı uygulamayı hiç açmadan doğrudan oyuna girmiş olabilir
    /// (bildirimden derin bağlantı gibi), o yüzden satırın varlığına güvenilmez.
    /// </summary>
    private static async Task EnsurePlayedRowAsync(
        LingualLoopContext context,
        string userId,
        DateOnly today,
        DateTime utcNow,
        CancellationToken cancellationToken)
    {
        var activity = await context.UserDailyActivities
            .FirstOrDefaultAsync(
                a => a.UserId == userId && a.ActivityDate == today,
                cancellationToken);

        if (activity == null)
        {
            context.UserDailyActivities.Add(new UserDailyActivity
            {
                UserId = userId,
                ActivityDate = today,
                Played = true,
                CreatedAt = utcNow,
            });
            return;
        }

        if (!activity.Played)
        {
            activity.Played = true;
        }

        // Gün oynandıysa artık "korunmuş" sayılmaz; koruma boş günler içindir.
        if (activity.Frozen)
        {
            activity.Frozen = false;
        }
    }
}
