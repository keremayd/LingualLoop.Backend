using AwsService.Abstractions;
using Common.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Postgres;
using Postgres.Abstractions;
using Postgres.Models;
using Service.DataTransferObjects.Requests;
using Service.DataTransferObjects.Responses;
using Service.Helpers;

namespace Service.Handlers.Queries;

public class GetLeagueByUserIdQueryHandler : IRequestHandler<GetLeagueByUserIdRequest, LeagueProgressResponse>
{
    private readonly LingualLoopContext _context;
    private readonly IAwsService _awsService;

    public GetLeagueByUserIdQueryHandler(
        ILingualLoopGenericRepository<User> userRepository,
        IAwsService awsService)
    {
        _context = userRepository.GetDbContext();
        _awsService = awsService;
    }

    private string? _buildPhotoUrl(string? profilePhotoKey)
    {
        if (string.IsNullOrWhiteSpace(profilePhotoKey)) return null;

        return _awsService.GeneratePreSignedUrl(
            profilePhotoKey,
            BucketType.ProfilePhotos);
    }

    public async Task<LeagueProgressResponse> Handle(GetLeagueByUserIdRequest request, CancellationToken cancellationToken)
    {
        await UserLeagueProgressSchemaHelper.EnsureCreatedAsync(_context, cancellationToken);

        var utcNow = DateTime.UtcNow;
        var seasonKey = LeagueRules.GetCurrentSeasonKey(utcNow);

        var progress = await LeagueProgressSeeder.GetOrCreateAsync(
            _context,
            request.UserId,
            seasonKey,
            utcNow,
            cancellationToken);

        var leagueProgress = LeagueRules.BuildProgress(progress.Points, seasonKey);
        leagueProgress.PendingPromotion = LeagueRules.BuildPendingPromotion(
            progress.AnnouncedLeagueRank,
            leagueProgress.Rank);
        var leagueQuery = _context.UserLeagueProgresses
            .AsNoTracking()
            .Where(p =>
                p.SeasonKey == seasonKey &&
                p.Points >= leagueProgress.MinPoints);

        if (leagueProgress.MaxPoints.HasValue)
        {
            leagueQuery = leagueQuery.Where(p => p.Points < leagueProgress.MaxPoints.Value);
        }

        var leagueUserCount = await leagueQuery.CountAsync(cancellationToken);
        leagueProgress.LeagueUserCount = Math.Max(1, leagueUserCount);
        leagueProgress.LeaderboardRank = await leagueQuery
            .CountAsync(p => p.Points > leagueProgress.Points, cancellationToken) + 1;

        leagueProgress.Leaderboard = await BuildLeaderboardAsync(
            leagueQuery,
            request.UserId,
            leagueProgress,
            cancellationToken);

        return leagueProgress;
    }

    private async Task<List<LeagueLeaderboardEntryResponse>> BuildLeaderboardAsync(
        IQueryable<UserLeagueProgress> leagueQuery,
        string userId,
        LeagueProgressResponse leagueProgress,
        CancellationToken cancellationToken)
    {
        const int maxEntries = 30;

        var rows = await leagueQuery
            .OrderByDescending(p => p.Points)
            .ThenBy(p => p.UserId)
            .Take(maxEntries)
            .Select(p => new { p.UserId, p.Points })
            .ToListAsync(cancellationToken);

        var userIds = rows.Select(r => r.UserId).ToList();
        var names = await _context.Users
            .AsNoTracking()
            .Where(u => userIds.Contains(u.Id))
            .Select(u => new { u.Id, u.FirstName, u.LastName, u.ProfilePhoto })
            .ToDictionaryAsync(u => u.Id, cancellationToken);

        var entries = rows
            .Select((row, index) =>
            {
                names.TryGetValue(row.UserId, out var name);
                var displayName = $"{name?.FirstName} {name?.LastName}".Trim();

                return new LeagueLeaderboardEntryResponse
                {
                    Rank = index + 1,
                    UserId = row.UserId,
                    DisplayName = string.IsNullOrWhiteSpace(displayName)
                        ? "Gökyüzü Gezgini"
                        : displayName,
                    Points = row.Points,
                    IsCurrentUser = row.UserId == userId,
                    ProfilePhotoUrl = _buildPhotoUrl(name?.ProfilePhoto),
                };
            })
            .ToList();

        if (!entries.Any(e => e.IsCurrentUser))
        {
            var currentUser = await _context.Users
                .AsNoTracking()
                .Where(u => u.Id == userId)
                .Select(u => new { u.FirstName, u.LastName, u.ProfilePhoto })
                .FirstOrDefaultAsync(cancellationToken);
            var currentDisplayName =
                $"{currentUser?.FirstName} {currentUser?.LastName}".Trim();

            entries.Add(new LeagueLeaderboardEntryResponse
            {
                Rank = leagueProgress.LeaderboardRank ?? entries.Count + 1,
                UserId = userId,
                DisplayName = string.IsNullOrWhiteSpace(currentDisplayName)
                    ? "Gökyüzü Gezgini"
                    : currentDisplayName,
                Points = leagueProgress.Points,
                IsCurrentUser = true,
                ProfilePhotoUrl = _buildPhotoUrl(currentUser?.ProfilePhoto),
            });
        }

        return entries;
    }
}
