using Microsoft.EntityFrameworkCore;
using Postgres;
using Postgres.Models;

namespace Service.Helpers;

/// <summary>
/// Kullanıcının bilet satırını getirir veya oluşturur.
///
/// Kayıt akışında user_lives satırı hiç oluşturulmuyordu; satırı olmayan
/// kullanıcı NoDataFoundInUserLives hatası alıp oyuna hiç giremiyordu.
/// Satır burada tembel oluşturulur, böylece kayıt akışına dokunmadan hem
/// eski hem yeni hesaplar çalışır.
/// </summary>
public static class UserLivesSeeder
{
    public static async Task<UserLives> GetOrCreateAsync(
        LingualLoopContext context,
        string userId,
        DateTime utcNow,
        CancellationToken cancellationToken)
    {
        var userLives = await context.UserLives
            .FirstOrDefaultAsync(l => l.UserId == userId, cancellationToken);
        if (userLives is not null)
        {
            return userLives;
        }

        // Yeni hesap dolu başlar: ilk oturumda bilet duvarına çarpmak
        // oyunu tanımadan cezalandırmak olurdu.
        userLives = new UserLives
        {
            UserId = userId,
            Lives = LivesRules.DefaultMaxLives,
            MaxLives = LivesRules.DefaultMaxLives,
            LastLivesResetTime = utcNow,
        };
        context.UserLives.Add(userLives);

        try
        {
            await context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            // Paralel istek satırı önce oluşturduysa mevcut satırı kullan.
            // NOT: user_lives.user_id üzerinde unique index yok, bu yüzden
            // yarış burada hataya değil mükerrer satıra dönüşebilir. Index
            // eklenmesi ayrı bir iş olarak duruyor.
            context.Entry(userLives).State = EntityState.Detached;
            userLives = await context.UserLives
                .FirstAsync(l => l.UserId == userId, cancellationToken);
        }

        return userLives;
    }
}
