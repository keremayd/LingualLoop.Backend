using Microsoft.EntityFrameworkCore;
using Postgres;

namespace Service.Helpers;

/// <summary>
/// Sıradaki kartın **hangisi** olacağına karar verir.
///
/// <code>
/// 1. Tekrar sırası mı? (vadesi gelen, payı dolmamış) → unutmadan hatırlat
/// 2. Tanışıldı ama hiç test edilmedi?               → ilk testini ver (kutu 0 → 1)
/// 3. Oyunun destesinde yeni kelime var?             → tanıştır
/// 4. Deste bitti, test bekleyen var mı?             → onu pratikle karışık sor
/// 5. Hiçbiri yoksa                                  → tanıştığı kelimelerden rastgele (sonsuz)
/// </code>
///
/// Tekrar oyunun başına yığılmaz: oyunun payı kadar (<see cref="KartyDeckRules.ReviewQuota"/>)
/// ve destenin tanışılan kelimeleriyle sırayla gelir
/// (<see cref="KartyDeckRules.IsReviewTurn"/>). İstemci oyunda şimdiye kadar
/// verilen tekrar kartı sayısını gönderir; her kartın hangi aşamadan geldiği
/// yanıtta yazar (<see cref="KartyPick"/>).
///
/// ## Deste bir oyundur
///
/// Yeni kelime **oturumun destesinden** gelir (<see cref="KartyDeckRules"/>);
/// istemci oyunu açarken patikadaki şu anki desteyi gönderir. Destenin
/// kelimeleri bitince yeni kelime gelmez, kart yine hep var: istemci
/// destenin bittiğini <c>karty/session</c> ile öğrenip özeti gösterir,
/// kullanıcı isterse aynı oturumda pratiğe devam eder. Sıradaki deste
/// sıradaki oyundur. Günlük yeni kelime tavanı bunun yerine kalktı.
///
/// Kutu sistemi bu amaca karşı değil, tam olarak onun mühendisliği: unutma
/// eğrisi üstel olduğu için tekrarı unutmaya yakın vermek hatırlamayı uzatır.
/// Kutu *hangi* kartın geleceğini belirler, *kaç tane* geleceğini değil.
///
/// ## Hiçbir aşama deterministik değil
///
/// Her aşama rastgele seçer. Sebebi yaşanmış bir hata: "vadesi en erken olanı
/// ver" diye yazılan aşama tek kartta kilitlenmişti — erken cevap kutuyu
/// ilerletmediği için <c>DueDate</c> değişmiyor, aynı sorgu aynı kartı bir
/// daha döndürüyordu. **Deterministik sorgu + ilerlemeyen durum = sonsuz
/// döngü.** Önceliği korumak için sıralama kalıyor ama seçim bir pencere
/// içinden rastgele yapılıyor.
///
/// Sınıf sorguları yapar, **kuralları bilmez**: aralıklar
/// <see cref="KartyLeitner"/>, deste <see cref="KartyDeckRules"/>, ünite
/// kapısı <see cref="KartyUnitRules"/> içinde.
///
/// ## Puan bandı yok (Karty Yolu, 4 Kasım 2026)
///
/// Yeni kelime ünite sırasıyla geliyor, sıradaki ünite öğrenmeyle açılıyor.
/// Tekrar, test ve pratik aşamaları kullanıcının tanıştığı **bütün**
/// kelimelerden seçiyor. Eskiden her aşama kullanıcının zorluk puanının
/// bandıyla süzülüyordu; puan yanlışta düştüğü için bant vadesi gelmiş
/// tekrarları gizleyebiliyordu. Puan (termostat) emekli oldu, seçici onu
/// hiç okumuyor. Hiç ünite tanımlanmamış (boş) veritabanında yeni kelime
/// görülmemiş kartlardan rastgele gelir.
/// </summary>
public static class KartyScheduler
{
    /// <summary>
    /// Öncelikli aşamalarda kaç aday arasından rastgele seçileceği. Sıralama
    /// önceliği korur, pencere çeşitliliği sağlar.
    /// </summary>
    private const int CandidateWindow = 10;

    /// <summary>
    /// Tanışma ile ilk test arasına kaç kart girmesi gerektiği.
    ///
    /// Kelimeyi gördükten hemen sonra sormak kısa süreli belleği ölçer,
    /// öğrenmeyi değil — ekrandan yeni silinmiş bir kelimeyi herkes bilir.
    /// Bir kart arayla sorunca sıra şöyle akıyor:
    /// <c>tanış A · tanış B · test A · tanış C · test B …</c>
    ///
    /// Sayı aynı zamanda **tekrar kilidine karşı sigorta**: kutusu 0'da
    /// kalan (yani yanlış cevaplanan) tek bir kart varsa bu aşama devreye
    /// girmez ve kart, kendisini sonsuza kadar tekrar ettiremez.
    /// </summary>
    private const int IntroductionBuffer = 2;

