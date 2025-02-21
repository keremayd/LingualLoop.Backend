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
    [HttpGet("random/{userId}")]
    public async Task<ActionResult<ApiResponse<GetQuestionByScoreResponse>>> GetRandomQuestionByUserId([FromRoute] string userId)
    {
        var userLives = await _mediator.Send(new GetLivesByIdRequest() { UserId = userId });
        if (userLives.Lives <= 0)
        {
            throw new LingualLoopException(ErrorCode.TheUserHasNoLives.CreateMessage(),
                ErrorCode.TheUserHasNoLives.GetDescription(), HttpStatusCode.BadRequest);
        }
        
        var userScoreResponse = await _mediator.Send(new GetScoreByIdRequest() { UserId = userId });

        var response = await _mediator.Send(new GetQuestionByScoreRequest() { UserScore = userScoreResponse.Score, UserId = userScoreResponse.UserId });
        
        return Ok(new ApiResponse<GetQuestionByScoreResponse>()
        {
            Data = response
        });
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