namespace AwsService.Options;

public class AwsOptions
{
    public string AccessKey { get; set; } = string.Empty;
    public string SecretKey { get; set; } = string.Empty;
    public string Region { get; set; } = string.Empty;

    /// <summary>
    /// Konuşma sentezinin bölgesi. Boş bırakılırsa
    /// <see cref="DefaultSpeechRegion"/> kullanılır.
    ///
    /// **Neden S3'ten ayrı:** Polly'nin **neural** motoru her bölgede yok.
    /// Uygulamanın kovaları `eu-north-1`'de (Stockholm) ve orada neural
    /// desteklenmiyor — istek "The selected engine is not supported in this
    /// region" ile düşüyor. Standard motora inmek seçenek değil: Almanca'da
    /// belirgin robotik ve bu özelliğin bütün amacı telaffuz.
    ///
    /// Ayrı bölge maliyet ya da gecikme sorunu değil: ses arka planda bir
    /// kez üretiliyor, S3'e yüklendikten sonra bölgeyle ilgisi kalmıyor.
    /// </summary>
    public string SpeechRegion { get; set; } = string.Empty;

    /// <summary>Neural motorun desteklendiği, kovalara en yakın bölge.</summary>
    public const string DefaultSpeechRegion = "eu-central-1";
    public Dictionary<string, string> Buckets { get; set; } = new Dictionary<string, string>();
}