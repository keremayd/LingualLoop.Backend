using Microsoft.EntityFrameworkCore;
using Postgres;

namespace Service.Helpers;

/// <summary>
/// Günün bütün görevleri bitince görevler ekranında toplanan seri koruması.
/// Talep, görev talepleriyle aynı tabloya (`user_quest_claim`) ayrı bir
/// anahtarla yazılır: günde bir kez, benzersiz indeks yarışı da önler.
/// Koruma tavanı premium'a göre değişir (`PremiumRules.FreezeCap`).
/// </summary>
public static class QuestFreezeBonus
{
    /// <summary>Talep satırının görev anahtarı.</summary>
    public const string ClaimKey = "all_done_freeze";

    /// <summary>Kullanıcının elindeki koruma ve tutabileceği en fazla koruma.</summary>
    public static async Task<(int Count, int Cap)> ReadFreezesAsync(
        StelindaContext context,
        string userId,
        DateTime utcNow,
        CancellationToken cancellationToken)
    {
        var count = await context.UserStreaks
            .AsNoTracking()
            .Where(s => s.UserId == userId)
            .Select(s => (int?)s.FreezeCount)
            .FirstOrDefaultAsync(cancellationToken) ?? 0;
        var premiumUntil = await context.UserPremiums
            .AsNoTracking()
            .Where(p => p.UserId == userId)
            .Select(p => p.PremiumUntil)
            .FirstOrDefaultAsync(cancellationToken);
        return (count, PremiumRules.FreezeCap(PremiumRules.IsPremium(premiumUntil, utcNow)));
    }

    /// <summary>Günün görevlerinden kaçı bitti, kaç görev var.</summary>
    public static async Task<(int Completed, int Total)> CountCompletedAsync(
        StelindaContext context,
        string userId,
        DateTime utcNow,
        IReadOnlyCollection<string> claimedToday,
        CancellationToken cancellationToken)
    {
        var day = await QuestRules.ReadDayStateAsync(context, userId, utcNow, cancellationToken);
        var quests = QuestRules.ChooseForDay(day, claimedToday);
        var completed = 0;
        foreach (var quest in quests)
        {
            var progress = await QuestRules.GetProgressAsync(
                context, userId, quest.Key, utcNow, cancellationToken);
            if (progress >= quest.Target) completed++;
        }

        return (completed, quests.Count);
    }
}
