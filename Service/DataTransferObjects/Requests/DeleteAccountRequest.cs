using MediatR;
using Service.DataTransferObjects.Responses;

namespace Service.DataTransferObjects.Requests;

public class DeleteAccountRequest : IRequest<DeleteAccountResponse>
{
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// Apple ile bağlı hesapta silme anında Apple penceresinden alınan tek
    /// kullanımlık yetki kodu; sunucu bununla Apple'daki izni geri alır.
    /// </summary>
    public string? AppleAuthorizationCode { get; set; }

    /// <summary>
    /// İstemci Apple penceresini açamıyor (Android): hesap, Apple'daki izin
    /// geri alınmadan silinir.
    /// </summary>
    public bool SkipAppleRevocation { get; set; }
}

/// <summary><c>DELETE users/{id}/account</c> gövdesi; eski istemci göndermez.</summary>
public class DeleteAccountBody
{
    public string? AppleAuthorizationCode { get; set; }
    public bool SkipAppleRevocation { get; set; }
}
