using MediatR;
using Service.DataTransferObjects.Responses;

namespace Service.DataTransferObjects.Requests;

public class UpdateScoreRequest: IRequest<UpdateScoreResponse>
{
    public string UserId { get; set; } = string.Empty;
    public int Point { get; set; }
    public int? KartyId { get; set; }
}