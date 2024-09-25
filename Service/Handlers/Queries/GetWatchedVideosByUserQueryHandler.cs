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

namespace Service.Handlers.Queries;

public class GetWatchedVideosByUserQueryHandler : IRequestHandler<GetWatchedVideosByUserRequest, GetWatchedVideosByUserResponse>
{
    private readonly ILingualLoopGenericRepository<User> _genericRepository;
    private readonly LingualLoopContext _context;

    public GetWatchedVideosByUserQueryHandler(ILingualLoopGenericRepository<User> genericRepository)
    {
        _genericRepository = genericRepository;
        _context = genericRepository.GetDbContext();
    }
    
    public async Task<GetWatchedVideosByUserResponse> Handle(GetWatchedVideosByUserRequest request, CancellationToken cancellationToken)
    {
        var user = await _genericRepository.FirstAsync(u => u.UserId == request.UserId,
            user => new User() { UserId = user.UserId, UserNickname = user.UserNickname, VideoHistory = user.VideoHistory});

        if (user is null)
        {
            throw new LingualLoopException(ErrorCode.NoDataInUsers.CreateMessage(request.UserId),
                ErrorCode.NoDataInUsers.GetDescription(request.UserId), HttpStatusCode.BadRequest);
        }
        
        return new GetWatchedVideosByUserResponse()
        {
            UserId = user.UserId,
            UserNickname = user.UserNickname,
            UserVideoHistories = user.VideoHistory
        };
    }
}