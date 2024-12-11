using System.Net;
using Common.Enums;
using Common.Exceptions;
using Common.Extensions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Service.DataTransferObjects.Requests;
using Service.DataTransferObjects.Responses;

namespace LingualLoop.Api.Controllers;

[ApiController]
[Route("ll-api/questions")]
public class QuestionController : ControllerBase
{
    private readonly IMediator _mediator;

    public QuestionController(IMediator mediator)
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
}