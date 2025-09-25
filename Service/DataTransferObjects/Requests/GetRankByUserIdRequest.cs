using MediatR;

namespace Service.DataTransferObjects.Requests;

public class GetRankByUserIdRequest : IRequest<int>
{
    public string UserId { get; set; } = string.Empty;
}