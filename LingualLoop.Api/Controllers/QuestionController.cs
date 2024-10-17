using System.Net;
using Common.Enums;
using Common.Exceptions;
using Common.Extensions;
using MediatR;
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

    [HttpGet("random/{userId}")]
    public async Task<ActionResult<ApiResponse<GetQuestionByScoreResponse>>> GetRandomQuestionByUserId([FromRoute] string userId)
    {
        var userScoreResponse = await _mediator.Send(new GetScoreByIdRequest() { UserId = userId });

        var response = await _mediator.Send(new GetQuestionByScoreRequest() { UserScore = userScoreResponse.UserScore.Score, UserId = userScoreResponse.UserId });
        
        return Ok(new ApiResponse<GetQuestionByScoreResponse>()
        {
            Data = response
        });
    }
}