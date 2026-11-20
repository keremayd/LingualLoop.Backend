using Microsoft.EntityFrameworkCore;
using Postgres;

namespace Service.Helpers;

public static class AppleSubscriptionSchemaHelper
{
    private static readonly SemaphoreSlim SchemaLock = new(1, 1);
    private static bool _isReady;

    public static async Task EnsureCreatedAsync(StelindaContext context, CancellationToken cancellationToken)
    {
        if (_isReady) return;

        await SchemaLock.WaitAsync(cancellationToken);
        try
        {
            if (_isReady) return;

            await context.Database.ExecuteSqlRawAsync(
                """
                CREATE TABLE IF NOT EXISTS apple_subscription (
                    original_transaction_id text PRIMARY KEY,
                    user_id text NOT NULL REFERENCES "AspNetUsers"("Id") ON DELETE CASCADE,
                    product_id text NOT NULL,
                    environment text NOT NULL,
                    expires_at timestamp with time zone NULL,
                    revoked_at timestamp with time zone NULL,
                    updated_at timestamp with time zone NOT NULL
                );

                CREATE INDEX IF NOT EXISTS ix_apple_subscription_user
                    ON apple_subscription (user_id);
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
