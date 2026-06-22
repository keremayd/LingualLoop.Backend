using MediatR;
using Service.DataTransferObjects.Responses.Karty;

namespace Service.DataTransferObjects.Requests.Karty;

public class ResolveWrongKartyReviewRequest : IRequest<ResolveWrongKartyReviewResponse>
{
    public string UserId { get; set; } = string.Empty;
    public int KartyId { get; set; }
    public bool IsMastered { get; set; }
}
