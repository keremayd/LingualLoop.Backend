namespace Common.Options;

/// <summary>
/// İçerik girişi uçlarının paylaşılan anahtarı.
///
/// **Neden JWT yetmiyor.** Kart oluşturma uçları veriye **yazıyor** ve
/// S3'e dosya koyuyor; oysa `[Authorize]` kayıt olmuş **herkesi** geçirir.
/// Uygulamada rol sistemi yok (bkz. §6 güvenlik borcu), o yüzden içerik
/// girişi ayrı bir sırla korunuyor.
///
/// **Boşsa uç kapalıdır**, açık değil. Anahtarsız kalınca kimlik denetimini
/// atlamak (fail-open) bu ucu internete açık bir yükleme kapısına çevirirdi.
/// </summary>
public class KartyAdminOptions
{
    public const string SectionName = "KartyAdmin";

    /// <summary>
    /// `X-Karty-Admin-Key` başlığında beklenen değer. Yapılandırmada boşsa
    /// uç 503 döner.
    /// </summary>
    public string ApiKey { get; set; } = string.Empty;
}
