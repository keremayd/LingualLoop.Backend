using MediatR;
using Service.DataTransferObjects.Responses.Karty;

namespace Service.DataTransferObjects.Requests.Karty;

/// <summary>
/// Ünite yoksa açar (en az bir dilde ad zorunlu), varsa verilen alanları
/// günceller.
/// </summary>
public class UpsertKartyUnitRequest : IRequest<UpsertKartyUnitResponse>
{
    public int Sequence { get; set; }
    public int? EmblemKartyId { get; set; }

    /// <summary>
    /// Ünitenin dillerdeki adı: <c>{"tr": "Evde", "sq": "Në shtëpi"}</c>.
    /// Verilmeyen dile dokunulmaz, boş değer o dildeki adı siler
    /// (<c>KartyTranslationWriter</c>).
    /// </summary>
    public Dictionary<string, string?> Titles { get; set; } = new();
}
