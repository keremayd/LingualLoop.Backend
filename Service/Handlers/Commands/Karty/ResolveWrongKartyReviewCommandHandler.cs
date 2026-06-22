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

        var history = await _context.UserKartyHistories
            .FirstOrDefaultAsync(
                item => item.UserId == request.UserId && item.KartyId == request.KartyId,
                cancellationToken);

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

        await _context.SaveChangesAsync(cancellationToken);

        return new ResolveWrongKartyReviewResponse
        {
            UserId = history.UserId,
            KartyId = history.KartyId,
            IsMastered = request.IsMastered,
            WrongCount = history.WrongCount,
            ReviewedDate = history.ReviewedDate,
        };
    }
}