    /// <summary>
    /// Aday havuzunun alt sınırı: istemcinin destesi (3) + önden çekilen kart
    /// (1). Havuz bundan dar olursa istemci aynı kartı tekrar tekrar alıp
    /// eler ve deste kurur (2. aşamadaki dersin aynısı). Destenin son
    /// kelimelerinde ve son testlerinde havuz bu yüzden genişletilir.
    /// </summary>
    private const int CandidateFloor = 4;

    /// <summary>
    /// Seçilen kart ve geldiği aşama; sunulacak hiç kart yoksa kimlik
    /// <c>null</c>. <paramref name="game"/> yoksa (eski istemci) vadesi gelen
    /// tekrarların hepsi önce gelir, yeni kelime görülmemiş kelimesi kalan ilk
    /// desteden.
    /// </summary>
    public static async Task<KartyPick> SelectAsync(
        LingualLoopContext context,
        string userId,
        DateTime utcNow,
        KartyGameKey? game,
        CancellationToken cancellationToken)
    {
        var learnings = context.UserKartyLearnings
            .Where(learning => learning.UserId == userId);
        var seenKartyIds = learnings.Select(learning => learning.KartyId);

        // Vadesi gelmiş tekrarlar. En geç kalmışlar önce gelsin ki borç
        // birikmesin; seçim o pencere içinden rastgele.
        var dueIds = await learnings
            .Where(KartyLeitner.DueForReview(utcNow))
            .OrderBy(learning => learning.DueDate)
            .Take(CandidateWindow)
            .Select(learning => learning.KartyId)
            .ToListAsync(cancellationToken);

        // Tanışıldı ama hiç test edilmedi (kutu 0).
        //
        // Bu aşama olmadan tanışma ile ilk test arasında hiçbir bağ yoktu:
        // kutu 0 kartlar tekrarın `Box > 0` şartına takılıyor, yalnızca
        // sonsuz pratik havuzunda diğer her şeyle birlikte rastgele
        // görünüyorlardı. Yani destede tanışılan sekiz kelimenin her birinin
        // bir kez sorulacağının **garantisi yoktu** — biri üç kez gelirken
        // bir diğeri hiç gelmeyebiliyordu.
        //
        // En eskiler önce (pencere), seçim pencere içinden **rastgele**.
        //
        // Burada bir tur `untestedIds[0]` yazılmıştı ve istemcinin destesini
        // kurutuyordu: sunucu hep aynı kartı önerince istemci onu kuyrukta
        // bulup eleniyor, beş denemenin sonunda boş dönüyor ve deste 3'ten
        // aşağı düşüyordu. Akış o yüzden "4 tanışma, sonra 4 pratik" diye
        // kümeleniyordu. Deterministik sorgu bu dosyada üçüncü kez sorun
        // çıkardı — **aşamaların hiçbiri tek bir kart döndürmemeli.**
        var untestedIds = await learnings
            .Where(learning => learning.Box == 0)
            .OrderBy(learning => learning.IntroducedDate)
            .Take(CandidateWindow)
            .Select(learning => learning.KartyId)
            .ToListAsync(cancellationToken);

        var units = await KartyUnitProgressReader.ReadAsync(context, userId, cancellationToken);
        // Oyunun destesi. Yalnız tekrar oyununda deste yok: yeni kelime gelmez.
        var deck = units.Count == 0 || game?.IsReviewGame == true
            ? DeckState.Empty
            : await DeckStateAsync(context, units, game, seenKartyIds, cancellationToken);

        // 1 — Tekrar. Oyun bilgisi varsa payla sınırlı ve destenin
        // kelimeleriyle sırayla; yoksa hepsi önce.
        var reviewTurn = game is null
            ? dueIds.Count > 0
            : KartyDeckRules.IsReviewTurn(
                dueIds.Count,
                game.ReviewsServed,
                game.ReviewQuota,
                // Tekrar oyununda deste yok; tekrarlar pay dolana kadar gelir.
                deckWorkLeft: !game.IsReviewGame &&
                              (untestedIds.Count > 0 || deck.Unseen.Count > 0),
                deckSeen: deck.SeenCount);
        if (reviewTurn) return new KartyPick(PickRandom(dueIds), "review");

        // 2 — İlk test (kutu 0 → 1).
        if (untestedIds.Count >= IntroductionBuffer)
        {
            return new KartyPick(PickRandom(untestedIds), "test");
        }

        // 3 — Yeni kelime: oyunun destesinden.
        if (units.Count == 0 && game?.IsReviewGame != true)
        {
            // Hiç ünite tanımlanmamış (boş veritabanı): deste yok, görülmemiş
            // herhangi bir kart.
            var anyNew = await context.Karty
                .Where(karty => !seenKartyIds.Contains(karty.KartyId))
                .OrderBy(karty => EF.Functions.Random())
                .Select(karty => (int?)karty.KartyId)
                .FirstOrDefaultAsync(cancellationToken);
            if (anyNew is not null) return new KartyPick(anyNew, "new");
        }
        else if (deck.Unseen.Count > 0)
        {
            // Destenin son kelimelerinde aday azalır; test bekleyenler de
            // havuza girer ki istemcinin destesi kurumasın.
            var pick = PickRandom(deck.Unseen.Count >= CandidateFloor
                ? deck.Unseen
                : deck.Unseen.Concat(untestedIds).Distinct().ToList());
            return new KartyPick(pick, untestedIds.Contains(pick) ? "test" : "new");
        }

        // 4 — Destenin yeni kelimesi kalmadı ama test bekleyen var (son
        // kelimeler ya da yanlış bilinip kutu 0'da kalanlar). Tek adayı her
        // seferinde döndürmek istemcinin destesini kurutur ve kelimeyi
        // arka arkaya sordurur; birkaç pratik kartıyla karıştırılır. Deste
        // ancak bunlar doğru bilinince biter.
        var practiceIds = await learnings
            .Select(learning => learning.KartyId)
            .ToListAsync(cancellationToken);
        if (untestedIds.Count > 0)
        {
            var mix = untestedIds
                .Concat(practiceIds
                    .Except(untestedIds)
                    .OrderBy(_ => Random.Shared.Next())
                    .Take(CandidateFloor - 1))
                .ToList();
            var pick = PickRandom(mix);
            return new KartyPick(pick, untestedIds.Contains(pick) ? "test" : "practice");
        }

        // 5 — Sonsuz pratik: kullanıcının **tanıştığı** bütün kelimeler.
        //
        // Kutu şartı **yok**. Bir dönem burada `Box > 0` aranıyordu ve o filtre
        // tanışma kartlarını dışarıda bırakıyordu: ANLADIM'a basılan kelime
        // `Box = 0` ile kaydedildiği için havuza hiç girmiyor, yani yeni
        // tanışılan kelime soru olarak asla geri gelmiyordu. Tanışmış ama
        // henüz cevaplanmamış kelime, pratiğe **en çok** ihtiyacı olandır.
        if (practiceIds.Count > 0) return new KartyPick(PickRandom(practiceIds), "practice");

        // 6 — Hiç öğrenme kaydı yok ve 3. aşama kart bulamadı (yalnız
        // kartı olmayan ünitelerde ya da ünitesiz kartlarda olur): son çare
        // olarak herhangi bir kart.
        var any = await context.Karty
            .OrderBy(karty => EF.Functions.Random())
            .Select(karty => (int?)karty.KartyId)
            .FirstOrDefaultAsync(cancellationToken);
        return new KartyPick(any, "new");
    }

