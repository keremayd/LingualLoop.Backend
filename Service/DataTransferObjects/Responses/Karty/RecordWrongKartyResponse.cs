namespace Service.DataTransferObjects.Responses.Karty;

public class RecordWrongKartyResponse
{
    public string UserId { get; set; } = string.Empty;
    public int KartyId { get; set; }
    public int WrongCount { get; set; }
    public DateTime LastWrongDate { get; set; }
}
