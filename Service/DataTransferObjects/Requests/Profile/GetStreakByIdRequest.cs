using MediatR;
using Service.DataTransferObjects.Responses.Profile;

namespace Service.DataTransferObjects.Requests.Profile;

public class GetStreakByIdRequest : IRequest<GetStreakByIdResponse>
{
    public string UserId { get; set; } = string.Empty;
}
