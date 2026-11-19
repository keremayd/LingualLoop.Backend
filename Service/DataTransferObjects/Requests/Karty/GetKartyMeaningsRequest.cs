using MediatR;
using Service.DataTransferObjects.Responses.Karty;

namespace Service.DataTransferObjects.Requests.Karty;

/// <summary>
/// Bütün kartlar ve bir dildeki anlamları, patika sırasıyla. Çeviriyi
/// kontrole göndermek ve eksik anlamı görmek için (anlamı olmayanda <c>null</c>).
/// </summary>
public class GetKartyMeaningsRequest : IRequest<GetKartyMeaningsResponse>
{
    public string Locale { get; set; } = string.Empty;
}
