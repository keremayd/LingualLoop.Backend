namespace Service.DataTransferObjects.Responses.Karty;

/// <summary>Görsel seçilen sorularda bir seçenek.</summary>
public class KartyOptionResponse
{
    public int KartyId { get; set; }
    public string KartyUrl { get; set; } = string.Empty;
}
