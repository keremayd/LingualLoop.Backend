using MediatR;
using Service.DataTransferObjects.Responses;

namespace Service.DataTransferObjects.Requests;

public class GetLivesByIdRequest : IRequest<GetLivesByIdResponse>
{
    public string UserId { get; set; } = string.Empty;
}