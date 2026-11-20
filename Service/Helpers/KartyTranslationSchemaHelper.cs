using Microsoft.EntityFrameworkCore;
using Postgres;

namespace Service.Helpers;

/// <summary>
/// Dile bağlı içeriğin tablolarını garanti eder: kart anlamları
/// (<c>karty_translation</c>) ve ünite adlarının çevirileri
/// (<c>karty_unit_translation</c>). Ana dil kuralı <see cref="LocaleRules"/>.
///
/// Depodaki desen: idempotent DDL, veriye dokunmaz, süreç başına bir kez.
/// <see cref="KartyLearningSchemaHelper"/> bunu çağırır: kart döndüren her
/// sorgu artık anlamı da okuyor (<see cref="KartyUnitSchemaHelper"/> ile aynı
/// gerekçe).
/// </summary>
public static class KartyTranslationSchemaHelper
{
    private static readonly SemaphoreSlim SchemaLock = new(1, 1);
    private static bool _isReady;

    public static async Task EnsureCreatedAsync(
        StelindaContext context,
        CancellationToken cancellationToken)
    {
        if (_isReady) return;

        await SchemaLock.WaitAsync(cancellationToken);
        try
        {
            if (_isReady) return;

            // Ünite çevirisi karty_unit'e bağlı.
            await KartyUnitSchemaHelper.EnsureCreatedAsync(context, cancellationToken);

            await context.Database.ExecuteSqlRawAsync(
                """
                CREATE TABLE IF NOT EXISTS karty_translation (
                    karty_id integer NOT NULL REFERENCES karty (karty_id) ON DELETE CASCADE,
                    locale text NOT NULL,
                    meaning text NOT NULL,
                    updated_at timestamp with time zone NOT NULL DEFAULT now(),
                    PRIMARY KEY (karty_id, locale)
                );

                CREATE TABLE IF NOT EXISTS karty_unit_translation (
                    karty_unit_id integer NOT NULL
                        REFERENCES karty_unit (karty_unit_id) ON DELETE CASCADE,
                    locale text NOT NULL,
                    title text NOT NULL,
                    updated_at timestamp with time zone NOT NULL DEFAULT now(),
                    PRIMARY KEY (karty_unit_id, locale)
                );
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
