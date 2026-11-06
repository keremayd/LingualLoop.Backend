namespace Service.DataTransferObjects.Responses.Karty;

public class AcknowledgeKartyUnitUnlockResponse
{
    /// <summary>Kutlaması gösterilmiş en ileri ünitenin sırası; kayıt yoksa 0.</summary>
    public int AnnouncedSequence { get; set; }
}
