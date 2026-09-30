using Microsoft.EntityFrameworkCore;
using Postgres;

namespace Service.Helpers;

/// <summary>
/// Sıradaki kartın **hangisi** olacağına karar verir.
///
/// <code>
/// 1. Vadesi gelmiş tekrar var mı?      → unutmadan hatırlat
/// 2. Tanışıldı ama hiç test edilmedi?  → ilk testini ver (kutu 0 → 1)
/// 3. Günlük yeni kelime kotası var mı? → tanıştır
/// 4. Hiçbiri yoksa                     → tanıştığı kelimelerden rastgele (sonsuz)
/// </code>
///
/// ## Tavan bir duvar değil, öncelik
///
/// Günlük yeni kelime sınırı yalnızca **2. aşamayı** kapatır; 3. aşama
/// sonsuzdur. Karty'nin amacı ders vermek değil, kullanıcının kendi
/// seviyesindeki kelimeleri kaydırarak **unutmamasını** sağlamak — bu yüzden
/// "bugünlük bitti" diye bir hâl yok, kart hep var.
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
/// Sınıf sorguları yapar, **kuralları bilmez**: aralıklar ve tavan
/// <see cref="KartyLeitner"/>, gün sınırı <see cref="IstanbulDay"/> içinde.
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
    /// Seçilen kartın kimliği; puan bandında hiç kart yoksa <c>null</c>.
    /// </summary>
    public static async Task<int?> SelectKartyIdAsync(
        LingualLoopContext context,
        string userId,
        int userScore,
        DateTime utcNow,
        CancellationToken cancellationToken)
    {
        var bandKartyIds = context.Karty
            .Where(karty => karty.MinScore <= userScore && karty.MaxScore >= userScore)
            .Select(karty => karty.KartyId);

        var learningInBand = context.UserKartyLearnings
            .Where(learning =>
                learning.UserId == userId &&
                bandKartyIds.Contains(learning.KartyId));

        // 1 — Vadesi gelmiş tekrar. En geç kalmışlar önce gelsin ki borç
        // birikmesin; seçim o pencere içinden rastgele.
        var dueIds = await learningInBand
            .Where(learning => learning.Box > 0 && learning.DueDate <= utcNow)
            .OrderBy(learning => learning.DueDate)
            .Take(CandidateWindow)
            .Select(learning => learning.KartyId)
            .ToListAsync(cancellationToken);
        if (dueIds.Count > 0) return PickRandom(dueIds);

        // 2 — Tanışıldı ama hiç test edilmedi (kutu 0).
        //
        // Bu aşama olmadan tanışma ile ilk test arasında hiçbir bağ yoktu:
        // kutu 0 kartlar 1. aşamanın `Box > 0` şartına takılıyor, yalnızca
        // sonsuz pratik havuzunda diğer her şeyle birlikte rastgele
        // görünüyorlardı. Yani bugün tanışılan sekiz kelimenin her birinin
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
        var untestedIds = await learningInBand
            .Where(learning => learning.Box == 0)
            .OrderBy(learning => learning.IntroducedDate)
            .Take(CandidateWindow)
            .Select(learning => learning.KartyId)
            .ToListAsync(cancellationToken);
        if (untestedIds.Count >= IntroductionBuffer) return PickRandom(untestedIds);

        // 3 — Yeni kelime. Kota İstanbul gününe göre sayılıyor.
        var (dayStartUtc, dayEndUtc) = IstanbulDay.UtcRangeOf(utcNow);
        var introducedToday = await context.UserKartyLearnings
            .CountAsync(
                learning =>
                    learning.UserId == userId &&
                    learning.IntroducedDate >= dayStartUtc &&
                    learning.IntroducedDate < dayEndUtc,
                cancellationToken);

        var seenKartyIds = context.UserKartyLearnings
            .Where(learning => learning.UserId == userId)
            .Select(learning => learning.KartyId);

        if (introducedToday < KartyLeitner.DailyNewWordLimit)
        {
            var newKartyId = await context.Karty
                .Where(karty =>
                    karty.MinScore <= userScore &&
                    karty.MaxScore >= userScore &&
                    !seenKartyIds.Contains(karty.KartyId))
                .OrderBy(karty => EF.Functions.Random())
                .Select(karty => (int?)karty.KartyId)
                .FirstOrDefaultAsync(cancellationToken);
            if (newKartyId is not null) return newKartyId;
        }

        // 4 — Sonsuz pratik: kullanıcının **tanıştığı** bütün kelimeler.
        //
        // Kutu şartı **yok**. Bir dönem burada `Box > 0` aranıyordu ve o filtre
        // tanışma kartlarını dışarıda bırakıyordu: ANLADIM'a basılan kelime
        // `Box = 0` ile kaydedildiği için havuza hiç girmiyor, yani yeni
        // tanışılan kelime soru olarak asla geri gelmiyordu. Tanışmış ama
        // henüz cevaplanmamış kelime, pratiğe **en çok** ihtiyacı olandır.
        var practiceIds = await learningInBand
            .Select(learning => learning.KartyId)
            .ToListAsync(cancellationToken);
        if (practiceIds.Count > 0) return PickRandom(practiceIds);

        // 5 — İlk oturum: hiç öğrenme kaydı yok, banttan rastgele.
        //
        // Yalnız bu durumda banda dokunuluyor. Kota dolduğunda buraya
        // düşülseydi tanışmamış bir kart seçilir ve tavan aşılırdı.
        return await context.Karty
            .Where(karty => karty.MinScore <= userScore && karty.MaxScore >= userScore)
            .OrderBy(karty => EF.Functions.Random())
            .Select(karty => (int?)karty.KartyId)
            .FirstOrDefaultAsync(cancellationToken);
    }

    private static int PickRandom(IReadOnlyList<int> ids) =>
        ids[Random.Shared.Next(ids.Count)];
}
