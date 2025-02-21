using MediatR;
using Service.DataTransferObjects.Responses.Karty;

namespace Service.DataTransferObjects.Requests.Karty;

public class GetKartyByScoreRequest : IRequest<GetKartyByScoreResponse>
{
    public int UserScore { get; set; }
    public string UserId { get; set; } = string.Empty;
}