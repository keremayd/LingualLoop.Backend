using Microsoft.EntityFrameworkCore;
using Postgres;

namespace Service.Helpers;

public static class PremiumStatus
{
    /// Kullanıcının premium bitişi (UTC); hiç üye olmadıysa null.
    public static async Task<DateTime?> PremiumUntilAsync(
        StelindaContext context, string userId, CancellationToken cancellationToken)
    {
        var until = await context.UserPremiums
            .AsNoTracking()
            .Where(p => p.UserId == userId)
            .Select(p => p.PremiumUntil)
            .FirstOrDefaultAsync(cancellationToken);
        return until.HasValue ? DateTime.SpecifyKind(until.Value, DateTimeKind.Utc) : null;
    }
}
