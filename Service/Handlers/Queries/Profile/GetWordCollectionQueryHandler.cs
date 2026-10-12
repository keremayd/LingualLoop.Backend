using MediatR;
using Microsoft.EntityFrameworkCore;
using Postgres;
using Postgres.Abstractions;
using Postgres.Models;
using Service.DataTransferObjects.Requests.Profile;
using Service.DataTransferObjects.Responses.Profile;
using Service.Helpers;

namespace Service.Handlers.Queries.Profile;

public class GetWordCollectionQueryHandler
    : IRequestHandler<GetWordCollectionRequest, GetWordCollectionResponse>
{
    private const int PageSize = 20;
    private const int ArticleLearningGoal = 5;
    private readonly LingualLoopContext _context;

    public GetWordCollectionQueryHandler(
        ILingualLoopGenericRepository<UserKartyLearning> learningRepository)
    {
        _context = learningRepository.GetDbContext();
    }

    public async Task<GetWordCollectionResponse> Handle(
        GetWordCollectionRequest request,
        CancellationToken cancellationToken)
    {
        await KartyLearningSchemaHelper.EnsureCreatedAsync(_context, cancellationToken);
        await KartyAudioSchemaHelper.EnsureCreatedAsync(_context, cancellationToken);

        var utcNow = DateTime.UtcNow;
        var score = await _context.UserScores.AsNoTracking()
            .Where(item => item.UserId == request.UserId)
            .Select(item => (int?)item.Score)
            .FirstOrDefaultAsync(cancellationToken);

        // Older rows can have a correct answer without an introduction date.
        // Both kinds represent a word the user has actually encountered.
        var words = _context.UserKartyLearnings.AsNoTracking()
            .Where(item => item.UserId == request.UserId &&
                           (item.IntroducedDate != null || item.CorrectCount > 0) &&
                           item.Karty != null);

        var dueWords = words.Where(item =>
            score != null && item.Box > 0 && item.DueDate <= utcNow &&
            item.Karty!.MinScore <= score && item.Karty.MaxScore >= score);
        var articleWords = words.Where(item =>
            item.CorrectCount > 0 && item.ArticleCorrectCount < ArticleLearningGoal);

        var response = new GetWordCollectionResponse
        {
            TotalCount = await words.CountAsync(cancellationToken),
            NewCount = await words.CountAsync(item => item.CorrectCount == 0,
                cancellationToken),
            DueCount = await dueWords.CountAsync(cancellationToken),
            ArticleCount = await articleWords.CountAsync(cancellationToken),
            Page = Math.Clamp(request.Page, 1, int.MaxValue / PageSize),
        };

        var filtered = (request.Category ?? "all").Trim().ToLowerInvariant() switch
        {
            "new" => words.Where(item => item.CorrectCount == 0),
            "due" => dueWords,
            "article" => articleWords,
            _ => words,
        };
        response.FilteredCount = await filtered.CountAsync(cancellationToken);

        // Only the current page gets signed media URLs in the controller.
        // The collection may grow much larger than the current 15-card deck.
        response.Items = await filtered
            .OrderByDescending(item => item.IntroducedDate ?? item.FirstLearnedDate)
            .ThenByDescending(item => item.KartyId)
            .Skip((response.Page - 1) * PageSize)
            .Take(PageSize)
            .Select(item => new WordCollectionItemResponse
            {
                KartyId = item.KartyId,
                NounText = item.Karty!.NounText,
                Article = item.Karty.Article,
                KartyUrl = item.Karty.KartyUrl,
                AudioUrl = item.Karty.AudioUrl,
                IsNew = item.CorrectCount == 0,
                IsDue = score != null && item.Box > 0 && item.DueDate <= utcNow &&
                        item.Karty.MinScore <= score && item.Karty.MaxScore >= score,
                NeedsArticlePractice = item.CorrectCount > 0 &&
                                       item.ArticleCorrectCount < ArticleLearningGoal,
            })
            .ToListAsync(cancellationToken);
        response.HasMore = response.Page * PageSize < response.FilteredCount;
        return response;
    }
}
