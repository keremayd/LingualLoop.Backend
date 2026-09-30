using Microsoft.EntityFrameworkCore;
using Postgres;

namespace Service.Helpers;

/// <summary>
/// Süresi dolmuş pasif biletleri veritabanında anında gerçekleştirir.
///
/// Hangfire toplu bakım işidir; kullanıcının ekranda gördüğü kesin yenilenme
/// anı değildir. Bu yardımcı hem kullanıcı sorgusunda hem bakım işinde aynı
/// atomik UPDATE'i çalıştırır. PostgreSQL satır kilidi ve WHERE koşulunu
/// güncel satır üzerinde yeniden denetlediği için eşzamanlı iki çağrı aynı
/// yenilenme aralığını iki kez vermez.
/// </summary>
public static class LivesRegeneration
{
    private static readonly long RegenSeconds =
        (long)LivesRules.RegenInterval.TotalSeconds;

    public static Task<int> MaterializeForUserAsync(
        LingualLoopContext context,
        string userId,
        DateTime utcNow,
        CancellationToken cancellationToken)
    {
        return context.Database.ExecuteSqlInterpolatedAsync($$"""
            UPDATE user_lives
            SET lives = LEAST(
                    max_lives,
                    lives + FLOOR(
                        (
                            EXTRACT(EPOCH FROM {{utcNow}}) -
                            EXTRACT(EPOCH FROM last_lives_reset_time)
                        )
                        / {{RegenSeconds}}
                    )::integer + 1
                ),
                last_lives_reset_time = last_lives_reset_time +
                    LEAST(
                        max_lives - lives,
                        FLOOR(
                            (
                                EXTRACT(EPOCH FROM {{utcNow}}) -
                                EXTRACT(EPOCH FROM last_lives_reset_time)
                            )
                            / {{RegenSeconds}}
                        )::integer + 1
                    ) * {{RegenSeconds}} * INTERVAL '1 second'
            WHERE user_id = {{userId}}
              AND lives < max_lives
              AND EXTRACT(EPOCH FROM last_lives_reset_time) <=
                  EXTRACT(EPOCH FROM {{utcNow}})
            """, cancellationToken);
    }

    public static Task<int> MaterializeAllDueAsync(
        LingualLoopContext context,
        DateTime utcNow,
        CancellationToken cancellationToken)
    {
        return context.Database.ExecuteSqlInterpolatedAsync($$"""
            UPDATE user_lives
            SET lives = LEAST(
                    max_lives,
                    lives + FLOOR(
                        (
                            EXTRACT(EPOCH FROM {{utcNow}}) -
                            EXTRACT(EPOCH FROM last_lives_reset_time)
                        )
                        / {{RegenSeconds}}
                    )::integer + 1
                ),
                last_lives_reset_time = last_lives_reset_time +
                    LEAST(
                        max_lives - lives,
                        FLOOR(
                            (
                                EXTRACT(EPOCH FROM {{utcNow}}) -
                                EXTRACT(EPOCH FROM last_lives_reset_time)
                            )
                            / {{RegenSeconds}}
                        )::integer + 1
                    ) * {{RegenSeconds}} * INTERVAL '1 second'
            WHERE lives < max_lives
              AND EXTRACT(EPOCH FROM last_lives_reset_time) <=
                  EXTRACT(EPOCH FROM {{utcNow}})
            """, cancellationToken);
    }
}
