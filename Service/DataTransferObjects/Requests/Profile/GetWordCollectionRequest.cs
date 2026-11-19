using MediatR;
using Service.DataTransferObjects.Responses.Profile;

namespace Service.DataTransferObjects.Requests.Profile;

public class GetWordCollectionRequest : IRequest<GetWordCollectionResponse>
{
    public string UserId { get; set; } = string.Empty;
    public string Category { get; set; } = "all";
    public int Page { get; set; } = 1;

    /// <summary>Kullanıcının ana dili (<c>LocaleRules</c>); anlam ve ünite adı bu dilde döner.</summary>
    public string Locale { get; set; } = Service.Helpers.LocaleRules.Default;
}
