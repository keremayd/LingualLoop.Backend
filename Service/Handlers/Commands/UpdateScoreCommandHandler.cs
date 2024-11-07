using System.Net;
using Common.Enums;
using Common.Exceptions;
using Common.Extensions;
using MediatR;
using Postgres;
using Postgres.Abstractions;
using Postgres.Models;
using Service.DataTransferObjects.Requests;
using Service.DataTransferObjects.Responses;

namespace Service.Handlers.Commands;

public class UpdateScoreCommandHandler : IRequestHandler<UpdateScoreRequest, UpdateScoreResponse>
{
    private readonly ILingualLoopGenericRepository<User> _userRepository;
    private readonly ILingualLoopGenericRepository<UserScore> _userScoreRepository;
    private readonly LingualLoopContext _context;
    
    public UpdateScoreCommandHandler(ILingualLoopGenericRepository<User> userRepository, ILingualLoopGenericRepository<UserScore> userScoreRepository)
    {
        _userScoreRepository = userScoreRepository;
        _userRepository = userRepository;
        _context = userRepository.GetDbContext();
    }
    
    public async Task<UpdateScoreResponse> Handle(UpdateScoreRequest request, CancellationToken cancellationToken)
    {
        var user = await _userRepository.FirstAsync(u => u.Id == request.UserId,
            user => new User() { Id = user.Id, UserNickname = user.UserNickname, UserScore = user.UserScore});
        if (user is null)
            throw new LingualLoopException(ErrorCode.NoDataInUsers.CreateMessage(request.UserId),
                ErrorCode.NoDataInUsers.GetDescription(request.UserId), HttpStatusCode.BadRequest);

        var userScore = await _userScoreRepository.FirstAsync(u => u.UserId == request.UserId,
            response => new UserScore() { UserScoreId = response.UserScoreId, Score = response.Score, UserId = response.UserId });
        if (userScore == null)
            throw new LingualLoopException(ErrorCode.NoDataInUserScores.CreateMessage(request.UserId),
                ErrorCode.NoDataInUserScores.GetDescription(request.UserId), HttpStatusCode.BadRequest);

        userScore.Score += request.Point;
        
        _userScoreRepository.Update(userScore);
        await _userScoreRepository.SaveChangesAsync(cancellationToken);
        
        return new UpdateScoreResponse()
        {
            UserId = request.UserId,
            Score = userScore.Score
        };
    }
}