using MediatR;
using Microsoft.EntityFrameworkCore;
using Postgres;
using Postgres.Abstractions;
using Postgres.Models;
using Service.DataTransferObjects.Requests.ArticlePractice;
using Service.DataTransferObjects.Responses.ArticlePractice;
using Service.Helpers;

namespace Service.Handlers.Commands.ArticlePractice;

public class AnswerArticlePracticeCommandHandler
    : IRequestHandler<AnswerArticlePracticeRequest, AnswerArticlePracticeResponse>
{
    private const int CorrectAnswerPoints = 1;
    private readonly LingualLoopContext _context;

    public AnswerArticlePracticeCommandHandler(
        ILingualLoopGenericRepository<UserKartyLearning> learningRepository)
    {
        _context = learningRepository.GetDbContext();
    }

    public async Task<AnswerArticlePracticeResponse> Handle(
        AnswerArticlePracticeRequest request,
        CancellationToken cancellationToken)
    {
        await KartyLearningSchemaHelper.EnsureCreatedAsync(_context, cancellationToken);

        var learning = await _context.UserKartyLearnings
            .Include(item => item.Karty)
            .FirstOrDefaultAsync(
                item => item.UserId == request.UserId && item.KartyId == request.KartyId,
                cancellationToken);
        var lastServed = await _context.UserKartyLearnings
            .Where(item => item.UserId == request.UserId && item.LastArticleServedDate.HasValue)
            .OrderByDescending(item => item.LastArticleServedDate)
            .FirstOrDefaultAsync(cancellationToken);
        if (learning?.Karty is null || lastServed?.KartyId != request.KartyId)
        {
            return new AnswerArticlePracticeResponse { IsAccepted = false };
        }

        if (learning.LastArticleAnsweredDate.HasValue &&
            learning.LastArticleServedDate.HasValue &&
            learning.LastArticleAnsweredDate >= learning.LastArticleServedDate)
        {
            return new AnswerArticlePracticeResponse { IsAccepted = false };
        }

        var selectedArticle = request.SelectedArticle.Trim().ToLowerInvariant();
        var correctArticle = learning.Karty.Article.Trim().ToLowerInvariant();
        var isCorrect = selectedArticle == correctArticle;
        learning.ArticleAttemptCount += 1;

        if (isCorrect)
        {
            learning.ArticleCorrectCount += 1;
            learning.LastArticleAnsweredDate = DateTime.UtcNow;
            var userScore = await _context.UserScores
                .FirstOrDefaultAsync(item => item.UserId == request.UserId, cancellationToken);
            if (userScore is not null) userScore.Score += CorrectAnswerPoints;
        }

        // Buraya yalnızca **kabul edilen** bir cevap ulaşıyor (geçersiz ve
        // mükerrer cevaplar yukarıda erken dönüyor). Doğru olması şart değil:
        // Artikel Pusulası'nda cevap vermek de günü "oynandı" yapar.
        await StreakActivityRecorder.MarkPlayedAsync(
            _context, request.UserId, DateTime.UtcNow, cancellationToken);

        await _context.SaveChangesAsync(cancellationToken);
        return new AnswerArticlePracticeResponse
        {
            IsAccepted = true,
            IsCorrect = isCorrect,
            CorrectArticle = correctArticle,
            EarnedPoints = isCorrect ? CorrectAnswerPoints : 0,
            ArticleAttemptCount = learning.ArticleAttemptCount,
            ArticleCorrectCount = learning.ArticleCorrectCount,
        };
    }
}
