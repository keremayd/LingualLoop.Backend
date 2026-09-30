using MediatR;
using Microsoft.EntityFrameworkCore;
using Postgres;
using Postgres.Abstractions;
using Postgres.Models;
using Service.DataTransferObjects.Requests.ArticlePractice;
using Service.DataTransferObjects.Responses.ArticlePractice;
using Service.Helpers;

namespace Service.Handlers.Queries.ArticlePractice;

public class GetArticlePracticeTaskQueryHandler
    : IRequestHandler<GetArticlePracticeTaskRequest, GetArticlePracticeTaskResponse>
{
    private const int RequiredWordCount = 2;
    private const int LearningGoal = 5;
    private readonly LingualLoopContext _context;

    public GetArticlePracticeTaskQueryHandler(
        ILingualLoopGenericRepository<UserKartyLearning> learningRepository)
    {
        _context = learningRepository.GetDbContext();
    }

    public async Task<GetArticlePracticeTaskResponse> Handle(
        GetArticlePracticeTaskRequest request,
        CancellationToken cancellationToken)
    {
        await KartyLearningSchemaHelper.EnsureCreatedAsync(_context, cancellationToken);

        // `CorrectCount > 0` şart. Satırın varlığı artık "öğrendi" demiyor:
        // tanışma adımı da satır oluşturuyor (`CorrectCount = 0`). Filtre
        // olmasaydı kullanıcı bir kelimeyi **yalnızca görmüş** olduğu için
        // Artikel Pusulası'nda o kelimeden sorulmaya başlanırdı.
        var learnedWords = await _context.UserKartyLearnings
            .Where(item => item.UserId == request.UserId && item.CorrectCount > 0)
            .Include(item => item.Karty)
            .ToListAsync(cancellationToken);
        if (learnedWords.Count < RequiredWordCount)
        {
            return new GetArticlePracticeTaskResponse
            {
                IsUnlocked = false,
                LearnedWordCount = learnedWords.Count,
                RequiredWordCount = RequiredWordCount,
            };
        }

        var activeWords = learnedWords
            .Where(item => item.ArticleCorrectCount < LearningGoal)
            .ToList();
        if (activeWords.Count == 0)
        {
            return new GetArticlePracticeTaskResponse
            {
                IsUnlocked = true,
                LearnedWordCount = learnedWords.Count,
                RequiredWordCount = RequiredWordCount,
            };
        }

        var lastServed = activeWords
            .Where(item => item.LastArticleServedDate.HasValue)
            .OrderByDescending(item => item.LastArticleServedDate)
            .FirstOrDefault();
        var alternatives = lastServed is null
            ? activeWords
            : activeWords.Where(item => item.KartyId != lastServed.KartyId).ToList();
        if (alternatives.Count == 0)
        {
            alternatives = activeWords;
        }

        var selected = request.PreferredKartyId.HasValue
            ? alternatives.FirstOrDefault(item => item.KartyId == request.PreferredKartyId.Value)
            : null;
        selected ??= SelectNext(alternatives);
        selected.ConsecutiveServeCount = 1;

        var previewCandidates = activeWords
            .Where(item => item.KartyId != selected.KartyId)
            .ToList();
        var preview = previewCandidates.Count == 0
            ? null
            : SelectNext(previewCandidates);

        selected.LastArticleServedDate = DateTime.UtcNow;
        await _context.SaveChangesAsync(cancellationToken);

        return new GetArticlePracticeTaskResponse
        {
            IsUnlocked = true,
            LearnedWordCount = learnedWords.Count,
            RequiredWordCount = RequiredWordCount,
            Task = ToTask(selected),
            NextTask = preview is null ? null : ToTask(preview),
        };
    }

    private static UserKartyLearning SelectNext(IEnumerable<UserKartyLearning> candidates)
    {
        return candidates
            .OrderBy(item => Math.Min(item.ArticleCorrectCount, LearningGoal))
            .ThenByDescending(item => item.ArticleAttemptCount - item.ArticleCorrectCount)
            .ThenBy(_ => Random.Shared.NextDouble())
            .First();
    }

    private static ArticlePracticeTaskResponse ToTask(UserKartyLearning learning)
    {
        return new ArticlePracticeTaskResponse
        {
            KartyId = learning.KartyId,
            NounText = learning.Karty!.NounText,
            KartyUrl = learning.Karty.KartyUrl,
            ArticleAttemptCount = learning.ArticleAttemptCount,
            ArticleCorrectCount = learning.ArticleCorrectCount,
            LearningStack = Math.Min(learning.ArticleCorrectCount, LearningGoal),
            LearningGoal = LearningGoal,
        };
    }
}
