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

[Authorize]
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
        var getUserByIdResponse = await _mediator.Send(new UpdateScoreRequest() { UserId = request.UserId, Point = request.Point});
        
        return Ok(new ApiResponse<UpdateScoreResponse>()
        {
            Data = getUserByIdResponse
        });
    }
    
    [HttpGet("{id}/score")]
    public async Task<ActionResult<ApiResponse<GetScoreByIdResponse>>> GetScoreById([FromRoute] string id)
    {
        var response = await _mediator.Send(new GetScoreByIdRequest() { UserId = id });

        return Ok(new ApiResponse<GetScoreByIdResponse>()
        {
            Data = response
        });
    }
}