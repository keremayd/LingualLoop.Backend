using System.Net;
using Common.Enums;
using Common.Exceptions;
using Common.Extensions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Postgres;
using Postgres.Abstractions;
using Service.DataTransferObjects.Requests.Karty;
using Service.DataTransferObjects.Responses.Karty;
using KartyModel = Postgres.Models.Karty;

namespace Service.Handlers.Queries;

public class GetKartyByScoreQueryHandler : IRequestHandler<GetKartyByScoreRequest, GetKartyByScoreResponse>
{
    private readonly LingualLoopContext _context;

    public GetKartyByScoreQueryHandler(ILingualLoopGenericRepository<KartyModel> kartyRepository, ILingualLoopGenericRepository<Postgres.Models.UserKartyHistory> ukhRepository)
    {
        _context = kartyRepository.GetDbContext();
    }

    public async Task<GetKartyByScoreResponse> Handle(GetKartyByScoreRequest request, CancellationToken cancellationToken)
    {
        var kartyQuestion = await _context.Karty
            .Where(q => q.MinScore <= request.UserScore && q.MaxScore >= request.UserScore)
            .OrderBy(q => EF.Functions.Random())
            .Select(q => new GetKartyByScoreResponse()
            {
                KartyId = q.KartyId,
                QuestionText = q.QuestionText,
                CorrectText = q.CorrectText,
                KartyUrl = q.KartyUrl,
                MinScore = q.MinScore,
                MaxScore = q.MaxScore,
                IsCorrect = q.IsCorrect
            })
            .FirstOrDefaultAsync(cancellationToken);
        
        if (kartyQuestion is null)
        {
            throw new LingualLoopException(ErrorCode.NoDataFoundInKarty.CreateMessage(request.UserScore),
                ErrorCode.NoDataFoundInKarty.GetDescription(request.UserScore), HttpStatusCode.BadRequest);
        }

        return kartyQuestion;
    }
}
