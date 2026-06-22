using MediatR;
using Service.DataTransferObjects.Responses.Karty;

namespace Service.DataTransferObjects.Requests.Karty;

public class GetWrongKartyReviewRequest : IRequest<GetKartyByScoreResponse>
{
    public string UserId { get; set; } = string.Empty;
}
