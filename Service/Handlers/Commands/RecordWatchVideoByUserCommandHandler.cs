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

public class RecordWatchVideoByUserCommandHandler : IRequestHandler<RecordWatchVideoByUserRequest, RecordWatchVideoByUserResponse>
{
    private readonly ILingualLoopGenericRepository<UserVideoHistory> _genericRepository;
    private readonly LingualLoopContext _context;

    public RecordWatchVideoByUserCommandHandler(ILingualLoopGenericRepository<UserVideoHistory> genericRepository)
    {
        _genericRepository = genericRepository;
        _context = genericRepository.GetDbContext();
    }

    public async Task<RecordWatchVideoByUserResponse> Handle(RecordWatchVideoByUserRequest request, CancellationToken cancellationToken)
    {
        var userVideoHistory = await _genericRepository.FirstAsync(u => u.UserId == request.UserId && u.VideoId == request.VideoId,
                user => new UserVideoHistory() { VideoId = user.VideoId, UserId = user.UserId, WatchedDate = user.WatchedDate});
        if (userVideoHistory != null)
        {
            return new RecordWatchVideoByUserResponse()
            {
                VideoId = userVideoHistory.VideoId,
                UserId = userVideoHistory.UserId,
                WatchedDate = userVideoHistory.WatchedDate
            };
        }

        //TODO mapper ile tür dönüşümü yapılacak
        var entity = new UserVideoHistory()
        {
            UserId = request.UserId,
            VideoId = request.VideoId,
            WatchedDate = DateTime.UtcNow
        };
        _genericRepository.Create(entity);
        var response = await _genericRepository.SaveChangesAsync(cancellationToken);
        if (response == 0)
        {
            throw new LingualLoopException(ErrorCode.NoDataCreatedInUserVideoHistory.CreateMessage(request.VideoId, request.UserId),
                ErrorCode.NoDataCreatedInUserVideoHistory.GetDescription(request.VideoId, request.UserId), HttpStatusCode.InternalServerError);
        }
    
        return new RecordWatchVideoByUserResponse()
        {
            VideoId = entity.VideoId,
            UserId = entity.UserId,
            WatchedDate = entity.WatchedDate
        };
    }
}