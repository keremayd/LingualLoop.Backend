using Common.Options;
using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Postgres;
using Postgres.Abstractions;
using Postgres.Models;
using Service.DataTransferObjects.Requests;
using Service.DataTransferObjects.Responses;
using Service.Helpers;

namespace Service.Handlers.Commands;

/// App Store Server Notifications V2: yenileme, iptal, iade, bitiş. Her
/// bildirimdeki imzalı işlem premium süresini yeniden yazar
/// (ApplePremium.ApplyAsync). Ödemesi başarısız olup ek süreye (grace period)
/// giren abonelikte premium ek sürenin sonuna kadar sürer.
public class HandleAppleNotificationCommandHandler : IRequestHandler<AppleNotificationRequest, AppleNotificationResponse>
{
    private readonly StelindaContext _context;
    private readonly AppleOptions _apple;
    private readonly ILogger<HandleAppleNotificationCommandHandler> _logger;

    public HandleAppleNotificationCommandHandler(
        IStelindaGenericRepository<User> userRepository,
        IOptions<AppleOptions> apple,
        ILogger<HandleAppleNotificationCommandHandler> logger)
    {
        _context = userRepository.GetDbContext();
        _apple = apple.Value;
        _logger = logger;
    }

    public async Task<AppleNotificationResponse> Handle(AppleNotificationRequest request, CancellationToken cancellationToken)
    {
        var payload = AppleSignedData.Verify(request.SignedPayload);
        if (payload == null) return new AppleNotificationResponse { Valid = false };

        var type = payload.Value.GetString("notificationType");
        if (!payload.Value.TryGetProperty("data", out var data))
        {
            _logger.LogInformation("Apple bildirimi (veri yok): {Type}", type);
            return new AppleNotificationResponse { Valid = true };
        }

        var transaction = AppleSignedData.Verify(data.GetString("signedTransactionInfo"));
        if (transaction == null)
        {
            _logger.LogInformation("Apple bildirimi (işlem yok): {Type}", type);
            return new AppleNotificationResponse { Valid = true };
        }

        var utcNow = DateTime.UtcNow;
        var (outcome, userId, premiumUntil) = await ApplePremium.ApplyAsync(
            _context, transaction.Value, _apple.ClientId, null, utcNow, cancellationToken);

        // Ödeme alınamadı ama Apple ek süre tanıdı: premium o süre boyunca sürer.
        var renewal = AppleSignedData.Verify(data.GetString("signedRenewalInfo"));
        var graceUntil = renewal?.GetDate("gracePeriodExpiresDate");
        if (outcome == ApplePremium.Outcome.Applied && userId != null &&
            graceUntil.HasValue && graceUntil > (premiumUntil ?? utcNow) &&
            transaction.Value.GetDate("revocationDate") == null)
        {
            var premium = _context.UserPremiums.First(p => p.UserId == userId);
            premium.PremiumUntil = graceUntil;
            premium.UpdatedAt = utcNow;
            await _context.SaveChangesAsync(cancellationToken);
        }

        _logger.LogInformation("Apple bildirimi {Type}/{Subtype}: {Outcome}",
            type, payload.Value.GetString("subtype"), outcome);
        return new AppleNotificationResponse { Valid = true, Applied = outcome == ApplePremium.Outcome.Applied };
    }
}
