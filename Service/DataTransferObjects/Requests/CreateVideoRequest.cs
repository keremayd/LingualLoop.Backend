using MediatR;
using Service.DataTransferObjects.Responses;

namespace Service.DataTransferObjects.Requests;

public class CreateVideoRequest : IRequest<CreateVideoResponse>
{
    public string VideoUrl { get; set; } = string.Empty;
    public string VideoTitle { get; set; } = string.Empty;
    public string VideoDescription { get; set; } = string.Empty;
}