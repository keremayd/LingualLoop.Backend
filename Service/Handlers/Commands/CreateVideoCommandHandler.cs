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
    private readonly IStelindaGenericRepository<Video> _genericRepository;
    private readonly StelindaContext _context;

    public CreateVideoCommandHandler(IStelindaGenericRepository<Video> genericRepository)
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
            throw new StelindaException(ErrorCode.TheVideoWasRegistered.CreateMessage(checkVideo.VideoUrl, checkVideo.VideoTitle),
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
            throw new StelindaException(ErrorCode.NoDataCreatedInVideos.CreateMessage(video.VideoUrl, video.VideoTitle),
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