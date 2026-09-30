namespace Service.Helpers;

/// <summary>
/// Karty telaffuz seslerinin kuralları — **tek doğruluk kaynağı**.
///
/// Üretim işi de, sunum tarafı da bu sabitleri okur; ses ya da dosya adı
/// sözleşmesi iki yerde ayrı yazılsaydı biri değiştiğinde diğeri sessizce
/// yanlış dosyayı arardı.
/// </summary>
public static class KartyAudioRules
{
    /// <summary>
    /// Seslendirme dili.
    /// </summary>
    public const string LanguageCode = "de-DE";

    /// <summary>
    /// Seçilen ses: **Vicki**, neural.
    ///
    /// **Değiştirilmemeli.** Ses zamanla uygulamanın kimliğinin parçası olur
    /// (maskot gibi); sonradan değiştirmek üretilmiş bütün dosyaları
    /// geçersiz kılar ve kullanıcı bir gün başka biri konuşuyormuş gibi
    /// hisseder. Değiştirilecekse tüm havuz yeniden üretilmeli, karışık
    /// bırakılmamalı.
    /// </summary>
    public const string VoiceId = "Vicki";

    public const string ContentType = "audio/mpeg";

    /// <summary>
    /// S3 anahtarı: kartın **kendi klasörünün içinde**.
    ///
    /// Görseller `assets/{kartyId}/...` altında duruyor ve ses de o kartın
    /// bir varlığı — görselden farkı yok. Ayrı bir kök klasör (`audio/...`)
    /// ikinci bir sözleşme demekti: kart silinince iki yerde temizlik
    /// gerekir, `assets/10/` listelendiğinde kartın yarısı görünürdü.
    ///
    /// Dosya adı görselin sözleşmesini izliyor (`karty_10.png`) ama
    /// **rolünü de söylüyor**: `karty_10_listening.mp3`. Yalnız uzantı
    /// farkıyla ayrılsalardı bir kartın ileride ikinci bir sesi olduğunda
    /// (örnek cümle, yavaş telaffuz) ad çakışırdı; rol adın parçası olunca
    /// yeni dosya `karty_10_sentence.mp3` diye eklenebiliyor.
    /// </summary>
    public static string KeyFor(int kartyId) =>
        $"{KartyAssetRules.FolderFor(kartyId)}/karty_{kartyId}_listening.mp3";

    /// <summary>
    /// Seslendirilecek metin: **artikel + isim**.
    ///
    /// Almancada isim artikeliyle birlikte öğrenilir; "Zahnbürste" tek
    /// başına eksik bilgidir. Bu aynı zamanda Artikel Pusulası'nı da
    /// besliyor — kullanıcı doğru artikeli duyarak da öğreniyor.
    /// </summary>
    public static string TextFor(string article, string nounText)
    {
        var trimmedArticle = article.Trim();
        var trimmedNoun = nounText.Trim();

        if (string.IsNullOrEmpty(trimmedArticle)) return trimmedNoun;
        if (string.IsNullOrEmpty(trimmedNoun)) return string.Empty;

        return $"{trimmedArticle} {trimmedNoun}";
    }
}
