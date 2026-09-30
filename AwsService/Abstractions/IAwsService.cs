using Common.Enums;

namespace AwsService.Abstractions;

public interface IAwsService
{
    /// <summary>
    /// Dosyayı verilen kovaya yükler.
    ///
    /// Kova parametresi sonradan eklendi: metot yalnızca `ProfilePhotos`
    /// kovasına yazacak biçimde sabitlenmişti ve Karty sesleri için ikinci
    /// bir hedef gerekince ortaya çıktı. Varsayılan değer eski çağrı
    /// yerlerini bozmuyor.
    /// </summary>
    Task UploadFileAsync(
        string key,
        Stream fileStream,
        string contentType,
        BucketType bucketType = BucketType.ProfilePhotos);
    string GeneratePreSignedUrl(string key, BucketType bucketType, int expireMinutes = 120);
}