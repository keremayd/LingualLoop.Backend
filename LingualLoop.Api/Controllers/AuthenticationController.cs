using System.Net;
using Common.Enums;
using Common.Exceptions;
using Common.Extensions;
using MediatR;
using Microsoft.AspNetCore.Cors;
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
    public async Task<ActionResult<ApiResponse<AuthenticateResponse>>> Authenticate([FromBody] ValidateUserRequest request)
    {
        var validateUserResponse = await _mediator.Send(new ValidateUserRequest() { UserName = request.UserName, Password = request.Password });

        var createTokenResponse = await _mediator.Send(new CreateTokenRequest() { UserName = request.UserName, Password = request.Password, PopulateExp = true});

        return Ok(new ApiResponse<AuthenticateResponse>()
        {
            Data = new AuthenticateResponse()
            {
                UserId = validateUserResponse.UserId,
                AccessToken = createTokenResponse.AccessToken,
                RefreshToken = createTokenResponse.RefreshToken
            }
        });
    }

    [HttpPost("refresh")]
    public async Task<ActionResult<ApiResponse<CreateTokenResponse>>> Refresh([FromBody] TokenDto tokenDto)
    {
        var refreshTokenResponse = await _mediator.Send(new RefreshTokenRequest() { AccessToken = tokenDto.AccessToken, RefreshToken = tokenDto.RefreshToken});

        var createTokenResponse = await _mediator.Send(new CreateTokenRequest() { UserName = refreshTokenResponse.UserName, Password = refreshTokenResponse.PasswordHash, PopulateExp = false});

        return Ok(new ApiResponse<CreateTokenResponse>()
        {
            Data = createTokenResponse
        });
    }
}