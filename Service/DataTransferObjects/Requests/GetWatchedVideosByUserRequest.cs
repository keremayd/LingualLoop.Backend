using MediatR;
using Service.DataTransferObjects.Responses;

namespace Service.DataTransferObjects.Requests;

public class GetWatchedVideosByUserRequest : IRequest<GetWatchedVideosByUserResponse>
{
    public string UserId { get; set; } = string.Empty;
}