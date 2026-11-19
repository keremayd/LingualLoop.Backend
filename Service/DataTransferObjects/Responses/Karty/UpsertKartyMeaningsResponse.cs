namespace Service.DataTransferObjects.Responses.Karty;

public class UpsertKartyMeaningsResponse
{
    public string Locale { get; set; } = string.Empty;

    /// <summary>Yeni yazılan ya da değişen anlam.</summary>
    public int Saved { get; set; }

    /// <summary>Boş değerle silinen anlam.</summary>
    public int Removed { get; set; }

    /// <summary>Zaten aynı olan anlam.</summary>
    public int Unchanged { get; set; }

    /// <summary>Böyle bir kart yok; yazılmadı.</summary>
    public List<string> UnknownNouns { get; set; } = [];

    /// <summary>Bu adla birden fazla kart var; hangisi olduğu belirsiz, yazılmadı.</summary>
    public List<string> AmbiguousNouns { get; set; } = [];

    /// <summary>Yazımdan sonra bu dilde anlamı olmayan kart sayısı (bütün kartlar içinde).</summary>
    public int MissingCount { get; set; }

    public int TotalCards { get; set; }
}
