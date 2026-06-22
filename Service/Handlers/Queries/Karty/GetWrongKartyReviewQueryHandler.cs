using System.Net;
using Common.Enums;
using Common.Exceptions;
using Common.Extensions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Postgres;
using Postgres.Abstractions;
using Postgres.Models;
using Service.DataTransferObjects.Requests.Karty;
using Service.DataTransferObjects.Responses.Karty;
using Service.Helpers;

namespace Service.Handlers.Queries.Karty;

public class GetWrongKartyReviewQueryHandler : IRequestHandler<GetWrongKartyReviewRequest, GetKartyByScoreResponse>
{
    private readonly LingualLoopContext _context;

    public GetWrongKartyReviewQueryHandler(ILingualLoopGenericRepository<UserKartyHistory> userKartyHistoryRepository)
    {
        _context = userKartyHistoryRepository.GetDbContext();
    }

    public async Task<GetKartyByScoreResponse> Handle(GetWrongKartyReviewRequest request, CancellationToken cancellationToken)
    {
        await UserKartyHistorySchemaHelper.EnsureCreatedAsync(_context, cancellationToken);

        var kartyQuestion = await _context.UserKartyHistories
            .Where(history => history.UserId == request.UserId && history.ReviewedDate == null)
            .OrderByDescending(history => history.WrongCount)
            .ThenBy(history => EF.Functions.Random())
            .Select(history => new GetKartyByScoreResponse
            {
                KartyId = history.Karty!.KartyId,
                QuestionText = history.Karty.QuestionText,
                CorrectText = history.Karty.CorrectText,
                KartyUrl = history.Karty.KartyUrl,
                MinScore = history.Karty.MinScore,
                MaxScore = history.Karty.MaxScore,
                IsCorrect = history.Karty.IsCorrect,
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (kartyQuestion is null)
        {
            throw new LingualLoopException(ErrorCode.NoDataFoundInKarty.CreateMessage(0),
                ErrorCode.NoDataFoundInKarty.GetDescription(0), HttpStatusCode.BadRequest);
        }

        return kartyQuestion;
    }
}
