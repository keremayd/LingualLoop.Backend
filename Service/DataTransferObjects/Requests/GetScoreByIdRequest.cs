using MediatR;
using Service.DataTransferObjects.Responses;

namespace Service.DataTransferObjects.Requests;

public class GetScoreByIdRequest : IRequest<GetScoreByIdResponse>
{
    public string UserId { get; set; } = string.Empty;
}