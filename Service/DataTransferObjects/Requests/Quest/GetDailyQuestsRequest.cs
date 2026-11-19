using MediatR;
using Service.DataTransferObjects.Responses;

namespace Service.DataTransferObjects.Requests;

public class GetDailyQuestsRequest : IRequest<GetDailyQuestsResponse>
{
    public string UserId { get; set; } = string.Empty;

    /// <summary>Kullanıcının ana dili; görev başlıkları bu dilde döner.</summary>
    public string Locale { get; set; } = Service.Helpers.LocaleRules.Default;
}
