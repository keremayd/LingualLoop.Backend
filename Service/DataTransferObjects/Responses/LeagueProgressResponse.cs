namespace Service.DataTransferObjects.Responses;

public class LeagueProgressResponse
{
    public string LeagueKey { get; set; } = string.Empty;
    public string LeagueName { get; set; } = string.Empty;
    public int Rank { get; set; }
    public int Points { get; set; }
    public int MinPoints { get; set; }
    public int? MaxPoints { get; set; }
    public int? PointsToNextLeague { get; set; }
    public double ProgressRatio { get; set; }
    public int SeasonKey { get; set; }
    public DateTime SeasonStartsAtUtc { get; set; }
    public DateTime SeasonEndsAtUtc { get; set; }
    public int? LeaderboardRank { get; set; }
    public int? LeagueUserCount { get; set; }
    public List<LeagueLeaderboardEntryResponse> Leaderboard { get; set; } = [];
    public LeaguePromotionResponse? PendingPromotion { get; set; }
}

public class LeaguePromotionResponse
{
    public int FromRank { get; set; }
    public string FromLeagueKey { get; set; } = string.Empty;
    public string FromLeagueName { get; set; } = string.Empty;
    public int ToRank { get; set; }
    public string ToLeagueKey { get; set; } = string.Empty;
    public string ToLeagueName { get; set; } = string.Empty;
}
