using Microsoft.EntityFrameworkCore;
using Postgres;

namespace Service.Helpers;

/// <summary>
/// Günlük görevler: o gün hangileri verilecek ve ilerleme nerede. Görevler
/// yeni tablo gerektirmez; ilerleme mevcut oyun tablolarından (learning,
/// history, streak, daily activity) İstanbul gününe göre türetilir.
///
/// ## Görev kullanıcının durumuna bakar
///
/// Liste eskiden herkese aynıydı ve bazı görevler o gün yapılamıyordu: bekleyen
/// rövanş kartı yoksa "2 rövanş kartını geri kazan" ancak bilerek yanlış
/// yapılarak biterdi; "3 günlük seriye ulaş" ilk iki gün bitemez, sonrasında
/// oynamadan da tamam görünürdü. Artık her yuvada öncelik sıralı seçenekler
/// var ve o gün yapılabilen ilki verilir. Ödüller yuvaya aittir, seçenek
/// değişince günlük toplam değişmez.
/// </summary>
public static class QuestRules
{
    public sealed record QuestDefinition(
        string Key,
        string Title,
        int Target,
        int RewardTickets);

    /// <summary>Görev seçiminin baktığı durum.</summary>
    /// <param name="NewWords">Bugün tanışılabilecek yeni kelime: açık ünitelerde görülmemişler + bugün tanışılanlar.</param>
    /// <param name="ReachableWords">Bugün doğru bilinebilecek kelime: tanışılanlar + açık ünitelerde görülmemişler.</param>
    /// <param name="ArticleWords">Bugün artikeli sorulabilecek kelime.</param>
    /// <param name="PendingReviews">Günün başında bekleyen rövanş kartı. Bugün yapılan yanlışlar sayılmaz: görev yanlış yaptırmaya çalışmamalı.</param>
    /// <param name="HadStreak">Bugünden önce süren bir seri var mı.</param>
    public sealed record QuestDayState(
        int NewWords,
        int ReachableWords,
        int ArticleWords,
        int PendingReviews,
        bool HadStreak);

    // Ödüller bilet tavanıyla birlikte ayarlanır (bkz. LivesRules): günde
    // toplam 10 bilet, tek seferde en fazla 3. Ödül tavanı aşamadığı için tek
    // bir ödülün tavanın yanında büyük kalmaması gerekir; aksi hâlde tavana
    // yakın oyuncunun ödülü boşa gider.
    private static readonly QuestDefinition CheckIn = new("checkin", "Güne başla", 1, 1);
    private static readonly QuestDefinition CorrectFive = new("correct_five", "5 kelimeyi doğru bil", 5, 2);
    private static readonly QuestDefinition LearnThree = new("learn_three", "3 yeni kelime öğren", 3, 2);
    private static readonly QuestDefinition CorrectEight = new("correct_eight", "8 kelimeyi doğru bil", 8, 2);
    private static readonly QuestDefinition ArticleThree = new("article_three", "3 artikeli doğru bil", 3, 2);
    private static readonly QuestDefinition CorrectTen = new("correct_ten", "10 kelimeyi doğru bil", 10, 2);

    /// <summary>Rövanş görevinin en yüksek hedefi; bekleyen kart azsa onunla sınırlıdır.</summary>
    private const int MaxReviewTarget = 2;

    private static QuestDefinition ReviewQuest(int pendingReviews)
    {
        var target = Math.Clamp(pendingReviews, 1, MaxReviewTarget);
        return new QuestDefinition("review_two", $"{target} rövanş kartını geri kazan", target, 2);
    }

    private static QuestDefinition StreakQuest(bool hadStreak) =>
        new("streak_keep", hadStreak ? "Serini sürdür" : "Serini başlat", 1, 3);

