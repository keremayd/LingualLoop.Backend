using MediatR;
using Service.DataTransferObjects.Responses.Karty;

namespace Service.DataTransferObjects.Requests.Karty;

/// <summary>
/// Ünite yoksa açar (başlık zorunlu), varsa verilen alanları günceller.
/// </summary>
public class UpsertKartyUnitRequest : IRequest<UpsertKartyUnitResponse>
{
    public int Sequence { get; set; }
    public string? Title { get; set; }
    public int? EmblemKartyId { get; set; }
}
