using MediatR;
using Service.DataTransferObjects.Responses.Karty;

namespace Service.DataTransferObjects.Requests.Karty;

public class AcknowledgeKartyUnitUnlockRequest : IRequest<AcknowledgeKartyUnitUnlockResponse>
{
    public string UserId { get; set; } = string.Empty;
}
