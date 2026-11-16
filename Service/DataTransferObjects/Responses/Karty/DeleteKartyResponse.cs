namespace Service.DataTransferObjects.Responses.Karty;

public class DeleteKartyResponse
{
    public int KartyId { get; set; }
    public string NounText { get; set; } = string.Empty;
    public string Article { get; set; } = string.Empty;

    /// <summary>Kartın silinmeden önceki ünitesi; ünitesizse <c>null</c>.</summary>
    public int? UnitSequence { get; set; }

    /// <summary>Kartın silinmeden önceki ünite içi sırası.</summary>
    public int? UnitOrder { get; set; }

    /// <summary>Silinen öğrenme kayıtları (kullanıcı başına bir satır); yalnız <c>force</c> ile sıfırdan büyük olabilir.</summary>
    public int DeletedLearningCount { get; set; }

    /// <summary>Silinen cevap geçmişi satırları; yalnız <c>force</c> ile sıfırdan büyük olabilir.</summary>
    public int DeletedHistoryCount { get; set; }

    /// <summary>Amblemi bu kart olan, amblemi boşalan ünitelerin sıraları.</summary>
    public List<int> ClearedEmblemUnits { get; set; } = new();

    /// <summary>
    /// S3'teki görsel ve ses silindi mi. <c>false</c> ise kart yine de
    /// silinmiştir; <see cref="Warning"/> sebebi taşır.
    /// </summary>
    public bool AssetsDeleted { get; set; }

    public string? Warning { get; set; }
}
