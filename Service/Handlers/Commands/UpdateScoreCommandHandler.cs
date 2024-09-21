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
    private readonly ILingualLoopGenericRepository<User> _genericRepository;
    private readonly LingualLoopContext _context;
    
    public UpdateScoreCommandHandler(ILingualLoopGenericRepository<User> genericRepository)
    {
        _genericRepository = genericRepository;
        _context = genericRepository.GetDbContext();
    }
    
    public async Task<UpdateScoreResponse> Handle(UpdateScoreRequest request, CancellationToken cancellationToken)
    {
        var user = await _genericRepository.FirstAsync(u => u.UserId == request.UserId,
            user => new User() { UserId = user.UserId, UserName = user.UserName, UserScore = user.UserScore});

        if (user is null)
        {
            throw new LingualLoopException(ErrorCode.NoDataInUser.CreateMessage(request.UserId),
                ErrorCode.NoDataInUser.GetDescription(request.UserId), HttpStatusCode.BadRequest);
        }
        
        user.UserScore.Score += request.Point;
        
        _genericRepository.Update(user);
        await _genericRepository.SaveChangesAsync(cancellationToken);
        
        return new UpdateScoreResponse()
        {
            UserId = request.UserId
        };
    }
}