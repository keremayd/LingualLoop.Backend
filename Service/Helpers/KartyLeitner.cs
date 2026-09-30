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

    /// <summary>
    /// Kutu 0: tanıştırıldı ama henüz doğru bilinmedi.
    /// Kutu 1–5: her doğru cevapta bir kademe, aralık uzayarak.
    /// </summary>
    private static readonly int[] IntervalDays = { 0, 1, 3, 7, 14, 30 };

    /// <summary>
    /// Günde kaç **yeni** kelime tanıştırılabilir.
    ///
    /// 8 seçildi çünkü denge durumunda günlük tekrar yükü
    /// <c>N × (1/1 + 1/3 + 1/7 + 1/14 + 1/30) ≈ N × 1.58</c> oluyor; 8 yeni
    /// kelime ≈ 13 tekrar, toplam ≈ 21 kart. 20 yeni kelime seçilseydi günde
    /// ~52 kart ederdi — insan davranışına aykırı bir yük.
    ///
    /// **Bu bir duvar değil, öncelik.** Kota dolduğunda oyun bitmez; kullanıcı
    /// tanıştığı kelimelerle sonsuza kadar pratik yapmaya devam eder
    /// (<see cref="KartyScheduler"/>, 3. aşama). Yalnızca *yeni* kelime akışı
    /// yavaşlar — yarınki tekrar yükü şişmesin diye.
    /// </summary>
    public const int DailyNewWordLimit = 8;

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
    /// Doğru cevabın **zorluk termostatını** oynatmaya yetip yetmediği.
    ///
    /// Termostat (<c>user_scores.score</c>) kart bandını seçiyor, yani
    /// "bu zorluk sana kolay geliyor" çıkarımını yapıyor. O çıkarım
    /// kartlar **rastgele** dağıtılırken kurulmuştu; zamanlayıcı geldikten
    /// sonra havuz ağırlıkla kullanıcının **zaten bildiği** kelimelerden
    /// oluşuyor. Aynı formül artık aynı girdiyi almıyor: yanlı bir örneklemi
    /// okuyan termostat kaçınılmaz olarak yukarı kayar.
    ///
    /// Bu yüzden yalnız **kanıt** sayılır:
    /// <list type="bullet">
    ///   <item>kartın ilk testi (kutu 0) — kelimeyi gerçekten ilk kez bildi;</item>
    ///   <item>vadesi gelmiş tekrar — aradan zaman geçmişken hatırladı.</item>
    /// </list>
    ///
    /// Vadesi gelmemiş bir kartı aynı gün tekrar bilmek pratiktir, ölçüm
    /// değil: XP, lig puanı ve seri kazandırır ama zorluğu oynatmaz. Kural,
    /// <see cref="IsDue"/> ile kutunun ilerlemesini yöneten kuralın aynısı —
    /// iki sistem tek kapıdan geçsin diye böyle.
    /// </summary>
    public static bool CountsAsEvidence(UserKartyLearning? learning, DateTime utcNow) =>
        learning is null || learning.Box == 0 || IsDue(learning, utcNow);
}
