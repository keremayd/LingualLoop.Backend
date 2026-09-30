using MediatR;
using Service.DataTransferObjects.Responses.ArticlePractice;

namespace Service.DataTransferObjects.Requests.ArticlePractice;

public class AnswerArticlePracticeRequest : IRequest<AnswerArticlePracticeResponse>
{
    public string UserId { get; set; } = string.Empty;
    public int KartyId { get; set; }
    public string SelectedArticle { get; set; } = string.Empty;
}
