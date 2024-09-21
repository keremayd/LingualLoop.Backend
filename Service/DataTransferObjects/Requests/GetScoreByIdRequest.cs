using MediatR;
using Service.DataTransferObjects.Responses;

namespace Service.DataTransferObjects.Requests;

public class GetScoreByIdRequest : IRequest<GetScoreByIdResponse>
{
    public int UserId { get; set; }
}