using MediatR;
using Service.DataTransferObjects.Responses.Profile;

namespace Service.DataTransferObjects.Requests.Profile;

public class GetProfileLearningStatsRequest : IRequest<GetProfileLearningStatsResponse>
{
    public string UserId { get; set; } = string.Empty;
}
