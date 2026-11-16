using MediatR;
using Service.DataTransferObjects.Responses.Karty;

namespace Service.DataTransferObjects.Requests.Karty;

/// <summary>
/// Kartı satırı ve S3 varlıklarıyla (görsel + telaffuz) siler. Kullanıcı
/// kaydı (öğrenme ya da cevap geçmişi) olan kart yalnız <see cref="Force"/>
/// ile silinir ve o kayıtlar da silinir.
/// </summary>
public class DeleteKartyRequest : IRequest<DeleteKartyResponse>
{
    public int KartyId { get; set; }
    public bool Force { get; set; }
}