    /// <summary>Oyunun destesi: kaç kelimesiyle tanışıldı, hangileri görülmedi.</summary>
    private sealed record DeckState(int SeenCount, List<int> Unseen)
    {
        public static readonly DeckState Empty = new(0, new List<int>());
    }

    /// <summary>
    /// Oyunun destesinin durumu. Oyun destesi verilmediyse (eski istemci)
    /// görülmemiş kelimesi kalan ilk deste. Deste kilitli bir ünitedeyse boş:
    /// yeni kelime yok; kullanıcı tekrar ve pratikle devam eder, ünite
    /// pekişince açılır.
    /// </summary>
    private static async Task<DeckState> DeckStateAsync(
        LingualLoopContext context,
        IReadOnlyList<KartyUnitSnapshot> units,
        KartyGameKey? game,
        IQueryable<int> seenKartyIds,
        CancellationToken cancellationToken)
    {
        var unit = game?.Deck is null
            ? units.FirstOrDefault(item => item.HasUnseen)
            : units.FirstOrDefault(item => item.Unit.Sequence == game.UnitSequence);
        if (unit is null || !unit.Unlocked) return DeckState.Empty;

        var cards = await context.Karty
            .Where(karty => karty.UnitId == unit.Unit.KartyUnitId)
            .OrderBy(karty => karty.UnitOrder ?? int.MaxValue)
            .ThenBy(karty => karty.KartyId)
            .Select(karty => new { karty.KartyId, Seen = seenKartyIds.Contains(karty.KartyId) })
            .ToListAsync(cancellationToken);

        int start;
        if (game?.Deck is int number)
        {
            start = (number - 1) * KartyDeckRules.Size;
        }
        else
        {
            var first = cards.FindIndex(card => !card.Seen);
            if (first < 0) return DeckState.Empty;
            start = (KartyDeckRules.DeckOf(first) - 1) * KartyDeckRules.Size;
        }

        var deck = cards.Skip(start).Take(KartyDeckRules.Size).ToList();
        return new DeckState(
            deck.Count(card => card.Seen),
            deck.Where(card => !card.Seen).Select(card => card.KartyId).ToList());
    }

    private static int PickRandom(IReadOnlyList<int> ids) =>
        ids[Random.Shared.Next(ids.Count)];
}
