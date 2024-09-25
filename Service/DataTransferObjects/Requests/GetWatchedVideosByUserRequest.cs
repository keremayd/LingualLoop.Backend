using MediatR;
using Service.DataTransferObjects.Responses;

namespace Service.DataTransferObjects.Requests;

public class GetWatchedVideosByUserRequest : IRequest<GetWatchedVideosByUserResponse>
{
    public int UserId { get; set; }
}