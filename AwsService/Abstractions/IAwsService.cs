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

    /// <summary>
    /// Klasörün (öneki '/' ile biten anahtarların) altındaki bütün nesneleri
    /// siler; hesap silinince profil fotoğrafları için. Önek bir klasör
    /// değilse hata verir: boş ya da eksik önek kovanın tamamını kapsardı.
    /// </summary>
    Task DeleteFolderAsync(string prefix, BucketType bucketType = BucketType.ProfilePhotos);
}