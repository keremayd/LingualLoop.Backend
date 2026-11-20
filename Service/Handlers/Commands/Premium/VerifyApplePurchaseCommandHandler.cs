using System.Net;
using Common.Enums;
using Common.Exceptions;
using Common.Extensions;
using Common.Options;
using MediatR;
using Microsoft.Extensions.Options;
using Postgres;
using Postgres.Abstractions;
using Postgres.Models;
using Service.DataTransferObjects.Requests;
using Service.DataTransferObjects.Responses;
using Service.Helpers;

namespace Service.Handlers.Commands;

/// Uygulama satın aldığında ya da geri yüklediğinde işlemi gönderir; işlem
/// Apple'ın imzasıyla doğrulanır ve premium süresi yazılır.
public class VerifyApplePurchaseCommandHandler : IRequestHandler<VerifyApplePurchaseRequest, PremiumStatusResponse>
{
    private readonly StelindaContext _context;
    private readonly AppleOptions _apple;

    public VerifyApplePurchaseCommandHandler(IStelindaGenericRepository<User> userRepository, IOptions<AppleOptions> apple)
    {
        _context = userRepository.GetDbContext();
        _apple = apple.Value;
    }

    public async Task<PremiumStatusResponse> Handle(VerifyApplePurchaseRequest request, CancellationToken cancellationToken)
    {
        var transaction = AppleSignedData.Verify(request.SignedTransaction);
        if (transaction == null) throw Invalid("imza");

        var utcNow = DateTime.UtcNow;
        var (outcome, _, premiumUntil) = await ApplePremium.ApplyAsync(
            _context, transaction.Value, _apple.ClientId, request.UserId, utcNow, cancellationToken);
        if (outcome != ApplePremium.Outcome.Applied) throw Invalid(outcome.ToString());

        return new PremiumStatusResponse
        {
            IsPremium = PremiumRules.IsPremium(premiumUntil, utcNow),
            PremiumUntil = premiumUntil,
        };
    }

    private static StelindaException Invalid(string reason) =>
        new(ErrorCode.AppleTransactionInvalid.CreateMessage(reason),
            ErrorCode.AppleTransactionInvalid.GetDescription(reason), HttpStatusCode.BadRequest);
}
