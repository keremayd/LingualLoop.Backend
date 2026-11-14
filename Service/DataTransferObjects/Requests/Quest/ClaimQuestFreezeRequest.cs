using MediatR;
using Service.DataTransferObjects.Responses;

namespace Service.DataTransferObjects.Requests;

public class ClaimQuestFreezeRequest : IRequest<ClaimQuestFreezeResponse>
{
    public string UserId { get; set; } = string.Empty;
}
