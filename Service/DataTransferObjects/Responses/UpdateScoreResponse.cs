namespace Service.DataTransferObjects.Responses;

public class UpdateScoreResponse
{
    public string UserId { get; set; } = string.Empty;

    /// <summary>Zorluk termostatı; iki yönlü. Kullanıcıya ham gösterilmez.</summary>
    public int? Score { get; set; }

    /// <summary>İlerleme sayacı; yalnızca artar. Kullanıcıya gösterilen sayı.</summary>
    public int? Experience { get; set; }

    public int? Lives { get; set; }
    public LeagueProgressResponse? League { get; set; }
}
