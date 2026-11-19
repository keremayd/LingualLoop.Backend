namespace Service.DataTransferObjects.Responses.Karty;

public class UpsertKartyUnitResponse
{
    public int Sequence { get; set; }
    /// <summary>Ünitenin bütün dillerdeki adı (<c>"tr" → "Evde", "sq" → "Në shtëpi"</c>).</summary>
    public Dictionary<string, string> Titles { get; set; } = new();
    public int? EmblemKartyId { get; set; }
    public int WordCount { get; set; }
    public bool Created { get; set; }
}
