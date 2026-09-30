using Microsoft.EntityFrameworkCore;
using Postgres;

namespace Service.Helpers;

/// <summary>
/// `user_scores.experience` kolonunu garantiler.
///
/// Skor iki iş birden yapıyordu: kart zorluğunu seçmek (iki yönlü hareket
/// etmesi gerekir) ve kullanıcıya ilerlemeyi göstermek (düşmemesi gerekir).
/// Aynı sayı ikisini yapamaz. `score` zorluk termostatı olarak kalır,
/// `experience` yalnızca artan ilerleme sayacı olur.
/// </summary>
public static class UserScoreSchemaHelper
{
    private static readonly SemaphoreSlim SchemaLock = new(1, 1);
    private static bool _isReady;

    public static async Task EnsureCreatedAsync(
        LingualLoopContext context,
        CancellationToken cancellationToken)
    {
        if (_isReady) return;

        await SchemaLock.WaitAsync(cancellationToken);
        try
        {
            if (_isReady) return;

            await context.Database.ExecuteSqlRawAsync(
                """
                ALTER TABLE user_scores
                ADD COLUMN IF NOT EXISTS experience integer NOT NULL DEFAULT 0;
                """,
                cancellationToken);

            _isReady = true;
        }
        finally
        {
            SchemaLock.Release();
        }
    }
}
