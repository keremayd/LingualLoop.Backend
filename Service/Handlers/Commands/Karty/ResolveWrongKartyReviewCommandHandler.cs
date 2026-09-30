using System.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Postgres;
using Postgres.Abstractions;
using Postgres.Models;
using Service.DataTransferObjects.Requests.Karty;
using Service.DataTransferObjects.Responses.Karty;
using Service.Helpers;

namespace Service.Handlers.Commands.Karty;

public class ResolveWrongKartyReviewCommandHandler : IRequestHandler<ResolveWrongKartyReviewRequest, ResolveWrongKartyReviewResponse>
{
    private readonly LingualLoopContext _context;

    public ResolveWrongKartyReviewCommandHandler(ILingualLoopGenericRepository<UserKartyHistory> userKartyHistoryRepository)
    {
        _context = userKartyHistoryRepository.GetDbContext();
    }

    public async Task<ResolveWrongKartyReviewResponse> Handle(ResolveWrongKartyReviewRequest request, CancellationToken cancellationToken)
    {
        await UserKartyHistorySchemaHelper.EnsureCreatedAsync(_context, cancellationToken);
        await KartyLearningSchemaHelper.EnsureCreatedAsync(_context, cancellationToken);

        // Aynı kart için paralel iki doğru cevap ikinci bir bilet üretmesin.
        // Satır kilidi, ikinci isteğin ilk commit'i görmesini sağlar.
        await using var transaction = await _context.Database.BeginTransactionAsync(
            IsolationLevel.ReadCommitted,
            cancellationToken);

        var history = await _context.UserKartyHistories
            .FromSqlInterpolated($$"""
                SELECT *
                FROM user_karty_history
                WHERE user_id = {{request.UserId}}
                  AND karty_id = {{request.KartyId}}
                FOR UPDATE
                """)
            .FirstOrDefaultAsync(
                cancellationToken);

        // Ödül yalnız gerçekten bekleyen bir kartın pending -> reviewed
        // geçişinde doğar. Tekrarlanan istek bu şartı sağlayamaz.
        var wasPending = history is not null && history.ReviewedDate is null;

        if (history is null)
        {
            history = new UserKartyHistory
            {
                UserId = request.UserId,
                KartyId = request.KartyId,
                WrongCount = request.IsMastered ? 0 : 1,
                LastWrongDate = DateTime.UtcNow,
                ReviewedDate = request.IsMastered ? DateTime.UtcNow : null,
            };

            _context.UserKartyHistories.Add(history);
        }
        else if (request.IsMastered)
        {
            history.ReviewedDate = DateTime.UtcNow;
        }
        else
        {
            history.WrongCount += 1;
            history.LastWrongDate = DateTime.UtcNow;
            history.ReviewedDate = null;
        }

        if (request.IsMastered)
        {
            await KartyLearningRecorder.RecordAsync(
                _context,
                request.UserId,
                request.KartyId,
                cancellationToken);
        }

        // Rövanş da öğrenmedir: verilen cevap günü "oynandı" yapar.
        await StreakActivityRecorder.MarkPlayedAsync(
            _context, request.UserId, DateTime.UtcNow, cancellationToken);

        var rewardTickets = 0;
        if (request.IsMastered && wasPending)
        {
            // Mevcut kart henüz SaveChanges edilmediği için sorgudan açıkça
            // çıkarılır. Başka bekleyen kart yoksa Rövanş oturumu bitmiştir.
            var hasOtherPendingReview = await _context.UserKartyHistories
                .AsNoTracking()
                .AnyAsync(
                    item => item.UserId == request.UserId &&
                            item.KartyId != request.KartyId &&
                            item.ReviewedDate == null,
                    cancellationToken);

            if (!hasOtherPendingReview)
            {
                var utcNow = DateTime.UtcNow;
                var userLives = await UserLivesSeeder.GetOrCreateAsync(
                    _context,
                    request.UserId,
                    utcNow,
                    cancellationToken);

                var livesBeforeReward = userLives.Lives;
                userLives.Lives = Math.Min(userLives.Lives + 1, userLives.MaxLives);
                rewardTickets = userLives.Lives - livesBeforeReward;
            }
        }

        await _context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return new ResolveWrongKartyReviewResponse
        {
            UserId = history.UserId,
            KartyId = history.KartyId,
            IsMastered = request.IsMastered,
            WrongCount = history.WrongCount,
            ReviewedDate = history.ReviewedDate,
            RewardTickets = rewardTickets,
        };
    }
}
