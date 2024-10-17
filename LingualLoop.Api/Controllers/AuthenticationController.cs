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
[Route("ll-api/authentication")]
public class AuthenticationController : ControllerBase
{
    private readonly IMediator _mediator;

    public AuthenticationController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost("register")]
    public async Task<ActionResult<ApiResponse<RegisterUserResponse>>> RegisterUser([FromBody] RegisterUserRequest request)
    {
        if (string.IsNullOrEmpty(request.UserNickname))
        {
            throw new LingualLoopException(ErrorCode.UserNicknameCannotBeNullOrEmpty.CreateMessage(),
                ErrorCode.UserNicknameCannotBeNullOrEmpty.GetDescription(), HttpStatusCode.BadRequest);
        }

        var response = await _mediator.Send(request);

        return Ok(new ApiResponse<RegisterUserResponse>()
        {
            Data = response,
            Message = "User başarıyla oluşturuldu."
        });
    }

    [HttpPost("login")]
    public async Task<ActionResult<ApiResponse<object>>> Authenticate([FromBody] ValidateUserRequest request)
    {
        var response = await _mediator.Send(new ValidateUserRequest() { UserName = request.UserName, Password = request.Password });

        var tokenResponse = await _mediator.Send(new CreateTokenRequest() { UserName = request.UserName, Password = request.Password });

        return Ok(new
        {
            Token = tokenResponse
        });
    }
}