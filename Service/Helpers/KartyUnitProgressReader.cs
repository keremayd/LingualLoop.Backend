using Microsoft.EntityFrameworkCore;
using Postgres;
using Postgres.Models;

namespace Service.Helpers;

/// <summary>
/// Bir kullanıcının ünitelerdeki durumunu okur: her ünitede kaç kart var,
/// kaçıyla tanışıldı, kaçı pekişti ve ünite açık mı. Kelime seçici
/// (<see cref="KartyScheduler"/>) ile patika sorgusu aynı okumayı kullanır ki
/// "hangi ünite açık" iki yerde farklı hesaplanmasın.
/// </summary>
public static class KartyUnitProgressReader
{
    public static async Task<IReadOnlyList<KartyUnitSnapshot>> ReadAsync(
        LingualLoopContext context,
        string userId,
        CancellationToken cancellationToken)
    {
        var units = await context.KartyUnits
            .AsNoTracking()
            .OrderBy(unit => unit.Sequence)
            .ToListAsync(cancellationToken);
        if (units.Count == 0) return Array.Empty<KartyUnitSnapshot>();

        var wordCounts = await context.Karty
            .AsNoTracking()
            .Where(karty => karty.UnitId != null)
            .GroupBy(karty => karty.UnitId!.Value)
            .Select(group => new { UnitId = group.Key, Count = group.Count() })
            .ToDictionaryAsync(item => item.UnitId, item => item.Count, cancellationToken);

        var learning = await (
                from row in context.UserKartyLearnings.AsNoTracking()
                join karty in context.Karty.AsNoTracking()
                    on row.KartyId equals karty.KartyId
                where row.UserId == userId && karty.UnitId != null
                group row by karty.UnitId!.Value
                into byUnit
                select new
                {
                    UnitId = byUnit.Key,
                    Seen = byUnit.Count(),
                    Consolidated = byUnit.Count(
                        item => item.Box >= KartyUnitRules.ConsolidatedBox),
                })
            .ToDictionaryAsync(item => item.UnitId, cancellationToken);

        var progress = units
            .Select(unit =>
            {
                learning.TryGetValue(unit.KartyUnitId, out var row);
                return new UnitProgress(
                    wordCounts.GetValueOrDefault(unit.KartyUnitId),
                    row?.Seen ?? 0,
                    row?.Consolidated ?? 0);
            })
            .ToList();
        var recorded = await context.UserKartyUnitStates
            .AsNoTracking()
            .Where(state => state.UserId == userId)
            .Select(state => (int?)state.UnlockedSequence)
            .FirstOrDefaultAsync(cancellationToken) ?? 0;
        var unlocked = KartyUnitRules.UnlockStates(
            progress,
            KartyUnitRules.RecordedIndex(units.Select(unit => unit.Sequence).ToList(), recorded));

        return units
            .Select((unit, i) => new KartyUnitSnapshot(unit, progress[i], unlocked[i]))
            .ToList();
    }
}

public sealed record KartyUnitSnapshot(KartyUnit Unit, UnitProgress Progress, bool Unlocked)
{
    public bool HasUnseen => Progress.SeenCount < Progress.WordCount;
}
