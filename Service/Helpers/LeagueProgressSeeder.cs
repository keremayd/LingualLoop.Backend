using Microsoft.EntityFrameworkCore;
using Postgres;
using Postgres.Models;

namespace Service.Helpers;

/// <summary>
/// Kullanıcının mevcut sezon lig kaydını getirir veya oluşturur.
/// Kullanıcının hiç lig kaydı yoksa (lig sistemi öncesi hesaplar) ilk kayıt
/// toplam skorundan tohumlanır; sonraki sezonlar 0 puandan başlar.
/// Lig puanı ve toplam skor bilinçli olarak ayrı sayaçlardır: toplam skor
/// içerik zorluğunu belirler ve resetlenmez, sezon puanı her sezon sıfırlanır.
/// </summary>
public static class LeagueProgressSeeder
{
    public static async Task<UserLeagueProgress> GetOrCreateAsync(
        LingualLoopContext context,
        string userId,
        int seasonKey,
        DateTime utcNow,
        CancellationToken cancellationToken)
    {
        var progress = await context.UserLeagueProgresses
            .FirstOrDefaultAsync(
                p => p.UserId == userId && p.SeasonKey == seasonKey,
                cancellationToken);
        if (progress is not null)
        {
            return progress;
        }

        var previousSeason = await context.UserLeagueProgresses
            .AsNoTracking()
            .Where(p => p.UserId == userId && p.SeasonKey < seasonKey)
            .OrderByDescending(p => p.SeasonKey)
            .FirstOrDefaultAsync(cancellationToken);

        int seedPoints;
        if (previousSeason is null)
        {
            // Lig sistemine ilk giriş: mevcut toplam skor sezon puanına taşınır.
            seedPoints = await context.UserScores
                .AsNoTracking()
                .Where(s => s.UserId == userId)
                .Select(s => (int?)s.Score)
                .FirstOrDefaultAsync(cancellationToken) ?? 0;
        }
        else
        {
            // "Bir kademe alttan başla": önceki sezonun bitiş ligine ve
            // aradan geçen sezon sayısına göre yeni sezon tabanı belirlenir.
            var seasonsElapsed = LeagueRules.GetSeasonsBetween(
                previousSeason.SeasonKey,
                seasonKey);
            seedPoints = LeagueRules.GetSeasonStartPoints(
                previousSeason.Points,
                seasonsElapsed);
        }

        progress = new UserLeagueProgress
        {
            UserId = userId,
            SeasonKey = seasonKey,
            Points = Math.Max(0, seedPoints),
            // Sezonun başlangıç ligi bir yükselme değildir. Kullanıcı yalnız
            // bu noktadan sonra gerçekten geçtiği eşikler için sonuç görür.
            AnnouncedLeagueRank = LeagueRules.GetLeagueRank(seedPoints),
            UpdatedAt = utcNow,
        };
        context.UserLeagueProgresses.Add(progress);

        try
        {
            await context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            // Unique index (user_id, season_key) yarışı: paralel istek kaydı
            // önce oluşturduysa mevcut kaydı kullan.
            context.Entry(progress).State = EntityState.Detached;
            progress = await context.UserLeagueProgresses
                .FirstAsync(
                    p => p.UserId == userId && p.SeasonKey == seasonKey,
                    cancellationToken);
        }

        return progress;
    }
}
