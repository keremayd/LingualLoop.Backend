namespace Service.DataTransferObjects.Responses.Karty;

public class UpsertKartyUnitResponse
{
    public int Sequence { get; set; }
    public string Title { get; set; } = string.Empty;
    public int? EmblemKartyId { get; set; }
    public int WordCount { get; set; }
    public bool Created { get; set; }
}
