using MediatR;
using Microsoft.EntityFrameworkCore;
using Postgres;
using Postgres.Abstractions;
using Postgres.Models;
using Service.DataTransferObjects.Requests.Karty;
using Service.DataTransferObjects.Responses.Karty;
using Service.Helpers;

namespace Service.Handlers.Queries.Karty;

/// <summary>
/// Oyun her cevaptan sonra sorar: oyunun destesi bitti mi, tekrar payı doldu
/// mu (<see cref="KartyDeckRules.IsGameComplete"/>). Bitince istemci özeti
/// gösterir. Okuma yolu; veri yazmaz.
///
/// "Bitti" kararı sunucuda, çünkü istemcinin önden çektiği kartlar eskimiş
/// olabilir: son testten önce çekilmiş bir kart hâlâ "test" gibi görünür.
/// </summary>
public class GetKartySessionStatusQueryHandler
    : IRequestHandler<GetKartySessionStatusRequest, GetKartySessionStatusResponse>
{
    private readonly LingualLoopContext _context;

    public GetKartySessionStatusQueryHandler(
        ILingualLoopGenericRepository<UserKartyLearning> learningRepository)
    {
        _context = learningRepository.GetDbContext();
    }

    public async Task<GetKartySessionStatusResponse> Handle(
        GetKartySessionStatusRequest request,
        CancellationToken cancellationToken)
    {
        await KartyLearningSchemaHelper.EnsureCreatedAsync(_context, cancellationToken);

        var now = DateTime.UtcNow;
        var dueReviews = await _context.UserKartyLearnings
            .Where(learning => learning.UserId == request.UserId)
            .Where(KartyLeitner.DueForReview(now))
            .CountAsync(cancellationToken);

        var unit = await _context.KartyUnits
            .AsNoTracking()
            .FirstOrDefaultAsync(item => item.Sequence == request.UnitSequence, cancellationToken);
        var quota = KartyDeckRules.ReviewQuota(deckGame: request.Deck is int);
        if (unit is null)
        {
            return new GetKartySessionStatusResponse
            {
                Complete = KartyDeckRules.IsGameComplete(
                    true, dueReviews, request.ReviewsAnswered, quota),
                DueReviews = dueReviews,
            };
        }

        var unitCards = await _context.Karty
            .AsNoTracking()
            .Where(karty => karty.UnitId == unit.KartyUnitId)
            .OrderBy(karty => karty.UnitOrder ?? int.MaxValue)
            .ThenBy(karty => karty.KartyId)
            .Select(karty => new { karty.KartyId, karty.NounText, karty.Article, karty.KartyUrl })
            .ToListAsync(cancellationToken);
        var ids = unitCards.Select(card => card.KartyId).ToList();
        var learnings = await _context.UserKartyLearnings
            .AsNoTracking()
            .Where(learning => learning.UserId == request.UserId && ids.Contains(learning.KartyId))
            .Select(learning => new { learning.KartyId, learning.Box, learning.DueDate })
            .ToDictionaryAsync(learning => learning.KartyId, cancellationToken);

        var deck = request.Deck is int number && number > 0
            ? unitCards.Skip((number - 1) * KartyDeckRules.Size).Take(KartyDeckRules.Size).ToList()
            : unitCards.Take(0).ToList();
        var tested = deck.Count(card =>
            learnings.TryGetValue(card.KartyId, out var learning) && learning.Box >= 1);
        // Yalnız tekrar oturumunda deste yok; bitiş tekrarlara bağlı.
        var deckComplete = deck.Count == 0 || KartyDeckRules.IsComplete(deck.Count, tested);

        return new GetKartySessionStatusResponse
        {
            Complete = KartyDeckRules.IsGameComplete(
                deckComplete, dueReviews, request.ReviewsAnswered, quota),
            DueReviews = dueReviews,
            DeckSize = deck.Count,
            DeckIntroduced = deck.Count(card => learnings.ContainsKey(card.KartyId)),
            DeckTested = tested,
            Words = deck
                .Select(card =>
                {
                    learnings.TryGetValue(card.KartyId, out var learning);
                    return new KartyPathWordResponse
                    {
                        KartyId = card.KartyId,
                        NounText = card.NounText,
                        Article = card.Article,
                        KartyUrl = card.KartyUrl,
                        Box = learning?.Box ?? 0,
                        IsDue = learning is not null &&
                                KartyLeitner.IsDueForReview(learning.Box, learning.DueDate, now),
                    };
                })
                .ToList(),
            UnitConsolidated = learnings.Values.Count(learning =>
                KartyUnitRules.IsConsolidated(learning.Box)),
            UnitWordCount = unitCards.Count,
        };
    }
}
