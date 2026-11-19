using MediatR;
using Service.DataTransferObjects.Responses;

namespace Service.DataTransferObjects.Requests;

/// <summary>Kullanıcının ana dilini kaydeder ("tr", "sq"); bkz. <c>LocaleRules</c>.</summary>
public class UpdateUserLanguageRequest : IRequest<UpdateUserLanguageResponse>
{
    public string UserId { get; set; } = string.Empty;
    public string NativeLanguage { get; set; } = string.Empty;
}
