namespace Service.DataTransferObjects.Responses.Karty;

public class RecordKartyIntroductionResponse
{
    public int KartyId { get; set; }

    /// <summary>
    /// Tanışmanın kaydedildiği an. Aynı kart ikinci kez onaylanırsa **ilk**
    /// tanışmanın damgası döner — işlem tekrarlanabilir (idempotent).
    /// </summary>
    public DateTime IntroducedDate { get; set; }

    /// <summary>
    /// Bugün kaç kelimeyle tanışıldı ve tavan kaç.
    ///
    /// İstemci bu ikisini bilmeden kotanın dolduğunu anlayamaz: kart destesi
    /// önceden çekildiği için elindeki tanışma kartı **eskimiş** olabilir.
    /// </summary>
    public int IntroducedToday { get; set; }

    public int DailyLimit { get; set; }

    /// <summary>
    /// Tanışma kaydedildi mi. Kota doluyken gelen yeni bir kart <c>false</c>
    /// döner: kullanıcı kartı görmüştür ama kelime bugüne yazılmaz, ileride
    /// yeniden tanıştırılır.
    /// </summary>
    public bool Recorded { get; set; }
}
