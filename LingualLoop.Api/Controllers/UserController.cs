using MediatR;
using Microsoft.AspNetCore.Mvc;
using Service.DataTransferObjects.Requests;
using Service.DataTransferObjects.Responses;
using Service.Handlers.Commands;

namespace LingualLoop.Api.Controllers;

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
    public async Task<IActionResult> GetAllUsers()
    {
        var response = await _mediator.Send(new GetAllUsersRequest());
        return Ok(response);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetUserById([FromRoute] int id)
    {
        var response = await _mediator.Send(new GetUserByIdRequest() { UserId = id });
        return Ok(response);
    }

    [HttpPost]
    public async Task<IActionResult> CreateUser([FromBody] CreateUserRequest request)
    {
        var response = await _mediator.Send(new CreateUserRequest() { UserName = request.UserName});
        return Ok(response);
    }
    
    [HttpPut("{id}/score")]
    public async Task<ActionResult<ApiResponse<UpdateScoreResponse>>> UpdateUserScore([FromRoute] int id, [FromBody] int point)
    {
        var response = await _mediator.Send(new UpdateScoreRequest() { UserId = id, Point = point });
        
        return Ok(new ApiResponse<UpdateScoreResponse>()
        {
            Data = response,
            Message = "User puanı güncellendi."
        });
    }
}