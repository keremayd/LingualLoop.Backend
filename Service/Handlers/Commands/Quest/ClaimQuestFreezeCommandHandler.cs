using MediatR;
using Microsoft.EntityFrameworkCore;
using Postgres;
using Postgres.Abstractions;
using Postgres.Models;
using Service.DataTransferObjects.Requests;
using Service.DataTransferObjects.Responses;
using Service.Helpers;

namespace Service.Handlers.Commands;

/// <summary>
/// Günün bütün görevleri bitince görevler ekranında seri koruması toplanır.
/// Günde bir kez; tavan ücretsiz 1, premium 2. Tavandayken talep tüketilmez.
/// </summary>
public class ClaimQuestFreezeCommandHandler
    : IRequestHandler<ClaimQuestFreezeRequest, ClaimQuestFreezeResponse>
{
    private readonly LingualLoopContext _context;

    public ClaimQuestFreezeCommandHandler(ILingualLoopGenericRepository<User> userRepository)
    {
        _context = userRepository.GetDbContext();
    }

    public async Task<ClaimQuestFreezeResponse> Handle(
        ClaimQuestFreezeRequest request,
        CancellationToken cancellationToken)
    {
        await UserQuestClaimSchemaHelper.EnsureCreatedAsync(_context, cancellationToken);
        await UserDailyActivitySchemaHelper.EnsureCreatedAsync(_context, cancellationToken);

        var utcNow = DateTime.UtcNow;
        var dayKey = QuestRules.GetDayKey(utcNow);
        var (freezeCount, freezeCap) = await QuestFreezeBonus.ReadFreezesAsync(
            _context, request.UserId, utcNow, cancellationToken);
        var refused = new ClaimQuestFreezeResponse
        {
            Claimed = false,
            FreezeCount = freezeCount,
            FreezeCap = freezeCap,
        };

        var claimedToday = await _context.UserQuestClaims
            .AsNoTracking()
            .Where(c => c.UserId == request.UserId && c.DayKey == dayKey)
            .Select(c => c.QuestKey)
            .ToListAsync(cancellationToken);
        if (claimedToday.Contains(QuestFreezeBonus.ClaimKey)) return refused;

        var (completed, total) = await QuestFreezeBonus.CountCompletedAsync(
            _context, request.UserId, utcNow, claimedToday, cancellationToken);
        if (total == 0 || completed < total) return refused;

        if (freezeCount >= freezeCap)
        {
            refused.BlockedByCap = true;
            return refused;
        }

        var streak = await _context.UserStreaks
            .FirstOrDefaultAsync(s => s.UserId == request.UserId, cancellationToken);
        if (streak == null)
        {
            // Satır günlük girişte açılır; açılmamışsa seri sıfırdan başlar.
            streak = new UserStreak
            {
                UserId = request.UserId,
                CurrentStreak = 0,
                LongestStreak = 0,
                LastActiveDate = StreakRules.GetIstanbulDate(utcNow),
                UpdatedAt = utcNow,
            };
            _context.UserStreaks.Add(streak);
        }

        streak.FreezeCount = Math.Min(streak.FreezeCount + 1, freezeCap);
        streak.UpdatedAt = utcNow;
        _context.UserQuestClaims.Add(new UserQuestClaim
        {
            UserId = request.UserId,
            QuestKey = QuestFreezeBonus.ClaimKey,
            DayKey = dayKey,
            ClaimedAt = utcNow,
        });

        try
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            // Benzersiz indeks yarışı: aynı gün iki kez talep edildi.
            return refused;
        }

        return new ClaimQuestFreezeResponse
        {
            Claimed = true,
            FreezeCount = streak.FreezeCount,
            FreezeCap = freezeCap,
        };
    }
}
