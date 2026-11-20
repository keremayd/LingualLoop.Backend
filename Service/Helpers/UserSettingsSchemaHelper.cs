using Microsoft.EntityFrameworkCore;
using Postgres;

namespace Service.Helpers;

/// <summary>
/// <c>user_settings</c> tablosunu garanti eder (bugün yalnız ana dil).
/// Hesap silinince satır da gider: <c>AspNetUsers</c>'a CASCADE bağlı
/// (CLAUDE.md "Hesap silme" kuralı). Süreç başına bir kez çalışır.
/// </summary>
public static class UserSettingsSchemaHelper
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

            await context.Database.ExecuteSqlRawAsync(
                """
                CREATE TABLE IF NOT EXISTS user_settings (
                    user_id text PRIMARY KEY REFERENCES "AspNetUsers"("Id") ON DELETE CASCADE,
                    native_language text NOT NULL,
                    updated_at timestamp with time zone NOT NULL DEFAULT now()
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
