using MediatR;
using Service.DataTransferObjects.Responses;

namespace Service.DataTransferObjects.Requests;

public class GetQuestionByScoreRequest : IRequest<GetQuestionByScoreResponse>
{
    public int UserScore { get; set; }
    public int UserId { get; set; }
}