using MediatR;
using Service.DataTransferObjects.Responses.Karty;

namespace Service.DataTransferObjects.Requests.Karty;

public class GetWrongKartyReviewRequest : IRequest<GetKartyByScoreResponse>
{
    public string UserId { get; set; } = string.Empty;

    /// <summary>Kullanıcının ana dili (<c>LocaleRules</c>); anlam ve ünite adı bu dilde döner.</summary>
    public string Locale { get; set; } = Service.Helpers.LocaleRules.Default;
}
