using Microsoft.EntityFrameworkCore;
using Postgres;
using Postgres.Models;

namespace Service.Helpers;

/// <summary>
/// Ünitenin açıldığı anı kaydeder (<see cref="UserKartyUnitState"/>).
///
/// Bir kelime pekiştiğinde (kutu 1 → 2) çağrılır; bir ünite yalnız o an
/// hedefe ulaşabilir. Kaydedilen ünite bir daha kilitlenmez ve patika onu
/// bir kez kutlar. Kural <see cref="KartyUnitRules.NewlyUnlocked"/> içinde.
/// </summary>
public static class KartyUnitUnlockRecorder
{
    public static async Task CaptureAsync(
        LingualLoopContext context,
        string userId,
        CancellationToken cancellationToken)
    {
        await KartyUnitSchemaHelper.EnsureCreatedAsync(context, cancellationToken);

        var snapshots = await KartyUnitProgressReader.ReadAsync(
            context, userId, cancellationToken);
        if (snapshots.Count < 2) return;

        var state = await context.UserKartyUnitStates
            .FirstOrDefaultAsync(item => item.UserId == userId, cancellationToken);
        var newly = KartyUnitRules.NewlyUnlocked(
            snapshots.Select(item => item.Unit.Sequence).ToList(),
            snapshots.Select(item => item.Progress).ToList(),
            snapshots.Select(item => item.Unlocked).ToList(),
            state?.UnlockedSequence ?? 0);
        if (newly.Count == 0) return;

        var now = DateTime.UtcNow;
        if (state is null)
        {
            state = new UserKartyUnitState
            {
                UserId = userId,
                UnlockedSequence = newly.Max(),
                // Bu ana kadar açık olanlar kutlanmış sayılır; yalnız yeni
                // açılan bekler.
                AnnouncedSequence = newly.Min() - 1,
                UpdatedAt = now,
            };
            context.UserKartyUnitStates.Add(state);
        }
        else
        {
            state.UnlockedSequence = newly.Max();
            state.UpdatedAt = now;
        }

        try
        {
            await context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            // Aynı anda gelen ikinci cevap satırı önce açtı; o kayıt da aynı
            // üniteyi tutuyor. Bu istekte kaydı tekrar denemenin anlamı yok.
            context.Entry(state).State = EntityState.Detached;
        }
    }
}
