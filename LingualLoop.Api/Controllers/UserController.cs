using System.Net;
using Common.Enums;
using Common.Exceptions;
using Common.Extensions;
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
    public async Task<ActionResult<ApiResponse<List<GetAllUsersResponse>>>> GetAllUsers()
    {
        var response = await _mediator.Send(new GetAllUsersRequest());
        return Ok(new ApiResponse<List<GetAllUsersResponse>>()
        {
            Data = response
        });
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ApiResponse<GetUserByIdResponse>>> GetUserById([FromRoute] int id)
    {
        var response = await _mediator.Send(new GetUserByIdRequest() { UserId = id });
        
        return Ok(new ApiResponse<GetUserByIdResponse>()
        {
            Data = response
        });
    }
    
    [HttpGet("{id}/score")]
    public async Task<ActionResult<ApiResponse<GetScoreByIdResponse>>> GetScoreById([FromRoute] int id)
    {
        var response = await _mediator.Send(new GetScoreByIdRequest() { UserId = id });

        return Ok(new ApiResponse<GetScoreByIdResponse>()
        {
            Data = response
        });
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<CreateUserResponse>>> CreateUser([FromBody] CreateUserRequest request)
    {
        if (string.IsNullOrEmpty(request.UserNickname))
        {
            throw new LingualLoopException(ErrorCode.UserNicknameCannotBeNullOrEmpty.CreateMessage(),
                ErrorCode.UserNicknameCannotBeNullOrEmpty.GetDescription(), HttpStatusCode.BadRequest);
        }
        
        var response = await _mediator.Send(new CreateUserRequest() { UserNickname = request.UserNickname });
        
        return Ok(new ApiResponse<CreateUserResponse>()
        {
            Data = response,
            Message = "User başarıyla oluşturuldu."
        });
    }
}