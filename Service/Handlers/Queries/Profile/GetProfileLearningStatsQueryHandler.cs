using MediatR;
using Microsoft.EntityFrameworkCore;
using Postgres;
using Postgres.Abstractions;
using Postgres.Models;
using Service.DataTransferObjects.Requests.Profile;
using Service.DataTransferObjects.Responses.Profile;
using Service.Helpers;

namespace Service.Handlers.Queries.Profile;

public class GetProfileLearningStatsQueryHandler
    : IRequestHandler<GetProfileLearningStatsRequest, GetProfileLearningStatsResponse>
{
    private const int ArticleLearningGoal = 5;

    private readonly LingualLoopContext _context;

    public GetProfileLearningStatsQueryHandler(
        ILingualLoopGenericRepository<UserKartyLearning> userKartyLearningRepository)
    {
        _context = userKartyLearningRepository.GetDbContext();
    }

    public async Task<GetProfileLearningStatsResponse> Handle(
        GetProfileLearningStatsRequest request,
        CancellationToken cancellationToken)
    {
        await KartyLearningSchemaHelper.EnsureCreatedAsync(_context, cancellationToken);
        await UserKartyHistorySchemaHelper.EnsureCreatedAsync(_context, cancellationToken);
        await UserDailyActivitySchemaHelper.EnsureCreatedAsync(_context, cancellationToken);

        var learnings = _context.UserKartyLearnings
            .Where(learning => learning.UserId == request.UserId);

        var pendingReviews = _context.UserKartyHistories
            .Where(history => history.UserId == request.UserId && history.ReviewedDate == null);

        var streak = await _context.UserStreaks
            .AsNoTracking()
            .FirstOrDefaultAsync(
                userStreak => userStreak.UserId == request.UserId,
                cancellationToken);

        return new GetProfileLearningStatsResponse
        {
            LearnedWordCount = await learnings
                .CountAsync(learning => learning.CorrectCount > 0, cancellationToken),
            LearnedArticleCount = await learnings
                .CountAsync(learning => learning.ArticleCorrectCount >= ArticleLearningGoal, cancellationToken),
            ArticleInProgressCount = await learnings
                .CountAsync(
                    learning => learning.ArticleCorrectCount > 0 &&
                                learning.ArticleCorrectCount < ArticleLearningGoal,
                    cancellationToken),
            ReviewPendingCount = await pendingReviews.CountAsync(cancellationToken),
            ReviewMistakeCount = await pendingReviews
                .SumAsync(history => (int?)history.WrongCount, cancellationToken) ?? 0,
            TotalCorrectAnswers = await learnings
                .SumAsync(learning => (int?)learning.CorrectCount, cancellationToken) ?? 0,
            TotalArticleCorrectAnswers = await learnings
                .SumAsync(learning => (int?)learning.ArticleCorrectCount, cancellationToken) ?? 0,
            CurrentStreak = streak?.CurrentStreak ?? 0,
            LongestStreak = streak?.LongestStreak ?? 0,
            FreezeCount = streak?.FreezeCount ?? 0,
            Week = await StreakWeekBuilder.BuildAsync(
                _context,
                request.UserId,
                StreakRules.GetIstanbulDate(DateTime.UtcNow),
                streak,
                cancellationToken),
        };
    }
}
