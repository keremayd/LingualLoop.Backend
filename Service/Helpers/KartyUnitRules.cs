namespace Service.Helpers;

/// <summary>
/// Karty Yolu'nun ünite kuralları — **tek doğruluk kaynağı**. Saf
/// fonksiyonlar; veritabanı bilmez.
///
/// ## Ünite puanla değil öğrenmeyle açılır
///
/// Yeni içerik eskiden <c>user_scores.score</c> bandıyla açılıyordu ve puan
/// yanlış cevapta düştüğü için kullanıcı açtığı seviyeden **geri düşebiliyordu**.
/// Bir patika ileri gider, geri gitmez. Artık sıradaki ünite, bir öncekinin
/// kelimelerinin <see cref="UnlockRatio"/> kadarı pekişince açılıyor
/// (WaniKani'nin seviye kapısı; bkz. <c>docs/KARTY_PATIKA.md</c>).
/// </summary>
public static class KartyUnitRules
{
    /// <summary>Sıradaki ünitenin açılması için pekişmesi gereken oran.</summary>
    public const double UnlockRatio = 0.8;

    /// <summary>
    /// Pekişti sayılan en düşük kutu: kelime bir kez ilk testte, bir kez de
    /// vadesinde (ertesi gün) doğru bilinmiş. Kutu 1 yalnız "bir kez bildi"
    /// demek ve aynı gün tahminle de gelebiliyor.
    /// </summary>
    public const int ConsolidatedBox = 2;

    public static bool IsConsolidated(int box) => box >= ConsolidatedBox;

    /// <summary>Hedef kelime sayısı: 40 kelimelik ünitede 32.</summary>
    public static int RequiredConsolidated(int wordCount) =>
        (int)Math.Ceiling(wordCount * UnlockRatio - 1e-9);

    public static bool HasReachedGoal(UnitProgress progress) =>
        progress.WordCount > 0 &&
        progress.ConsolidatedCount >= RequiredConsolidated(progress.WordCount);

    /// <summary>
    /// Ünitelerin açık olup olmadığı, <paramref name="units"/> sırasıyla.
    ///
    /// Ünite 1 hep açık. Sonraki ünite üç yoldan açıktır: bir önceki açık
    /// ünite hedefe ulaşmışsa, içinden en az bir kelimeyle tanışılmışsa ya da
    /// açılması kaydedilmişse (<paramref name="recordedThroughIndex"/>,
    /// <c>user_karty_unit_state</c>). Son ikisi kilidin geri kapanmasını
    /// önlüyor: yanlış cevap kutuyu 1'e düşürünce önceki ünitenin oranı
    /// hedefin altına inebilir. Kayıt olmasaydı yeni üniteden ilk kelime
    /// gelene kadar geçen sürede ünite yeniden kilitlenirdi.
    /// </summary>
    public static bool[] UnlockStates(
        IReadOnlyList<UnitProgress> units,
        int recordedThroughIndex = -1)
    {
        var unlocked = new bool[units.Count];
        for (var i = 0; i < units.Count; i++)
        {
            unlocked[i] = i == 0 ||
                          i <= recordedThroughIndex ||
                          units[i].SeenCount > 0 ||
                          (unlocked[i - 1] && HasReachedGoal(units[i - 1]));
        }

        return unlocked;
    }

    /// <summary>
    /// Kayıttaki ünite sırasının (<c>unlocked_sequence</c>) listedeki konumu:
    /// sırası kayda eşit ya da küçük son ünite. Kayıt yoksa -1.
    /// </summary>
    public static int RecordedIndex(IReadOnlyList<int> sequences, int recordedSequence)
    {
        var index = -1;
        for (var i = 0; i < sequences.Count; i++)
        {
            if (sequences[i] <= recordedSequence) index = i;
        }

        return index;
    }

    /// <summary>
    /// Yeni açılan ünitelerin sıraları: açık, içinden hiç kelime görülmemiş
    /// ve kaydın ötesinde. Kelimesi görülmüş ünite zaten açıktı (eski bant
    /// döneminde tanışılanlar dahil); onun için kutlama gösterilmez.
    /// </summary>
    public static IReadOnlyList<int> NewlyUnlocked(
        IReadOnlyList<int> sequences,
        IReadOnlyList<UnitProgress> units,
        IReadOnlyList<bool> unlocked,
        int recordedSequence)
    {
        var newly = new List<int>();
        for (var i = 1; i < units.Count; i++)
        {
            if (unlocked[i] && units[i].SeenCount == 0 && sequences[i] > recordedSequence)
            {
                newly.Add(sequences[i]);
            }
        }

        return newly;
    }
}

/// <summary>Bir kullanıcının bir ünitedeki durumu.</summary>
/// <param name="WordCount">Ünitedeki kart sayısı.</param>
/// <param name="SeenCount">Öğrenme kaydı açılmış (tanışılmış) kart sayısı.</param>
/// <param name="ConsolidatedCount">Kutusu <see cref="KartyUnitRules.ConsolidatedBox"/> ve üstü olan kart sayısı.</param>
public readonly record struct UnitProgress(int WordCount, int SeenCount, int ConsolidatedCount);
