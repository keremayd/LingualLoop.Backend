using System.Linq.Expressions;
using Postgres.Models;

namespace Service.Helpers;

/// <summary>
/// Aralıklı tekrar kuralları — **tek doğruluk kaynağı**.
///
/// Saf fonksiyonlar: veritabanı bilmez, saat okumaz (zamanı parametre alır).
/// Böylece zamanlayıcıdan ve handler'lardan bağımsız test edilebiliyor.
///
/// ## Neden sayaç değil kutu
///
/// Eskiden "öğrenildi" tanımı <c>CorrectCount &gt; 0</c> idi: tek doğru cevap
/// yetiyordu ve tekrarların **arasında zaman şartı yoktu**. Aynı oturumda üç
/// kez doğru bilmek hiçbir şey kanıtlamaz; kanıtlayan şey ertesi gün de
/// bilmektir. Kutu tam olarak bunu ölçüyor.
///
/// Yan fayda: şans eseri doğrular kendiliğinden eleniyor. Karty ikili bir
/// soru olduğu için hiçbir şey bilmeyen kullanıcı %50 tutturur; o tahmin
/// kutuyu bir kademe ilerletir ama ertesi günkü tekrar onu yakalar ve kutu
/// sıfırlanır. Soru tipini değiştirmeden tahmin şansı sönümleniyor.
/// </summary>
public static class KartyLeitner
{
    /// <summary>Mezuniyet kutusu. Buradan sonra aralık uzamaz.</summary>
    public const int MaxBox = 5;

    /// <summary>Bir kelimenin artikeli kaç doğru cevapla öğrenilmiş sayılır.</summary>
    public const int ArticleLearningGoal = 5;

    /// <summary>
    /// Kutu 0: tanıştırıldı ama henüz doğru bilinmedi.
    /// Kutu 1–5: her doğru cevapta bir kademe, aralık uzayarak.
    /// </summary>
    private static readonly int[] IntervalDays = { 0, 1, 3, 7, 14, 30 };

    public static int IntervalOf(int box) =>
        IntervalDays[Math.Clamp(box, 0, MaxBox)];

    public static DateTime NextDue(int box, DateTime utcNow) =>
        utcNow.AddDays(IntervalOf(box));

    public static int Advance(int box) => Math.Min(box + 1, MaxBox);

    /// <summary>
    /// Yanlış cevapta kutu **1'e** döner, 0'a değil: 0 "hiç doğru bilinmedi"
    /// demek ve kart o hâle geri düşerse tanışma adımına dönerdi. Kullanıcı
    /// kelimeyle tanışmıştı; unuttuğu şey kelimenin kendisi değil yazımı.
    /// </summary>
    public static int Reset() => 1;

    /// <summary>
    /// Kutunun ilerleyip ilerlemeyeceği. **Vade dolmadan ilerlemez** — kural
    /// bu tek satırda: aynı oturumdaki ikinci doğru cevap kutuyu ilerletmez,
    /// yoksa aralıklı tekrar aralıksız bir sayaca dönerdi.
    /// </summary>
    public static bool IsDue(UserKartyLearning learning, DateTime utcNow) =>
        learning.DueDate is null || learning.DueDate <= utcNow;

    /// <summary>
    /// Tekrar vakti gelmiş kelime: en az bir kez doğru bilinmiş (kutu &gt; 0)
    /// ve vadesi dolmuş. Seçicinin 1. aşaması, ana ekrandaki "N kelimeyi
    /// tekrar et" sayısı ve Kelimelerim'in Tekrar filtresi **bu** tanımı
    /// kullanır; üçü bir dönem ayrı ayrı yazılmıştı.
    ///
    /// Puan bandı yok: içerik artık ünitelerle açılıyor (<see cref="KartyUnitRules"/>)
    /// ve vadesi gelen kelime puan düştü diye gizlenmemeli — aralıklı tekrarın
    /// bütün değeri unutmaya yakın anda sormakta.
    /// </summary>
    public static Expression<Func<UserKartyLearning, bool>> DueForReview(DateTime utcNow) =>
        learning => learning.Box > 0 && learning.DueDate <= utcNow;

    /// <inheritdoc cref="DueForReview"/>
    public static bool IsDueForReview(int box, DateTime? dueDate, DateTime utcNow) =>
        box > 0 && dueDate <= utcNow;
}