    /// <summary>
    /// Günün görevleri, yuva sırasıyla. Ödülü alınmış seçenek durum değişse de
    /// yerinde kalır; hiçbir seçeneği yapılamayan yuva o gün verilmez.
    /// </summary>
    public static IReadOnlyList<QuestDefinition> ChooseForDay(
        QuestDayState day,
        IReadOnlyCollection<string> claimedToday)
    {
        (QuestDefinition Quest, bool Doable)[][] slots =
        [
            [(CheckIn, true)],
            [(CorrectFive, day.ReachableWords >= CorrectFive.Target)],
            [
                (LearnThree, day.NewWords >= LearnThree.Target),
                (CorrectEight, day.ReachableWords >= CorrectEight.Target),
            ],
            [
                (ReviewQuest(day.PendingReviews), day.PendingReviews > 0),
                (ArticleThree, day.ArticleWords >= ArticleThree.Target),
                (CorrectTen, day.ReachableWords >= CorrectTen.Target),
            ],
            [(StreakQuest(day.HadStreak), true)],
        ];

        var chosen = new List<QuestDefinition>();
        foreach (var slot in slots)
        {
            var claimed = slot.Where(option => claimedToday.Contains(option.Quest.Key));
            var doable = slot.Where(option => option.Doable);
            var pick = claimed.Concat(doable).Select(option => option.Quest).FirstOrDefault();
            if (pick is not null) chosen.Add(pick);
        }

        return chosen;
    }

    public static async Task<QuestDayState> ReadDayStateAsync(
        LingualLoopContext context,
        string userId,
        DateTime utcNow,
        CancellationToken cancellationToken)
    {
        var (startUtc, endUtc) = GetDayUtcRange(utcNow);
        var today = GetIstanbulDate(utcNow);

        var units = await KartyUnitProgressReader.ReadAsync(context, userId, cancellationToken);
        var unseenOpen = units
            .Where(unit => unit.Unlocked)
            .Sum(unit => Math.Max(0, unit.Progress.WordCount - unit.Progress.SeenCount));

        var learnings = await context.UserKartyLearnings
            .AsNoTracking()
            .Where(l => l.UserId == userId)
            .Select(l => new
            {
                l.CorrectCount,
                l.ArticleCorrectCount,
                l.FirstLearnedDate,
                l.LastArticleAnsweredDate,
            })
            .ToListAsync(cancellationToken);

        var introducedToday = learnings.Count(
            l => l.FirstLearnedDate >= startUtc && l.FirstLearnedDate < endUtc);
        // Henüz doğru bilinmemiş kelime bugün bilinip Artikel'e düşebilir.
        var articleOpen = learnings.Count(
            l => l.CorrectCount == 0 ||
                 l.ArticleCorrectCount < KartyLeitner.ArticleLearningGoal ||
                 (l.LastArticleAnsweredDate >= startUtc && l.LastArticleAnsweredDate < endUtc));

        // Bugün yeniden yanlış bilinen kartın tarihi ileri gittiği için sayı
        // gün içinde yalnız düşebilir; rövanş hedefi yükselip bitmiş görevi
        // geri almaz.
        var pendingReviews = await context.UserKartyHistories
            .AsNoTracking()
            .CountAsync(
                h => h.UserId == userId &&
                     h.LastWrongDate < startUtc &&
                     (h.ReviewedDate == null || h.ReviewedDate >= startUtc),
                cancellationToken);

        var streak = await context.UserStreaks
            .AsNoTracking()
            .Where(s => s.UserId == userId)
            .Select(s => new { s.CurrentStreak, s.LastActiveDate })
            .FirstOrDefaultAsync(cancellationToken);
        var hadStreak = streak is not null &&
                        (streak.LastActiveDate == today
                            ? streak.CurrentStreak > 1
                            : streak.CurrentStreak > 0);

        return new QuestDayState(
            NewWords: unseenOpen + introducedToday,
            ReachableWords: learnings.Count + unseenOpen,
            ArticleWords: articleOpen + unseenOpen,
            PendingReviews: pendingReviews,
            HadStreak: hadStreak);
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
            case "correct_eight":
            case "correct_ten":
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

            case "article_three":
                return await context.UserKartyLearnings
                    .AsNoTracking()
                    .CountAsync(
                        l => l.UserId == userId &&
                             l.LastArticleAnsweredDate >= startUtc &&
                             l.LastArticleAnsweredDate < endUtc,
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

            case "streak_keep":
                var played = await context.UserDailyActivities
                    .AsNoTracking()
                    .AnyAsync(
                        a => a.UserId == userId && a.ActivityDate == today && a.Played,
                        cancellationToken);
                return played ? 1 : 0;

            default:
                return 0;
        }
    }
}
