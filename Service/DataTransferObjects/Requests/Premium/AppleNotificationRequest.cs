using MediatR;
using Service.DataTransferObjects.Responses;

namespace Service.DataTransferObjects.Requests;

/// App Store Server Notifications V2 gövdesi.
public class AppleNotificationRequest : IRequest<AppleNotificationResponse>
{
    public string SignedPayload { get; set; } = string.Empty;
}
