namespace Service.DataTransferObjects.Responses.Karty;

public class RecordKartyIntroductionResponse
{
    public int KartyId { get; set; }

    /// <summary>
    /// Tanışmanın kaydedildiği an. Aynı kart ikinci kez onaylanırsa **ilk**
    /// tanışmanın damgası döner — işlem tekrarlanabilir (idempotent).
    /// </summary>
    public DateTime IntroducedDate { get; set; }

    /// <summary>Tanışma kaydedildi mi; günlük tavan kalktığından beri hep <c>true</c>.</summary>
    public bool Recorded { get; set; }
}
