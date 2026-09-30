using MediatR;
using Service.DataTransferObjects.Responses;

namespace Service.DataTransferObjects.Requests;

public class ClaimQuestRewardRequest : IRequest<ClaimQuestRewardResponse>
{
    public string UserId { get; set; } = string.Empty;
    public string QuestKey { get; set; } = string.Empty;
}
