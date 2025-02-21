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
using Service.DataTransferObjects.Responses;
using Service.DataTransferObjects.Responses.Karty;

namespace Service.Handlers.Queries;

public class GetKartyByScoreQueryHandler : IRequestHandler<GetKartyByScoreRequest, GetKartyByScoreResponse>
{
    private readonly ILingualLoopGenericRepository<Karty> _kartyRepository;
    private readonly ILingualLoopGenericRepository<UserKartyHistory> _ukhRepository;
    private readonly LingualLoopContext _context;

    public GetKartyByScoreQueryHandler(ILingualLoopGenericRepository<Karty> kartyRepository, ILingualLoopGenericRepository<UserKartyHistory> ukhRepository)
    {
        _kartyRepository = kartyRepository;
        _ukhRepository = ukhRepository;
        _context = kartyRepository.GetDbContext();
    }

    public async Task<GetKartyByScoreResponse> Handle(GetKartyByScoreRequest request, CancellationToken cancellationToken)
    {
        var watchedVideoList = await _ukhRepository.GetListAsync(f => f.UserId.Equals(request.UserId), 
            uvh => new UserKartyHistory(){ KartyId = uvh.KartyId });
        var watchedVideoIds = watchedVideoList.Select(w => w.KartyId).ToList();
        
        var kartyQuestion = await _context.Karty
            .Where(q => (q.MinScore <= request.UserScore && q.MaxScore >= request.UserScore) && !watchedVideoIds.Contains(q.KartyId))
            .OrderBy(q => EF.Functions.Random())
            .Select(q => new GetKartyByScoreResponse()
            {
                KartyId = q.KartyId,
                QuestionText = q.QuestionText,
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