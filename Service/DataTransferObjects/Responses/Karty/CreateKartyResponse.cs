namespace Service.DataTransferObjects.Responses.Karty;

public class CreateKartyResponse
{
    public int KartyId { get; set; }
    public string NounText { get; set; } = string.Empty;
    public string Article { get; set; } = string.Empty;
    public int MinScore { get; set; }
    public int MaxScore { get; set; }

    /// <summary>Görselin S3 anahtarı (imzalı adres değil).</summary>
    public string KartyUrl { get; set; } = string.Empty;

    /// <summary>
    /// Sesin S3 anahtarı. Üretim düştüyse <c>null</c> — kart yine de
    /// kullanılabilir, `KartyAudioSyncJob` sonraki turunda tamamlar.
    /// </summary>
    public string? AudioUrl { get; set; }

    /// <summary>Var olan satır güncellendi mi, yeni satır mı açıldı.</summary>
    public bool Updated { get; set; }

    /// <summary>
    /// Sesin bu çağrıda üretilip üretilmediği. `false` ise
    /// <see cref="Warning"/> sebebi taşıyor.
    /// </summary>
    public bool AudioGenerated { get; set; }

    /// <summary>
    /// Ölümcül olmayan sorun. Çağıran taraf (Claude oturumu) bunu kullanıcıya
    /// **göstermeli**: aksi hâlde sessiz bir eksik kalır.
    /// </summary>
    public string? Warning { get; set; }

    /// <summary>Doğrulama için imzalı önizleme adresleri.</summary>
    public string ImagePreviewUrl { get; set; } = string.Empty;

    public string? AudioPreviewUrl { get; set; }
}
