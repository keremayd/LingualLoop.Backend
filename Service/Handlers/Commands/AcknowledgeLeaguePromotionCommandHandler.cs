using MediatR;
using Postgres;
using Postgres.Abstractions;
using Postgres.Models;
using Service.DataTransferObjects.Requests;
using Service.DataTransferObjects.Responses;
using Service.Helpers;

namespace Service.Handlers.Commands;

public class AcknowledgeLeaguePromotionCommandHandler
    : IRequestHandler<AcknowledgeLeaguePromotionRequest, AcknowledgeLeaguePromotionResponse>
{
    private readonly LingualLoopContext _context;

    public AcknowledgeLeaguePromotionCommandHandler(
        ILingualLoopGenericRepository<UserLeagueProgress> repository)
    {
        _context = repository.GetDbContext();
    }

    public async Task<AcknowledgeLeaguePromotionResponse> Handle(
        AcknowledgeLeaguePromotionRequest request,
        CancellationToken cancellationToken)
    {
        await UserLeagueProgressSchemaHelper.EnsureCreatedAsync(
            _context,
            cancellationToken);

        var utcNow = DateTime.UtcNow;
        var seasonKey = LeagueRules.GetCurrentSeasonKey(utcNow);
        var progress = await LeagueProgressSeeder.GetOrCreateAsync(
            _context,
            request.UserId,
            seasonKey,
            utcNow,
            cancellationToken);

        var currentRank = LeagueRules.GetLeagueRank(progress.Points);
        if (currentRank > progress.AnnouncedLeagueRank)
        {
            progress.AnnouncedLeagueRank = currentRank;
            progress.UpdatedAt = utcNow;
            await _context.SaveChangesAsync(cancellationToken);
        }

        return new AcknowledgeLeaguePromotionResponse
        {
            AcknowledgedRank = progress.AnnouncedLeagueRank,
        };
    }
}
