using MediatR;
using Service.DataTransferObjects.Responses.Karty;

namespace Service.DataTransferObjects.Requests.Karty;

/// <summary>
/// Bir dildeki anlamları toplu yazar: <c>{"Tür": "derë", "Fenster": "dritare"}</c>.
/// Anahtar kartın Almanca adı (<c>noun_text</c>, büyük/küçük harf fark etmez);
/// kart ekleme ucu da adı benzersiz tutuyor. Boş değer o dildeki anlamı siler.
/// Çeviri kontrolden geçtikten sonra tek seferde yüklemek için.
/// </summary>
public class UpsertKartyMeaningsRequest : IRequest<UpsertKartyMeaningsResponse>
{
    public string Locale { get; set; } = string.Empty;
    public Dictionary<string, string?> Meanings { get; set; } = new();
}
