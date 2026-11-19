namespace Service.DataTransferObjects.Responses.Karty;

public class GetKartyMeaningsResponse
{
    public string Locale { get; set; } = string.Empty;
    public int MissingCount { get; set; }
    public List<KartyMeaningItemResponse> Items { get; set; } = [];
}

public class KartyMeaningItemResponse
{
    public int KartyId { get; set; }
    public int? UnitSequence { get; set; }
    public int? UnitOrder { get; set; }
    public string NounText { get; set; } = string.Empty;
    public string Article { get; set; } = string.Empty;
    public string? Meaning { get; set; }
}
