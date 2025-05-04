using MediatR;
using Service.DataTransferObjects.Responses.Video;

namespace Service.DataTransferObjects.Requests;

public class GetVideosByIdRequest : IRequest<List<GetVideosByIdResponse>>
{
    public string UserId { get; set; } = string.Empty;
}