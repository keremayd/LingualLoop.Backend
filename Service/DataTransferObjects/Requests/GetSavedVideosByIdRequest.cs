using MediatR;
using Service.DataTransferObjects.Responses.Video;

namespace Service.DataTransferObjects.Requests;

public class GetSavedVideosByIdRequest : IRequest<List<GetSavedVideosByIdResponse>>
{
    public string UserId { get; set; } = string.Empty;
}