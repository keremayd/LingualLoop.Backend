namespace Service.DataTransferObjects.Responses;

public class LeagueLeaderboardEntryResponse
{
    public int Rank { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public int Points { get; set; }
    public bool IsCurrentUser { get; set; }
    public string? ProfilePhotoUrl { get; set; }
}
