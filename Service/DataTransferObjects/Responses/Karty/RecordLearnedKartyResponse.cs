namespace Service.DataTransferObjects.Responses.Karty;

public class RecordLearnedKartyResponse
{
    public int KartyId { get; set; }
    public int CorrectCount { get; set; }
    public int LearnedWordCount { get; set; }
}
