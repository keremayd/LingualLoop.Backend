using MediatR;
using Service.DataTransferObjects.Responses.Karty;

namespace Service.DataTransferObjects.Requests.Karty;

/// <summary>
/// Oturumun durumu: destesi bitti mi, vadesi gelmiş tekrar kaldı mı.
/// <see cref="Deck"/> null ise oturum yalnız tekrardır (açık bütün desteler
/// bitmiş, sıradaki ünite kapalı).
/// </summary>
public class GetKartySessionStatusRequest : IRequest<GetKartySessionStatusResponse>
{
    public string UserId { get; set; } = string.Empty;
    public int UnitSequence { get; set; }
    public int? Deck { get; set; }

    /// <summary>Oyunda cevaplanan tekrar kartı sayısı; oyunun tekrar payıyla karşılaştırılır.</summary>
    public int ReviewsAnswered { get; set; }
}
