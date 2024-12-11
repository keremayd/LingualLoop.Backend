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

public class UpdateLivesCommandHandler : IRequestHandler<UpdateLivesRequest, UpdateLivesResponse>
{
    private readonly ILingualLoopGenericRepository<User> _userRepository;
    private readonly ILingualLoopGenericRepository<UserLives> _userLivesRepository;
    private readonly LingualLoopContext _context;
    
    public UpdateLivesCommandHandler(ILingualLoopGenericRepository<User> userRepository, ILingualLoopGenericRepository<UserLives> userLivesRepository)
    {
        _userLivesRepository = userLivesRepository;
        _userRepository = userRepository;
        _context = userRepository.GetDbContext();
    }
    
    public async Task<UpdateLivesResponse> Handle(UpdateLivesRequest request, CancellationToken cancellationToken)
    {
        var user = await _userRepository.FirstAsync(u => u.Id == request.UserId,
            user => new User() { Id = user.Id, UserNickname = user.UserNickname, UserScore = user.UserScore});
        if (user is null)
            throw new LingualLoopException(ErrorCode.NoDataInUsers.CreateMessage(request.UserId),
                ErrorCode.NoDataInUsers.GetDescription(request.UserId), HttpStatusCode.BadRequest);

        var userLives = await _userLivesRepository.FirstAsync(u => u.UserId == request.UserId,
            _userLives => new UserLives() { UserLivesId = _userLives.UserLivesId, UserId = _userLives.UserId, Lives = _userLives.Lives, MaxLives = _userLives.MaxLives, LastLivesResetTime = _userLives.LastLivesResetTime});
        if (userLives is null)
            throw new LingualLoopException(ErrorCode.NoDataFoundInUserLives.CreateMessage(request.UserId),
                ErrorCode.NoDataFoundInUserLives.GetDescription(request.UserId), HttpStatusCode.BadRequest);

        userLives.Lives -= 1;
        userLives.LastLivesResetTime = DateTime.SpecifyKind(userLives.LastLivesResetTime, DateTimeKind.Utc);
        
        _userLivesRepository.Update(userLives);
        await _userLivesRepository.SaveChangesAsync(cancellationToken);
        
        return new UpdateLivesResponse()
        {
            UserId = request.UserId,
            Lives = userLives.Lives
        };
    }
}