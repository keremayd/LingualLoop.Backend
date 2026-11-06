namespace Service.DataTransferObjects.Responses.Karty;

/// <summary>
/// Oyunun "bitti mi" sorusunun cevabı ve özet ekranının sunucudan gelen
/// kısmı. Puan, doğru sayısı ve rövanşa kalanlar istemcide toplanır.
/// </summary>
public class GetKartySessionStatusResponse
{
    /// <summary>
    /// Oyun bitti: deste bitti (her kelimesi en az bir kez doğru) ve ya
    /// vadesi gelen tekrar kalmadı ya da oyunun tekrar payı doldu
    /// (<c>KartyDeckRules.IsGameComplete</c>).
    /// </summary>
    public bool Complete { get; set; }

    public int DueReviews { get; set; }

    public int DeckSize { get; set; }
    public int DeckIntroduced { get; set; }
    public int DeckTested { get; set; }

    /// <summary>Destenin bütün kelimeleri, deste sırasıyla; görülmemişin kutusu 0.</summary>
    public List<KartyPathWordResponse> Words { get; set; } = new();

    /// <summary>Ünitenin şimdiki pekişen kelime sayısı (özetteki çubuğun vardığı yer).</summary>
    public int UnitConsolidated { get; set; }

    public int UnitWordCount { get; set; }
}
