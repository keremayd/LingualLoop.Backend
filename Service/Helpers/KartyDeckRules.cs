namespace Service.Helpers;

/// <summary>
/// Karty Yolu'nun destesi — **tek doğruluk kaynağı**.
///
/// Bir ünitenin kelimeleri ünite sırasıyla (<c>unit_order</c>, sonra
/// <c>karty_id</c>) 8'erli destelere bölünür; deste herkes için aynı
/// kelimelerdir. Patikanın düğümleri bu destelerdir, günler değil: gün
/// düğümleri o gün ne kadar oynandığına göre 1, 2, 6 kelimelik düzensiz
/// takvim kayıtlarına dönüşüyordu (kullanıcı kararı, 4 Kas 2026).
///
/// **Bir oyun bir destedir.** Oyun, destenin kelimeleri tanışılıp en az bir
/// kez doğru bilinince (kutu ≥ 1) ve oyunun tekrar payı dolunca (ya da
/// vadesi gelen kalmayınca) biter; istemci o an özeti gösterir.
///
/// **Tekrarlar oyunun başına yığılmaz** (5 Kas 2026, kullanıcı kararı):
/// deste oyununda en çok <see cref="ReviewsPerDeckGame"/> tekrar, yeni
/// kelimelerin arasına karışık gelir; desteler bitince açılan yalnız tekrar
/// oyununda en çok <see cref="ReviewsPerReviewGame"/>. Kalanlar sonraki
/// oyunlara kalır — uzun aradan dönen kullanıcının oyunu bir tekrar
/// maratonuna dönmez. Duolingo da tekrarı derslere karıştırıyor, borç listesi
/// göstermiyor; vadeleri ve ünite kapısını ise biz koruyoruz.
///
/// İstekli kullanıcı aynı gün sıradaki desteye geçebilir — günlük yeni
/// kelime tavanı bunun için kalktı. Yükü ünite kapısı sınırlıyor: kelime
/// ancak ertesi günkü tekrarda pekişir (<see cref="KartyUnitRules"/>), yani
/// bir günde en fazla bir ünitenin kelimeleri açılabilir. Bilet de oyun
/// başına alınıyor.
/// </summary>
public static class KartyDeckRules
{
    public const int Size = 8;

    /// <summary>Bir deste oyununda en çok kaç tekrar sorulur.</summary>
    public const int ReviewsPerDeckGame = 8;

    /// <summary>Yalnız tekrar oyununda (açık desteler bitmiş) en çok kaç tekrar sorulur.</summary>
    public const int ReviewsPerReviewGame = 15;

    public static int ReviewQuota(bool deckGame) =>
        deckGame ? ReviewsPerDeckGame : ReviewsPerReviewGame;

    /// <summary>
    /// Sıradaki kart tekrar mı olsun: vadesi gelen var, oyunun payı dolmamış
    /// ve tekrar yeni kelimelerin önüne geçmiyor — destenin tanışılan her
    /// kelimesine en fazla bir tekrar düşer, ikisi böylece karışık gelir.
    /// Destenin işi bittiyse kalan pay tekrarlarla dolar.
    /// </summary>
    public static bool IsReviewTurn(
        int dueCount,
        int reviewsServed,
        int quota,
        bool deckWorkLeft,
        int deckSeen) =>
        dueCount > 0 &&
        reviewsServed < quota &&
        (!deckWorkLeft || reviewsServed <= deckSeen);

    /// <summary>
    /// Oyun bitti mi: deste bitti (yalnız tekrar oyununda deste yok, bitmiş
    /// sayılır) ve ya vadesi gelen kalmadı ya da oyunun tekrar payı doldu.
    /// </summary>
    public static bool IsGameComplete(
        bool deckComplete,
        int dueReviews,
        int reviewsAnswered,
        int quota) =>
        deckComplete && (dueReviews == 0 || reviewsAnswered >= quota);

    /// <summary>Ünite içindeki sıradan (0'dan) deste numarası (1'den).</summary>
    public static int DeckOf(int position) => position / Size + 1;

    /// <summary>
    /// Deste bitti mi: bütün kelimeleri en az bir kez doğru bilinmiş. Yanlış
    /// bilinen kelime kutu 0'da kalır ve oturumda tekrar sorulur.
    /// </summary>
    public static bool IsComplete(int wordCount, int testedCount) =>
        wordCount > 0 && testedCount >= wordCount;

    /// <summary>Sıralı kelimeleri destelere böler.</summary>
    public static IReadOnlyList<IReadOnlyList<T>> Split<T>(IReadOnlyList<T> ordered)
    {
        var decks = new List<IReadOnlyList<T>>();
        for (var start = 0; start < ordered.Count; start += Size)
        {
            decks.Add(ordered.Skip(start).Take(Size).ToList());
        }

        return decks;
    }
}

/// <summary>
/// Oyunun kimliği: ünitenin sırası, ünite içindeki deste (yalnız tekrar
/// oyununda null) ve oyunda şimdiye kadar verilen tekrar kartı sayısı.
/// </summary>
public sealed record KartyGameKey(int UnitSequence, int? Deck, int ReviewsServed)
{
    public bool IsReviewGame => Deck is null;

    public int ReviewQuota => KartyDeckRules.ReviewQuota(deckGame: Deck is not null);
}

/// <summary>Seçilen kart ve hangi aşamadan geldiği: <c>review</c> · <c>test</c> · <c>new</c> · <c>practice</c>.</summary>
public sealed record KartyPick(int? KartyId, string Stage);
