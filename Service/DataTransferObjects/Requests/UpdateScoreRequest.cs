using MediatR;
using Service.DataTransferObjects.Responses;

namespace Service.DataTransferObjects.Requests;

public class UpdateScoreRequest: IRequest<UpdateScoreResponse>
{
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// Cevabın **yönü**: doğruda <c>+1</c>, yanlışta <c>-1</c>.
    ///
    /// Bir dönem boost açıkken istemci burada <c>3</c> gönderiyordu ve bu tek
    /// sayı üç sayaca birden yazılıyordu — görünen puan, lig puanı **ve**
    /// gizli zorluk termostatı. Yani bir ödül çarpanı bir **ölçümü**
    /// çarpıyordu. Çarpan artık <see cref="BoostActive"/> ile ayrı geliyor;
    /// hangi sayaca ne yazılacağına sunucu karar veriyor.
    /// </summary>
    public int Point { get; set; }

    /// <summary>
    /// Cevap verilirken boost açık mıydı. Yalnız **ödül** sayaçlarını
    /// (görünen puan ve lig puanı) çarpar, termostata dokunmaz.
    /// </summary>
    public bool BoostActive { get; set; }

    public int? KartyId { get; set; }
}
