using System.Net;
using Common.Enums;
using Common.Exceptions;
using Common.Extensions;
using MediatR;
using Postgres.Abstractions;
using Postgres.Models;
using Service.DataTransferObjects.Requests;
using Service.DataTransferObjects.Responses.Video;

namespace Service.Handlers.Queries.Video;

public class GetVideosByIdQueryHandler : IRequestHandler<GetVideosByIdRequest, List<GetVideosByIdResponse>>
{
    private readonly ILingualLoopGenericRepository<UserVideo> _userVideoRepository;

    public GetVideosByIdQueryHandler(ILingualLoopGenericRepository<UserVideo> userVideoRepository)
    {
        _userVideoRepository = userVideoRepository;
    }

    public async Task<List<GetVideosByIdResponse>> Handle(GetVideosByIdRequest request, CancellationToken cancellationToken)
    {
        var videoList = await _userVideoRepository.GetListAsync(u => u.UserId == request.UserId,
            video => new UserVideo()
            {
                UserVideoId = video.UserVideoId,
                UserId = video.UserId,
                VideoId = video.VideoId,
                SavedDate = video.SavedDate,
                Video = video.Video
            }
        );

        if (videoList is null || !videoList.Any())
            throw new LingualLoopException(ErrorCode.NoDataFoundInUserVideo.CreateMessage(request.UserId),
                ErrorCode.NoDataFoundInUserVideo.GetDescription(request.UserId), HttpStatusCode.BadRequest);

        return videoList.Select(b => new GetVideosByIdResponse
        {
            UserVideoId = b.UserVideoId,
            UserId = b.UserId,
            SavedDate = b.SavedDate,
            Video = b.Video!
        }).ToList();
    }
}