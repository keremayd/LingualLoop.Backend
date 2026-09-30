using MediatR;
using Service.DataTransferObjects.Responses.Karty;

namespace Service.DataTransferObjects.Requests.Karty;

/// <summary>
/// Tek çağrıda tam bir Karty kartı üretir: satır, görsel ve telaffuz.
///
/// **Neden `IFormFile` değil `byte[]`.** Handler `Service` katmanında ve o
/// katman ASP.NET'i tanımıyor; dosyayı akıştan okuma işi controller'ın.
/// Böylece handler bir konsol aracından ya da testten de çağrılabiliyor.
/// </summary>
public class CreateKartyRequest : IRequest<CreateKartyResponse>
{
    /// <summary>Artikelsiz isim: "Zahnbürste".</summary>
    public string NounText { get; set; } = string.Empty;

    /// <summary>"der" | "die" | "das".</summary>
    public string Article { get; set; } = string.Empty;

    /// <summary>
    /// Zorluk bandı. `GetKartyByScore` kullanıcının skoruna göre bu aralıktan
    /// seçim yapıyor; §8'deki açık borç yüzünden şu an havuzda yalnız 0 ve
    /// 100 bantları var.
    /// </summary>
    public int MinScore { get; set; }

    public int MaxScore { get; set; }

    public byte[] ImageContent { get; set; } = Array.Empty<byte>();

    /// <summary>Uzantı çıkarmak için; içerik türü buradan türetiliyor.</summary>
    public string ImageFileName { get; set; } = string.Empty;

    /// <summary>
    /// Aynı isim zaten varsa hata vermek yerine görseli/sesi **yenile**.
    ///
    /// Varsayılan `false`: çizim yenilendiğinde yeni bir satır açmak,
    /// kullanıcının o kelimeyle ilgili tekrar geçmişini (`user_karty_learning`)
    /// koparır — kelime "hiç görülmemiş" hâle döner.
    /// </summary>
    public bool OverwriteExisting { get; set; }
}
