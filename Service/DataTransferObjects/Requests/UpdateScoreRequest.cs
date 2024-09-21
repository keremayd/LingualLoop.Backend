using MediatR;
using Service.DataTransferObjects.Responses;

namespace Service.DataTransferObjects.Requests;

public class UpdateScoreRequest: IRequest<UpdateScoreResponse>
{
    public int UserId { get; set; }
    public int Point { get; set; }
}