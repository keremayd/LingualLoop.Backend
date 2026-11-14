namespace Service.DataTransferObjects.Responses;

/// <summary>Günün bütün görevleri bitince toplanan seri koruması.</summary>
public class QuestFreezeBonusResponse
{
    /// <summary>Bugün biten görev sayısı.</summary>
    public int Completed { get; set; }

    /// <summary>Bugünün görev sayısı.</summary>
    public int Total { get; set; }

    /// <summary>Bugünkü koruma toplandı mı.</summary>
    public bool Claimed { get; set; }

    public int FreezeCount { get; set; }

    /// <summary>Tutulabilecek en fazla koruma: ücretsiz 1, premium 2.</summary>
    public int FreezeCap { get; set; }
}
