using Common.Enums;
using Postgres.Models;

namespace Service.Helpers;

/// <summary>
/// Bir kartın hangi modda sunulacağına karar veren **tek nokta**.
///
/// Kural buraya toplandı çünkü sunum kararı zamanla büyüyecek: bugün
/// "tanıştı mı", yarın "tekrar zamanı geldi mi" (Leitner kutusu), sonra
/// "hangi soru tipi". Bunlar handler'ın içine dağılırsa kural okunamaz hâle
/// gelir ve iki ayrı yerde farklı davranmaya başlar.
///
/// Karar **yalnızca** kullanıcının o kelimeyle geçmişine bakar; kartın
/// içeriğine ya da puan bandına değil. O yüzden kart seçiminden bağımsız
/// test edilebiliyor.
/// </summary>
public static class KartyPresentationPolicy
{
    /// <param name="learning">
    /// Kullanıcının bu kartla geçmişi. <c>null</c> ise kelimeyle hiç
    /// karşılaşmamış demektir.
    /// </param>
    /// <param name="hasAudio">Kartın sesi üretilmiş mi; yoksa dinleme sorulamaz.</param>
    public static KartyCardMode Decide(UserKartyLearning? learning, bool hasAudio)
    {
        // Satırın **varlığı** yetmez: satır bir zamanlar yalnız doğru cevapta
        // oluşturuluyordu, yani eski kayıtlarda tanışma anı hiç yaşanmamış
        // olabilir. Tanışmanın kendi damgası var.
        return learning?.IntroducedDate is null
            ? KartyCardMode.Introduce
            : ForBox(learning.Box, hasAudio);
    }

    /// <summary>
    /// Kelime pekiştikçe zorlaşan soru sırası: önce anlam, sonra artikel,
    /// sonra yazım, en son dinleme. Yeni öğrenen hiçbir zaman yanlış yazılmış
    /// bir kelime görmez.
    /// </summary>
    public static KartyCardMode ForBox(int box, bool hasAudio) => box switch
    {
        <= 0 => KartyCardMode.PickImage,
        1 => KartyCardMode.PickArticle,
        2 => KartyCardMode.ArrangeLetters,
        _ => hasAudio ? KartyCardMode.Listen : KartyCardMode.ArrangeLetters,
    };
}
