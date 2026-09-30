using MediatR;
using Service.DataTransferObjects.Responses.ArticlePractice;

namespace Service.DataTransferObjects.Requests.ArticlePractice;

public class GetArticlePracticeTaskRequest : IRequest<GetArticlePracticeTaskResponse>
{
    public string UserId { get; set; } = string.Empty;
    public int? PreferredKartyId { get; set; }
}
