using MediatR;
using Service.DataTransferObjects.Responses;

namespace Service.DataTransferObjects.Requests;

public class GetLeagueByUserIdRequest : IRequest<LeagueProgressResponse>
{
    public string UserId { get; set; } = string.Empty;

    /// <summary>Kullanıcının ana dili; adı olmayan oyuncunun varsayılan adı bu dilde.</summary>
    public string Locale { get; set; } = Service.Helpers.LocaleRules.Default;
}
