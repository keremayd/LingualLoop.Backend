using MediatR;
using Microsoft.EntityFrameworkCore;
using Postgres;
using Postgres.Abstractions;
using Postgres.Models;
using Service.DataTransferObjects.Requests.Profile;
using Service.DataTransferObjects.Responses.Profile;
using Service.Helpers;

namespace Service.Handlers.Commands.Profile;

/// <summary>
/// Kullanıcının seri koruma kararını uygular.
///
/// Koruma bilinçli olarak otomatik harcanmaz; bu handler kararın sonucunu
/// yazar. Karar verilmeden seri askıda kalır — ne artar ne sıfırlanır.
/// </summary>
public class ResolveStreakFreezeCommandHandler
    : IRequestHandler<ResolveStreakFreezeRequest, RecordDailyActivityResponse>
{
    private readonly LingualLoopContext _context;

    public ResolveStreakFreezeCommandHandler(
        ILingualLoopGenericRepository<UserDailyActivity> userDailyActivityRepository)
    {
        _context = userDailyActivityRepository.GetDbContext();
    }

    public async Task<RecordDailyActivityResponse> Handle(
        ResolveStreakFreezeRequest request,
        CancellationToken cancellationToken)
    {
        await UserDailyActivitySchemaHelper.EnsureCreatedAsync(_context, cancellationToken);

        var today = StreakRules.GetIstanbulDate(DateTime.UtcNow);
        var now = DateTime.UtcNow;

        var streak = await _context.UserStreaks
            .FirstOrDefaultAsync(s => s.UserId == request.UserId, cancellationToken);

        if (streak == null)
        {
            return new RecordDailyActivityResponse
            {
                LastActiveDate = today,
                CheckedInToday = true,
            };
        }

        var missedDays = StreakRules.MissedDaysBetween(streak.LastActiveDate, today);
        var canCover = StreakRules.CanCoverWithFreezes(missedDays, streak.FreezeCount);

        if (request.UseFreeze && canCover)
        {
            // Kaçırılan her gün bir koruma yer.
            streak.FreezeCount -= missedDays;

            // Korunan günler zincirde **sayılır** — şeritte de girilen günle
            // aynı onay işaretini taşıyorlar. Bu yüzden seri kaçırılan gün
            // kadar ilerler. Eskiden burada `+= 1` vardı; o "1" bugünün
            // kendisiydi ve açılışta seri arttığı için doğruydu. Artık bugün
            // ancak oynanınca sayılacak, o yüzden buradan çıkarıldı.
            streak.CurrentStreak += missedDays;
            streak.LongestStreak = Math.Max(streak.LongestStreak, streak.CurrentStreak);

            await MarkFrozenDaysAsync(
                request.UserId, streak.LastActiveDate, today, now, cancellationToken);

            // Zincir dünün sonuna kadar kapatıldı. Bugün hâlâ oynanmayı
            // bekliyor; ilk cevapta seri bir artacak.
            streak.LastActiveDate = today.AddDays(-1);
        }
        else
        {
            // Kullanıcı korumayı saklamayı seçti (ya da artık yetmiyor):
            // seri kırıldı. Bugün henüz oynanmadığı için 1 değil **0** —
            // ilk cevap onu 1 yapacak. `LastActiveDate` bugüne çekiliyor ki
            // aynı boşluk her açılışta yeniden sorulmasın.
            streak.CurrentStreak = 0;
            streak.LastActiveDate = today;
        }

        streak.UpdatedAt = now;

        await _context.SaveChangesAsync(cancellationToken);

        return new RecordDailyActivityResponse
        {
            CurrentStreak = streak.CurrentStreak,
            LongestStreak = streak.LongestStreak,
            LastActiveDate = streak.LastActiveDate,
            FreezeCount = streak.FreezeCount,
            StreakAtRisk = false,
            MissedDays = missedDays,
            Week = await StreakWeekBuilder.BuildAsync(
                _context, request.UserId, today, streak, cancellationToken),
            CheckedInToday = true,
        };
    }

    /// <summary>
    /// Korumanın kapattığı günleri `frozen` işaretiyle yazar.
    ///
    /// Şerit bu satırları okuyor. Yazılmasaydı "korundu" bilgisi seri
    /// uzunluğundan tahmin edilmek zorunda kalırdı ve tahmin yanlış sonuç
    /// veriyordu. Aynı zamanda seri sayacı ile aktivite tablosu arasındaki
    /// boşluk kapanmış oluyor.
    /// </summary>
    private async Task MarkFrozenDaysAsync(
        string userId,
        DateOnly lastActiveDate,
        DateOnly today,
        DateTime now,
        CancellationToken cancellationToken)
    {
        var firstMissed = lastActiveDate.AddDays(1);

        // Satırlar **izlenerek** çekiliyor. Var olan bir satır artık "o gün
        // girildi" demek değil, "o gün uygulama açıldı" demek: kullanıcı açıp
        // hiç oynamamış olabilir ve o gün yine kaçırılmıştır. Eski hâli satırı
        // olan günleri atlıyordu; yeni modelde bu, korunan günün şeritte boş
        // görünmesine yol açardı.
        var existing = await _context.UserDailyActivities
            .Where(a => a.UserId == userId &&
                        a.ActivityDate >= firstMissed &&
                        a.ActivityDate < today)
            .ToListAsync(cancellationToken);

        var byDate = existing.ToDictionary(a => a.ActivityDate);

        for (var date = firstMissed; date < today; date = date.AddDays(1))
        {
            if (byDate.TryGetValue(date, out var row))
            {
                // Gerçekten oynanmış bir gün korumaya muhtaç değildir.
                if (!row.Played) row.Frozen = true;
                continue;
            }

            _context.UserDailyActivities.Add(new UserDailyActivity
            {
                UserId = userId,
                ActivityDate = date,
                Frozen = true,
                Played = false,
                CreatedAt = now
            });
        }
    }
}
