using MediatR;
using Service.DataTransferObjects.Responses;

namespace Service.DataTransferObjects.Requests;

public class GetQuestionByScoreRequest : IRequest<GetQuestionByScoreResponse>
{
    public int UserScore { get; set; }
    public string UserId { get; set; } = string.Empty;
}