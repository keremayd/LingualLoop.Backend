using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using AwsService.Abstractions;
using Common.Enums;
using Common.Exceptions;
using Common.Extensions;
using Google.Apis.Auth;
using MediatR;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using Service.DataTransferObjects.Requests;
using Service.DataTransferObjects.Requests.Authentication;
using Service.DataTransferObjects.Responses;
using Service.Handlers.Commands;

namespace LingualLoop.Api.Controllers;

[ApiController]
[Route("ll-api/authentication")]
public class AuthenticationController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly IAwsService _amazonService;

    public AuthenticationController(IMediator mediator, IAwsService amazonService)
    {
        _mediator = mediator;
        _amazonService = amazonService;
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

        var createTokenResponse = await _mediator.Send(new CreateTokenRequest() { Id = validateUserResponse.User.Id, Password = request.Password, PopulateExp = true});

        var photoSignedUrl = _amazonService.GeneratePreSignedUrl(validateUserResponse.User.ProfilePhoto!);
        
        return Ok(new ApiResponse<AuthenticateResponse>()
        {
            Data = new AuthenticateResponse()
            {
                UserId = validateUserResponse.User.Id,
                FirstName = validateUserResponse.User.FirstName!,
                LastName = validateUserResponse.User.LastName!,
                DisplayName = validateUserResponse.User.DisplayName,
                ProfilePhotoUrl = photoSignedUrl,
                UserNickname = validateUserResponse.User.UserNickname,
                UserName = validateUserResponse.User.UserName!,
                AccessToken = createTokenResponse.AccessToken,
                RefreshToken = createTokenResponse.RefreshToken
            }
        });
    }
    
    [HttpPost("google-login")]
    public async Task<ActionResult<ApiResponse<AuthenticateResponse>>> GoogleLogin([FromBody] ValidateUserByEmailRequest request)
    {
        // 1. Google Token'ı doğrula
        var payload = await GoogleJsonWebSignature.ValidateAsync(request.IdToken);

        // 2.1 Email ile kullanıcı var mı kontrol et
        var getUserByEmailResponse = await _mediator.Send(new GetUserByEmailRequest { Email = payload.Email });
        
        if (getUserByEmailResponse.User == null)
        {
            // 2.2 Kayıt işlemi
            var slug = payload.Name.ToSluggedUsername(); 
            
            var registeredUser = await _mediator.Send(new RegisterUserRequest() { Email = payload.Email, UserName = slug, FirstName = payload.GivenName, LastName = payload.FamilyName, UserNickname = slug , Roles = new List<string>(){"User"}});
            
            getUserByEmailResponse.User = registeredUser.User;
        }

        // 3. Token üret
        var createTokenResponse = await _mediator.Send(new CreateTokenRequest
        {
            Id = getUserByEmailResponse.User!.Id,
            Password = null,
            PopulateExp = true
        });

        return Ok(new ApiResponse<AuthenticateResponse>
        {
            Data = new AuthenticateResponse
            {
                UserId = getUserByEmailResponse.User.Id,
                FirstName = getUserByEmailResponse.User.FirstName!,
                LastName = getUserByEmailResponse.User.LastName!,
                DisplayName = getUserByEmailResponse.User.DisplayName,
                UserName = getUserByEmailResponse.User.UserName!,
                UserNickname = getUserByEmailResponse.User.UserNickname,
                AccessToken = createTokenResponse.AccessToken,
                RefreshToken = createTokenResponse.RefreshToken
            }
        });
    }

    [HttpPost("refresh")]
    public async Task<ActionResult<ApiResponse<CreateTokenResponse>>> Refresh([FromBody] TokenDto tokenDto)
    {
        var refreshTokenResponse = await _mediator.Send(new RefreshTokenRequest() { AccessToken = tokenDto.AccessToken, RefreshToken = tokenDto.RefreshToken});

        var createTokenResponse = await _mediator.Send(new CreateTokenRequest() { Id = refreshTokenResponse.Id, Password = refreshTokenResponse.PasswordHash, PopulateExp = false});

        return Ok(new ApiResponse<CreateTokenResponse>()
        {
            Data = createTokenResponse
        });
    }
}