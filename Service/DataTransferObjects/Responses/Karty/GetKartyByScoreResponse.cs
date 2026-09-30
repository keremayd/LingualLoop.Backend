using System.Text.Json.Serialization;
using Common.Enums;

namespace Service.DataTransferObjects.Responses.Karty;

public class GetKartyByScoreResponse
{
    public int KartyId { get; set; }

    /// <summary>
    /// Kartın nasıl sunulacağı. **Metin olarak** serileştiriliyor: sayı
    /// gönderilseydi enum'a ileride yeni bir mod eklemek sıra değişikliğiyle
    /// istemcide sessizce yanlış moda dönüşebilirdi.
    /// </summary>
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public KartyCardMode Mode { get; set; } = KartyCardMode.Spelling;
    public string QuestionText { get; set; } = string.Empty;
    public string CorrectText { get; set; } = string.Empty;
    public string Article { get; set; } = string.Empty;
    public string KartyUrl { get; set; } = string.Empty;

    /// <summary>
    /// Kelimenin Almanca telaffuzu (imzalı URL). Ses henüz üretilmemişse
    /// <c>null</c> — o kartta telaffuz butonu **gösterilmez**.
    ///
    /// Boş string değil null: "" bir URL gibi görünüp istemcide boş istek
    /// tetikleyebilirdi; yokluğun tipte görünmesi daha güvenli.
    /// </summary>
    public string? AudioUrl { get; set; }
    public bool IsCorrect { get; set; }
    public int MinScore { get; set; }
    public int MaxScore { get; set; }
}
