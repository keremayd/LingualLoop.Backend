using Microsoft.EntityFrameworkCore;
using Postgres;

namespace Service.Helpers;

/// <summary>
/// Günlük görev tanımları ve ilerleme hesabı. Görevler yeni tablo
/// gerektirmez; ilerleme mevcut oyun tablolarından (learning, history,
/// streak, daily activity) İstanbul gününe göre türetilir.
/// </summary>
public static class QuestRules
{
    public sealed record QuestDefinition(
        string Key,
        string Title,
        int Target,
        int RewardTickets);

    /// <summary>
    /// Ödüller bilet tavanıyla birlikte ayarlanır (bkz. LivesRules): günde
    /// toplam 10 bilet, tek seferde en fazla 3. Ödül tavanı aşamadığı için
    /// tek bir ödülün tavanın yanında büyük kalmaması gerekir; aksi hâlde
    /// tavana yakın oyuncunun ödülü boşa gider.
    /// </summary>
    public static readonly QuestDefinition[] DailyQuests =
    [
        new("checkin", "Güne başla", 1, 1),
        new("correct_five", "5 kelimeyi doğru bil", 5, 2),
        new("learn_three", "3 yeni kelime öğren", 3, 2),
        new("review_two", "2 rövanş kartını geri kazan", 2, 2),
        new("streak_three", "3 günlük seriye ulaş", 3, 3),
    ];

    public static QuestDefinition? Find(string questKey)
    {
        return DailyQuests.FirstOrDefault(q => q.Key == questKey);
    }

    // Gün sınırı tanımı `IstanbulDay`'e taşındı: görevlere özel bir kural
    // değil, uygulamanın her yerinde geçerli. Kart zamanlaması da "bugün kaç
    // yeni kelime verildi" diye soruyor; iki ayrı tanım er geç birbirinden
    // ayrılır. Aşağıdakiler mevcut çağıranları kırmamak için duran ince
    // sarmalayıcılar.

    public static DateOnly GetIstanbulDate(DateTime utcNow) =>
        IstanbulDay.DateOf(utcNow);

    public static int GetDayKey(DateTime utcNow) => IstanbulDay.KeyOf(utcNow);

    /// <summary>İstanbul gününün UTC cinsinden [başlangıç, bitiş) aralığı.</summary>
    public static (DateTime StartUtc, DateTime EndUtc) GetDayUtcRange(DateTime utcNow) =>
        IstanbulDay.UtcRangeOf(utcNow);

    public static async Task<int> GetProgressAsync(
        LingualLoopContext context,
        string userId,
        string questKey,
        DateTime utcNow,
        CancellationToken cancellationToken)
    {
        var (startUtc, endUtc) = GetDayUtcRange(utcNow);
        var today = GetIstanbulDate(utcNow);

        switch (questKey)
        {
            case "checkin":
                var checkedIn = await context.UserDailyActivities
                    .AsNoTracking()
                    .AnyAsync(
                        // Korunan gün giriş sayılmaz; görev gerçek girişi ister.
                        a => a.UserId == userId && a.ActivityDate == today && !a.Frozen,
                        cancellationToken);
                return checkedIn ? 1 : 0;

            case "correct_five":
                return await context.UserKartyLearnings
                    .AsNoTracking()
                    .CountAsync(
                        l => l.UserId == userId &&
                             l.LastCorrectDate >= startUtc &&
                             l.LastCorrectDate < endUtc,
                        cancellationToken);

            case "learn_three":
                return await context.UserKartyLearnings
                    .AsNoTracking()
                    .CountAsync(
                        l => l.UserId == userId &&
                             l.FirstLearnedDate >= startUtc &&
                             l.FirstLearnedDate < endUtc,
                        cancellationToken);

            case "review_two":
                return await context.UserKartyHistories
                    .AsNoTracking()
                    .CountAsync(
                        h => h.UserId == userId &&
                             h.ReviewedDate != null &&
                             h.ReviewedDate >= startUtc &&
                             h.ReviewedDate < endUtc,
                        cancellationToken);

            case "streak_three":
                var streak = await context.UserStreaks
                    .AsNoTracking()
                    .Where(s => s.UserId == userId)
                    .Select(s => (int?)s.CurrentStreak)
                    .FirstOrDefaultAsync(cancellationToken) ?? 0;
                return streak;

            default:
                return 0;
        }
    }
}
