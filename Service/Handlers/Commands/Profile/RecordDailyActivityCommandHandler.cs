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
/// Günlük giriş kaydı ve seri hesabı.
///
/// Seri arka planda gece yarısı kırılmaz; boşluk, kullanıcı geri döndüğünde
/// burada tespit edilir.
///
/// **Koruma otomatik harcanmaz.** Boşluk varsa ve kullanıcının yeterli
/// koruması varsa seri ne artar ne sıfırlanır: `StreakAtRisk` ile karar
/// kullanıcıya bırakılır (`ResolveStreakFreezeCommandHandler`). Kısa bir seri
/// için kıymetli bir korumayı yakmak istemeyebilir. Karar verilene kadar her
/// açılışta yeniden sorulur.
/// </summary>
public class RecordDailyActivityCommandHandler
    : IRequestHandler<RecordDailyActivityRequest, RecordDailyActivityResponse>
{
    private static readonly SemaphoreSlim CheckInLock = new(1, 1);

    private readonly LingualLoopContext _context;

    public RecordDailyActivityCommandHandler(
        ILingualLoopGenericRepository<UserDailyActivity> userDailyActivityRepository)
    {
        _context = userDailyActivityRepository.GetDbContext();
    }

    public async Task<RecordDailyActivityResponse> Handle(
        RecordDailyActivityRequest request,
        CancellationToken cancellationToken)
    {
        await UserDailyActivitySchemaHelper.EnsureCreatedAsync(_context, cancellationToken);

        var today = StreakRules.GetIstanbulDate(DateTime.UtcNow);
        var now = DateTime.UtcNow;

        await CheckInLock.WaitAsync(cancellationToken);
        try
        {
            var alreadyRecorded = await _context.UserDailyActivities
                .AsNoTracking()
                .AnyAsync(
                    activity => activity.UserId == request.UserId &&
                                activity.ActivityDate == today,
                    cancellationToken);

            var streak = await _context.UserStreaks
                .FirstOrDefaultAsync(
                    userStreak => userStreak.UserId == request.UserId,
                    cancellationToken);

            if (!alreadyRecorded)
            {
                _context.UserDailyActivities.Add(new UserDailyActivity
                {
                    UserId = request.UserId,
                    ActivityDate = today,
                    // Giriş oynamak değildir. `played` yalnızca gerçek bir
                    // cevap verildiğinde `StreakActivityRecorder` işaretler.
                    Played = false,
                    CreatedAt = now
                });
            }

            await RefillPremiumFreezesAsync(request.UserId, streak, today, now, cancellationToken);

            var atRisk = false;
            var missedDays = 0;

            // Seri burada **artmaz.** Açılışın işi yalnızca girişi kaydetmek ve
            // zincirde kurtarılabilir bir boşluk varsa koruma sorusunu sormak.
            // Artış `StreakActivityRecorder` içinde, gerçekten oynandığında olur.
            if (streak == null)
            {
                streak = new UserStreak
                {
                    UserId = request.UserId,
                    // Kullanıcı henüz oynamadı: seri sıfırdan başlar.
                    CurrentStreak = 0,
                    LongestStreak = 0,
                    LastActiveDate = today,
                    UpdatedAt = now
                };
                _context.UserStreaks.Add(streak);
            }
            else if (streak.CurrentStreak == 0)
            {
                // Seri zaten kırık; sorulacak bir şey yok. İlk oynayışta 1'den
                // yeniden başlar.
            }
            else if (streak.LastActiveDate >= today.AddDays(-1))
            {
                // Dün ya da bugün oynanmış: zincirde boşluk yok. Bugün henüz
                // oynanmamış olsa bile seri risk altında değil, günün vakti var.
            }
            else
            {
                missedDays = StreakRules.MissedDaysBetween(streak.LastActiveDate, today);

                if (StreakRules.CanCoverWithFreezes(missedDays, streak.FreezeCount))
                {
                    // Karar kullanıcının. Seriye ve son oynama tarihine
                    // dokunulmuyor ki "hayır" derse doğru şekilde sıfırlansın,
                    // "evet" derse kaldığı yerden devam etsin.
                    atRisk = true;
                }
                else
                {
                    // Kurtarılamıyor: seri kırıldı. Sayaç sıfırlanıyor ama
                    // bugün "oynanmış" sayılmıyor — ilk cevapta 1'e çıkacak.
                    // `LastActiveDate` bugüne çekiliyor ki her açılışta aynı
                    // boşluk yeniden hesaplanmasın; sayaç 0 iken bu tarih
                    // zincir değil yalnızca çıpadır.
                    streak.CurrentStreak = 0;
                    streak.LastActiveDate = today;
                    streak.UpdatedAt = now;
                }
            }

            await _context.SaveChangesAsync(cancellationToken);

            return new RecordDailyActivityResponse
            {
                CurrentStreak = streak.CurrentStreak,
                LongestStreak = streak.LongestStreak,
                LastActiveDate = streak.LastActiveDate,
                FreezeCount = streak.FreezeCount,
                StreakAtRisk = atRisk,
                MissedDays = missedDays,
                Week = await StreakWeekBuilder.BuildAsync(
                    _context, request.UserId, today, streak, cancellationToken),
                CheckedInToday = true
            };
        }
        finally
        {
            CheckInLock.Release();
        }
    }

    /// <summary>
    /// Premium üyenin korumaları tembel olarak dolar: ayrı bir zamanlanmış iş
    /// yerine, kullanıcının zaten her açılışta geçtiği bu akışta kontrol edilir.
    /// </summary>
    private async Task RefillPremiumFreezesAsync(
        string userId,
        UserStreak? streak,
        DateOnly today,
        DateTime now,
        CancellationToken cancellationToken)
    {
        if (streak == null) return;
        if (!PremiumRules.ShouldRefill(streak.FreezeRefilledAt, today)) return;

        var premiumUntil = await _context.UserPremiums
            .AsNoTracking()
            .Where(p => p.UserId == userId)
            .Select(p => p.PremiumUntil)
            .FirstOrDefaultAsync(cancellationToken);

        if (!PremiumRules.IsPremium(premiumUntil, now)) return;

        streak.FreezeCount = PremiumRules.StreakFreezeCap;
        streak.FreezeRefilledAt = today;
    }
}
