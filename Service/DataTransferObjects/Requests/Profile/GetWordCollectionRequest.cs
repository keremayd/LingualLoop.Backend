using MediatR;
using Service.DataTransferObjects.Responses.Profile;

namespace Service.DataTransferObjects.Requests.Profile;

public class GetWordCollectionRequest : IRequest<GetWordCollectionResponse>
{
    public string UserId { get; set; } = string.Empty;
    public string Category { get; set; } = "all";
    public int Page { get; set; } = 1;
}
