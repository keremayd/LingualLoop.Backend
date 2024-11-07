using System.Net;
using Common.Enums;
using Common.Exceptions;
using Common.Extensions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Service.DataTransferObjects.Requests;
using Service.DataTransferObjects.Responses;
using Service.Handlers.Commands;

namespace LingualLoop.Api.Controllers;

[ApiController]
[Route("ll-api/videos")]
public class VideoController : ControllerBase
{
    private readonly IMediator _mediator;

    public VideoController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [Authorize]
    [HttpGet("watched/{userId}")]
    public async Task<ActionResult<ApiResponse<GetWatchedVideosByUserResponse>>> GetWatchedVideosByUser([FromRoute] string userId)
    {
        var response = await _mediator.Send(new GetWatchedVideosByUserRequest() { UserId = userId });
        
        return Ok(new ApiResponse<GetWatchedVideosByUserResponse>()
        {
            Data = response
        });
    }
    
    [Authorize(Roles = "Admin")]
    [HttpPost]
    public async Task<ActionResult<ApiResponse<CreateVideoResponse>>> CreateVideo([FromBody] CreateVideoRequest request)
    {
        var response = await _mediator.Send(new CreateVideoRequest() { VideoUrl = request.VideoUrl, VideoTitle = request.VideoTitle, VideoDescription = request.VideoDescription});
        
        return Ok(new ApiResponse<CreateVideoResponse>()
        {
            Data = response
        });
    }

    [Authorize]
    [HttpPost("{videoId:int}/watch/{userId}")]
    public async Task<ActionResult<ApiResponse<RecordWatchVideoByUserResponse>>> RecordWatchVideoByUser([FromRoute] int videoId, string userId)
    {
        var response = await _mediator.Send(new RecordWatchVideoByUserRequest() { VideoId = videoId, UserId = userId});
        
        return Ok(new ApiResponse<RecordWatchVideoByUserResponse>()
        {
            Data = response
        });
    }
}