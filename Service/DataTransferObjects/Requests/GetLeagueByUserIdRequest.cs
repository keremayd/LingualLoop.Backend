using MediatR;
using Service.DataTransferObjects.Responses;

namespace Service.DataTransferObjects.Requests;

public class GetLeagueByUserIdRequest : IRequest<LeagueProgressResponse>
{
    public string UserId { get; set; } = string.Empty;
}
