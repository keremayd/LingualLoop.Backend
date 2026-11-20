using Microsoft.EntityFrameworkCore;
using Postgres;
using Postgres.Models;
using System.Text.Json;

namespace Service.Helpers;

/// Doğrulanmış bir App Store işlemini premium süresine çevirir. Satın alma
/// (uygulamadan) ve App Store sunucu bildirimi (yenileme, iade, bitiş) aynı
/// kuralı kullanır: iptal edilmiş işlem premium'u hemen bitirir, değilse
/// premium işlemin bitişine kadar sürer.
public static class ApplePremium
{
    public const string ProductPrefix = "com.stelinda.app.premium.";

    public enum Outcome { Applied, WrongBundle, WrongProduct, UnknownSubscription }

    /// [userId] null ise kullanıcı aboneliğin kaydından bulunur (bildirim).
    public static async Task<(Outcome Outcome, string? UserId, DateTime? PremiumUntil)> ApplyAsync(
        StelindaContext context,
        JsonElement transaction,
        string bundleId,
        string? userId,
        DateTime utcNow,
        CancellationToken cancellationToken)
    {
        if (transaction.GetString("bundleId") != bundleId) return (Outcome.WrongBundle, null, null);

        var productId = transaction.GetString("productId") ?? string.Empty;
        var originalId = transaction.GetString("originalTransactionId");
        if (!productId.StartsWith(ProductPrefix, StringComparison.Ordinal) || string.IsNullOrEmpty(originalId))
        {
            return (Outcome.WrongProduct, null, null);
        }

        await AppleSubscriptionSchemaHelper.EnsureCreatedAsync(context, cancellationToken);

        var subscription = await context.AppleSubscriptions
            .FirstOrDefaultAsync(s => s.OriginalTransactionId == originalId, cancellationToken);
        if (subscription == null)
        {
            if (userId == null) return (Outcome.UnknownSubscription, null, null);
            subscription = new AppleSubscription { OriginalTransactionId = originalId };
            context.AppleSubscriptions.Add(subscription);
        }

        // Aynı Apple aboneliği başka bir hesapla geri yüklenirse premium o hesaba
        // geçer: abonelik aynı anda tek hesapta.
        var previousUser = subscription.UserId;
        if (userId != null) subscription.UserId = userId;
        var owner = subscription.UserId;

        var expires = transaction.GetDate("expiresDate");
        var revoked = transaction.GetDate("revocationDate");
        subscription.ProductId = productId;
        subscription.Environment = transaction.GetString("environment") ?? string.Empty;
        subscription.ExpiresAt = expires;
        subscription.RevokedAt = revoked;
        subscription.UpdatedAt = utcNow;

        var premiumUntil = revoked.HasValue ? utcNow : expires ?? utcNow;
        await SetPremiumUntilAsync(context, owner, premiumUntil, utcNow, cancellationToken);
        if (!string.IsNullOrEmpty(previousUser) && previousUser != owner)
        {
            await SetPremiumUntilAsync(context, previousUser, utcNow, utcNow, cancellationToken);
        }

        await context.SaveChangesAsync(cancellationToken);
        return (Outcome.Applied, owner, premiumUntil);
    }

    private static async Task SetPremiumUntilAsync(
        StelindaContext context, string userId, DateTime until, DateTime utcNow, CancellationToken cancellationToken)
    {
        var premium = await context.UserPremiums.FirstOrDefaultAsync(p => p.UserId == userId, cancellationToken);
        if (premium == null)
        {
            premium = new UserPremium { UserId = userId };
            context.UserPremiums.Add(premium);
        }
        premium.PremiumUntil = DateTime.SpecifyKind(until, DateTimeKind.Utc);
        premium.UpdatedAt = utcNow;
    }
}
