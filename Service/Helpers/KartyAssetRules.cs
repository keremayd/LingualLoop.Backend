namespace Service.Helpers;

/// <summary>
/// Bir Karty kartının S3'teki varlıklarının yol sözleşmesi — **tek doğruluk
/// kaynağı**.
///
/// Klasör buradan türetiliyor, <see cref="KartyAudioRules"/> de aynı
/// fonksiyonu kullanıyor. İki yerde ayrı yazılsaydı biri değiştiğinde
/// görsel bir klasörde, ses başka klasörde kalırdı.
/// </summary>
public static class KartyAssetRules
{
    /// <summary>Kartın bütün varlıklarının durduğu klasör.</summary>
    public static string FolderFor(int kartyId) => $"assets/{kartyId}";

    /// <summary>
    /// Kart görselinin anahtarı.
    ///
    /// Uzantı dışarıdan geliyor çünkü yüklenen dosya PNG olmayabilir; ama
    /// **ad kalıbı sabit**: `karty_{id}`. Rol eki yok, çünkü görsel kartın
    /// birincil varlığı — ses gibi ikincil varlıklar rolünü adında taşıyor
    /// (`karty_{id}_listening.mp3`).
    /// </summary>
    public static string ImageKeyFor(int kartyId, string extension)
    {
        var normalized = NormalizeExtension(extension);
        return $"{FolderFor(kartyId)}/karty_{kartyId}{normalized}";
    }

    /// <summary>
    /// Kabul edilen görsel uzantıları ve MIME karşılıkları.
    ///
    /// Beyaz liste, kara liste değil: tanımadığımız bir tür geldiğinde
    /// reddetmek, tanımadığımız bir türü S3'e koyup istemcinin çözememesini
    /// beklemekten iyi.
    /// </summary>
    public static readonly IReadOnlyDictionary<string, string> AllowedImageTypes =
        new Dictionary<string, string>
        {
            [".png"] = "image/png",
            [".jpg"] = "image/jpeg",
            [".jpeg"] = "image/jpeg",
            [".webp"] = "image/webp",
        };

    public static bool IsAllowedImageExtension(string extension) =>
        AllowedImageTypes.ContainsKey(NormalizeExtension(extension));

    public static string ContentTypeForImage(string extension) =>
        AllowedImageTypes[NormalizeExtension(extension)];

    private static string NormalizeExtension(string extension)
    {
        var trimmed = (extension ?? string.Empty).Trim().ToLowerInvariant();
        if (trimmed.Length == 0) return ".png";

        return trimmed.StartsWith('.') ? trimmed : $".{trimmed}";
    }
}
