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

public class CreateVideoCommandHandler : IRequestHandler<CreateVideoRequest, CreateVideoResponse>
{
    private readonly ILingualLoopGenericRepository<Video> _genericRepository;
    private readonly LingualLoopContext _context;

    public CreateVideoCommandHandler(ILingualLoopGenericRepository<Video> genericRepository)
    {
        _genericRepository = genericRepository;
        _context = genericRepository.GetDbContext();
    }

    public async Task<CreateVideoResponse> Handle(CreateVideoRequest request, CancellationToken cancellationToken)
    {
        var checkVideo = await _genericRepository.FirstAsync(x => x.VideoUrl.Equals(request.VideoUrl), 
            video => new Video() { VideoUrl = video.VideoUrl, VideoTitle = video.VideoTitle});
        if (checkVideo != null)
        {
            throw new LingualLoopException(ErrorCode.TheVideoWasRegistered.CreateMessage(checkVideo.VideoUrl, checkVideo.VideoTitle),
                ErrorCode.TheVideoWasRegistered.GetDescription(checkVideo.VideoUrl, checkVideo.VideoTitle), HttpStatusCode.Conflict);
        }

        var video = new Video()
        {
            VideoUrl = request.VideoUrl,
            VideoTitle = request.VideoTitle,
            VideoDescription = request.VideoDescription
        };

        _genericRepository.Create(video);
        var response = await _genericRepository.SaveChangesAsync(cancellationToken);
        if (response == 0)
        {
            throw new LingualLoopException(ErrorCode.NoDataCreatedInVideos.CreateMessage(video.VideoUrl, video.VideoTitle),
                ErrorCode.NoDataCreatedInVideos.GetDescription(video.VideoUrl, video.VideoTitle), HttpStatusCode.InternalServerError);
        }

        return new CreateVideoResponse()
        {
            VideoUrl = video.VideoUrl,
            VideoTitle = video.VideoTitle,
            VideoDescription = video.VideoDescription
        };
    }
}