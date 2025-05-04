using MediatR;
using Service.DataTransferObjects.Responses.Badge;

namespace Service.DataTransferObjects.Requests.Badge;

public class GetBadgesByIdRequest : IRequest<List<GetBadgesByIdResponse>>
{
    public string UserId { get; set; } = string.Empty;
    public int BadgeId { get; set; }
}