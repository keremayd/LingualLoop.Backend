using System.Net;
using Common.Enums;
using Common.Exceptions;
using Common.Extensions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Postgres;
using Postgres.Abstractions;
using Postgres.Models;
using Service.DataTransferObjects.Requests;
using Service.DataTransferObjects.Responses;
using Service.Helpers;

namespace Service.Handlers.Queries;

public class GetScoreByIdQueryHandler : IRequestHandler<GetScoreByIdRequest, GetScoreWithLivesByIdResponse>
{
    private readonly ILingualLoopGenericRepository<User> _genericRepository;
    private readonly LingualLoopContext _context;

    public GetScoreByIdQueryHandler(ILingualLoopGenericRepository<User> genericRepository)
    {
        _genericRepository = genericRepository;
        _context = genericRepository.GetDbContext();
    }
    
    public async Task<GetScoreWithLivesByIdResponse> Handle(GetScoreByIdRequest request, CancellationToken cancellationToken)
    {
        await UserScoreSchemaHelper.EnsureCreatedAsync(_context, cancellationToken);

        var user = await _genericRepository.FirstAsync(u => u.Id == request.UserId,
            user => new User() { Id = user.Id, UserScore = user.UserScore});

        if (user is null)
        {
            throw new LingualLoopException(ErrorCode.NoDataInUsers.CreateMessage(request.UserId),
                ErrorCode.NoDataInUsers.GetDescription(request.UserId), HttpStatusCode.BadRequest);
        }

        return new GetScoreWithLivesByIdResponse()
        {
            UserId = user.Id,
            Score = user.UserScore.Score,
            Experience = user.UserScore.Experience,
            Level = LevelRules.GetLevel(user.UserScore.Score),
            LevelProgress = LevelRules.GetProgressInLevel(user.UserScore.Score),
            LevelBandSize = LevelRules.BandSize
        };
    }
}