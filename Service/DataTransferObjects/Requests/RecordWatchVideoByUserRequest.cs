using MediatR;
using Service.DataTransferObjects.Responses;

namespace Service.DataTransferObjects.Requests;

public class RecordWatchVideoByUserRequest : IRequest<RecordWatchVideoByUserResponse>
{
    public int VideoId { get; set; }
    public string UserId { get; set; } = string.Empty;
}