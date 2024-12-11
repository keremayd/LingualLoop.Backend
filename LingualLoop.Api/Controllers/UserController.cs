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

//[Authorize]
[ApiController]
[Route("ll-api/users")]
public class UserController : ControllerBase
{
    private readonly IMediator _mediator;

    public UserController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<List<GetAllUsersResponse>>>> GetAllUsers()
    {
        var response = await _mediator.Send(new GetAllUsersRequest());
        return Ok(new ApiResponse<List<GetAllUsersResponse>>()
        {
            Data = response
        });
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ApiResponse<GetUserByIdResponse>>> GetUserById([FromRoute] string id)
    {
        var response = await _mediator.Send(new GetUserByIdRequest() { UserId = id });
        
        return Ok(new ApiResponse<GetUserByIdResponse>()
        {
            Data = response
        });
    }
    
    [HttpPost("updateScore")]
    public async Task<ActionResult<ApiResponse<UpdateScoreResponse>>> UpdateScoreById([FromBody] UpdateScoreRequest request)
    {
        var updateScoreResponse = await _mediator.Send(new UpdateScoreRequest() { UserId = request.UserId, Point = request.Point});

        if (request.Point == -1)
        {
            var updateLivesResponse = await _mediator.Send(new UpdateLivesRequest() { UserId = request.UserId });
            
            return Ok(new ApiResponse<UpdateScoreResponse>()
            {
                Data = new UpdateScoreResponse()
                {
                    UserId = updateScoreResponse.UserId,
                    Score = updateScoreResponse.Score,
                    Lives = updateLivesResponse.Lives
                }
            });
        }

        return Ok(new ApiResponse<UpdateScoreResponse>()
        {
            Data = new UpdateScoreResponse()
            {
                UserId = updateScoreResponse.UserId,
                Score = updateScoreResponse.Score,
            }
        });
    }
    
    [HttpGet("{id}/score-with-lives")]
    public async Task<ActionResult<ApiResponse<GetScoreWithLivesByIdResponse>>> GetScoreWithLivesById([FromRoute] string id)
    {
        var getScoreByIdResponse = await _mediator.Send(new GetScoreByIdRequest() { UserId = id });
        
        var getLivesByIdResponse = await _mediator.Send(new GetLivesByIdRequest() { UserId = id });

        return Ok(new ApiResponse<GetScoreWithLivesByIdResponse>()
        {
            Data = new GetScoreWithLivesByIdResponse()
            {
                UserId = id,
                UserNickname = getScoreByIdResponse.UserNickname,
                Score = getScoreByIdResponse.Score,
                Lives = getLivesByIdResponse.Lives
            }
        });
    }
}