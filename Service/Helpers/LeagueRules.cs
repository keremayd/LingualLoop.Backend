using Service.DataTransferObjects.Responses;

namespace Service.Helpers;

public static class LeagueRules
{
    private static readonly LeagueDefinition[] Leagues =
    [
        new("merkur", "Merkür", 1, 0, 10),
        new("aytasi", "Aytaşı", 2, 10, 20),
        new("yildiz", "Yıldız", 3, 20, 30),
        new("kuyruklu", "Kuyruklu", 4, 30, 40),
        new("mars", "Mars", 5, 40, 50),
        new("uranus", "Uranüs", 6, 50, 60),
        new("saturn", "Satürn", 7, 60, 70),
        new("nebula", "Nebula", 8, 70, 80),
        new("kosmoz", "Kosmoz", 9, 80, 90),
        new("supernova", "Supernova", 10, 90, 120),
        new("pulsar", "Pulsar", 11, 120, null),
    ];

    public static int GetCurrentSeasonKey(DateTime utcNow)
    {
        return utcNow.Year * 100 + utcNow.Month;
    }

    public static DateTime GetSeasonStartUtc(int seasonKey)
    {
        var year = seasonKey / 100;
        var month = seasonKey % 100;
        return new DateTime(year, month, 1, 0, 0, 0, DateTimeKind.Utc);
    }

    public static DateTime GetSeasonEndUtc(int seasonKey)
    {
        return GetSeasonStartUtc(seasonKey).AddMonths(1);
    }

    public static LeagueProgressResponse BuildProgress(int points, int seasonKey)
    {
        var normalizedPoints = Math.Max(0, points);
        var league = Leagues.Last(l =>
            normalizedPoints >= l.MinPoints &&
            (!l.MaxPoints.HasValue || normalizedPoints < l.MaxPoints.Value));

        var progressRatio = 1.0;
        int? pointsToNextLeague = null;

        if (league.MaxPoints.HasValue)
        {
            var bandWidth = league.MaxPoints.Value - league.MinPoints;
            pointsToNextLeague = Math.Max(0, league.MaxPoints.Value - normalizedPoints);
            progressRatio = Math.Clamp(
                (double)(normalizedPoints - league.MinPoints) / bandWidth,
                0,
                1);
        }

        return new LeagueProgressResponse
        {
            LeagueKey = league.Key,
            LeagueName = league.Name,
            Rank = league.Rank,
            Points = normalizedPoints,
            MinPoints = league.MinPoints,
            MaxPoints = league.MaxPoints,
            PointsToNextLeague = pointsToNextLeague,
            ProgressRatio = progressRatio,
            SeasonKey = seasonKey,
            SeasonStartsAtUtc = GetSeasonStartUtc(seasonKey),
            SeasonEndsAtUtc = GetSeasonEndUtc(seasonKey),
            LeaderboardRank = null,
            LeagueUserCount = null,
        };
    }

    public static int GetLeagueRank(int points)
    {
        var normalizedPoints = Math.Max(0, points);
        return Leagues.Last(l =>
            normalizedPoints >= l.MinPoints &&
            (!l.MaxPoints.HasValue || normalizedPoints < l.MaxPoints.Value)).Rank;
    }

    public static LeaguePromotionResponse? BuildPendingPromotion(
        int announcedRank,
        int currentRank)
    {
        var normalizedAnnouncedRank = Math.Clamp(announcedRank, 1, Leagues.Length);
        var normalizedCurrentRank = Math.Clamp(currentRank, 1, Leagues.Length);
        if (normalizedCurrentRank <= normalizedAnnouncedRank) return null;

        var from = Leagues.Single(l => l.Rank == normalizedAnnouncedRank);
        var to = Leagues.Single(l => l.Rank == normalizedCurrentRank);

        return new LeaguePromotionResponse
        {
            FromRank = from.Rank,
            FromLeagueKey = from.Key,
            FromLeagueName = from.Name,
            ToRank = to.Rank,
            ToLeagueKey = to.Key,
            ToLeagueName = to.Name,
        };
    }

    /// <summary>
    /// Yeni sezonun başlangıç puanını hesaplar: "bir kademe alttan başla"
    /// kuralı. Kullanıcı önceki sezonu bitirdiği ligin, kaçırdığı her sezon
    /// için bir alt kademesinin tabanından başlar. Örn. Pulsar'da bitiren
    /// bir sonraki sezona Supernova tabanı (90) ile başlar; 3 sezon hiç
    /// oynamayan 3 kademe düşer. Taban Merkür'dür (0 puan).
    /// </summary>
    public static int GetSeasonStartPoints(int previousFinalPoints, int seasonsElapsed)
    {
        var normalizedPoints = Math.Max(0, previousFinalPoints);
        var previousLeague = Leagues.Last(l =>
            normalizedPoints >= l.MinPoints &&
            (!l.MaxPoints.HasValue || normalizedPoints < l.MaxPoints.Value));

        var demotionSteps = Math.Max(1, seasonsElapsed);
        var newRank = Math.Max(1, previousLeague.Rank - demotionSteps);

        return Leagues.Single(l => l.Rank == newRank).MinPoints;
    }

    /// <summary>İki sezon anahtarı arasındaki ay farkı (seasonKey = yyyyMM).</summary>
    public static int GetSeasonsBetween(int fromSeasonKey, int toSeasonKey)
    {
        var fromMonths = (fromSeasonKey / 100) * 12 + (fromSeasonKey % 100);
        var toMonths = (toSeasonKey / 100) * 12 + (toSeasonKey % 100);
        return Math.Max(0, toMonths - fromMonths);
    }

    private sealed record LeagueDefinition(
        string Key,
        string Name,
        int Rank,
        int MinPoints,
        int? MaxPoints);
}
