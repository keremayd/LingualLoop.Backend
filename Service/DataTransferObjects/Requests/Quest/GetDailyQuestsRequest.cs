using MediatR;
using Service.DataTransferObjects.Responses;

namespace Service.DataTransferObjects.Requests;

public class GetDailyQuestsRequest : IRequest<GetDailyQuestsResponse>
{
    public string UserId { get; set; } = string.Empty;
}
