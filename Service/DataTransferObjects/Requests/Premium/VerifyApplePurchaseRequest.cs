using MediatR;
using Service.DataTransferObjects.Responses;

namespace Service.DataTransferObjects.Requests;

/// Uygulamanın StoreKit 2'den aldığı imzalı işlem (JWS).
public class VerifyApplePurchaseRequest : IRequest<PremiumStatusResponse>
{
    public string UserId { get; set; } = string.Empty;
    public string SignedTransaction { get; set; } = string.Empty;
}
