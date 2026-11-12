using MediatR;
using Service.DataTransferObjects.Responses;

namespace Service.DataTransferObjects.Requests.Authentication;

public class AppleLoginRequest : IRequest<AppleLoginResponse>
{
    /// <summary>Apple'ın imzalı kimlik token'ı (JWT).</summary>
    public string IdentityToken { get; set; } = string.Empty;

    /// <summary>Uygulamanın ürettiği nonce'un açık hâli; Apple'a özeti gitti.</summary>
    public string RawNonce { get; set; } = string.Empty;

    /// <summary>Ad ve soyad; Apple yalnız ilk girişte verir.</summary>
    public string? GivenName { get; set; }

    public string? FamilyName { get; set; }
}
