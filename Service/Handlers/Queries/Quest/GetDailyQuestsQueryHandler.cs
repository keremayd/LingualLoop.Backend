using MediatR;
using Microsoft.EntityFrameworkCore;
using Postgres;
using Postgres.Abstractions;
using Postgres.Models;
using Service.DataTransferObjects.Requests;
using Service.DataTransferObjects.Responses;
using Service.Helpers;

namespace Service.Handlers.Queries;

public class GetDailyQuestsQueryHandler : IRequestHandler<GetDailyQuestsRequest, GetDailyQuestsResponse>
{
    private readonly LingualLoopContext _context;

    public GetDailyQuestsQueryHandler(ILingualLoopGenericRepository<User> userRepository)
    {
        _context = userRepository.GetDbContext();
    }

    public async Task<GetDailyQuestsResponse> Handle(GetDailyQuestsRequest request, CancellationToken cancellationToken)
    {
        await UserQuestClaimSchemaHelper.EnsureCreatedAsync(_context, cancellationToken);

        var utcNow = DateTime.UtcNow;
        var dayKey = QuestRules.GetDayKey(utcNow);
        var (_, endUtc) = QuestRules.GetDayUtcRange(utcNow);

        var claimedKeys = await _context.UserQuestClaims
            .AsNoTracking()
            .Where(c => c.UserId == request.UserId && c.DayKey == dayKey)
            .Select(c => c.QuestKey)
            .ToListAsync(cancellationToken);

        var quests = new List<DailyQuestResponse>();
        foreach (var definition in QuestRules.DailyQuests)
        {
            var progress = await QuestRules.GetProgressAsync(
                _context, request.UserId, definition.Key, utcNow, cancellationToken);
            var clampedProgress = Math.Min(progress, definition.Target);

            quests.Add(new DailyQuestResponse
            {
                QuestKey = definition.Key,
                Title = definition.Title,
                Target = definition.Target,
                Progress = clampedProgress,
                RewardTickets = definition.RewardTickets,
                IsCompleted = clampedProgress >= definition.Target,
                IsClaimed = claimedKeys.Contains(definition.Key),
            });
        }

        return new GetDailyQuestsResponse
        {
            DayKey = dayKey,
            ResetAtUtc = endUtc,
            Quests = quests,
        };
    }
}
