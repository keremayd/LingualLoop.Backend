using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using AwsService.Abstractions;
using Common.Enums;
using Common.Exceptions;
using Common.Extensions;
using Common.Helper;
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
    public async Task<ActionResult<ApiResponse<RegisterUserResponse>>> RegisterUser([FromForm] RegisterUserWithPhotoRequest  request)
    {
        var file = request.File;
        var name = request.FirstName + request.LastName;
        var slug = name.ToSluggedUsername(); 

        var registerUserRequest = new RegisterUserRequest()
        {
            UserNickname = slug,
            UserName= slug,
            FirstName = request.FirstName,
            LastName  = request.LastName,
            Password  =  request.Password,
            Email = request.Email,
            PhoneNumber = request.PhoneNumber,
            // Anonim kayıt isteği rol seçemez.
            Roles = new List<string> { "User" },
        };
        
        var response = await _mediator.Send(registerUserRequest);
        
        var extension = Path.GetExtension(file.FileName);
        var key = $"profile-photos/{response.User!.Id}/profile{extension}";

        await _amazonService.UploadFileAsync(key, file.OpenReadStream(), file.ContentType);
        
        await _mediator.Send(new UploadPhotoByIdRequest() { Id = response.User!.Id, PhotoUrl = key });

        var photoSignedUrl = _amazonService.GeneratePreSignedUrl(key, BucketType.ProfilePhotos);

        return Ok(new ApiResponse<RegisterUserResponse>()
        {
            Data = new RegisterUserResponse()
            {
                User = response.User,
                SignedUrl = photoSignedUrl
            },
            Message = "User başarıyla oluşturuldu."
        });
    }

    [HttpPost("login")]
    public async Task<ActionResult<ApiResponse<AuthenticateResponse>>> Authenticate([FromBody] ValidateUserRequest request)
    {
        var validateUserResponse = await _mediator.Send(new ValidateUserRequest() { Email = request.Email, Password = request.Password });

        var userRank = await _mediator.Send(new GetRankByUserIdRequest() { UserId = validateUserResponse.User.Id });
        
        var createTokenResponse = await _mediator.Send(new CreateTokenRequest() { Id = validateUserResponse.User.Id, Password = request.Password, PopulateExp = true});

        var photoSignedUrl = _amazonService.GeneratePreSignedUrl(validateUserResponse.User.ProfilePhoto!, BucketType.ProfilePhotos);
        
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
                UserRank = userRank,
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
            
            // 2.3 Profil fotoğrafını google'dan aldık
            var stream = await ImageHelper.DownloadImageFromUrlAsync(payload.Picture);
            var key = $"profile-photos/{registeredUser.User!.Id}/profile.png";

            await _amazonService.UploadFileAsync(key, stream, "image/png");
            
            await _mediator.Send(new UploadPhotoByIdRequest() { Id = registeredUser.User.Id, PhotoUrl = key });
            
            getUserByEmailResponse.User = registeredUser.User;
        }

        // 3–4. Rank, token ve fotoğraf
        return Ok(new ApiResponse<AuthenticateResponse>
        {
            Data = await SessionFor(getUserByEmailResponse.User)
        });
    }

    /// <summary>
    /// Apple ile giriş: token sunucuda doğrulanır, hesap bulunur, bağlanır ya da
    /// açılır (bkz. <see cref="AppleLoginCommandHandler"/>).
    /// </summary>
    [HttpPost("apple-login")]
    public async Task<ActionResult<ApiResponse<AuthenticateResponse>>> AppleLogin([FromBody] AppleLoginRequest request)
    {
        var response = await _mediator.Send(request);

        return Ok(new ApiResponse<AuthenticateResponse>
        {
            Data = await SessionFor(response.User)
        });
    }

    /// <summary>
    /// Sosyal girişlerin ortak sonu: sıralama, oturum anahtarları ve imzalı
    /// fotoğraf adresi. Apple ile açılan hesabın fotoğrafı yok; Google ile de
    /// girebildiği için (e-posta eşleşmesi) adres boşsa imzalanmaz.
    /// </summary>
    private async Task<AuthenticateResponse> SessionFor(Postgres.Models.User user)
    {
        var userRank = await _mediator.Send(new GetRankByUserIdRequest() { UserId = user.Id });

        var createTokenResponse = await _mediator.Send(new CreateTokenRequest
        {
            Id = user.Id,
            Password = null,
            PopulateExp = true
        });

        var photoSignedUrl = string.IsNullOrEmpty(user.ProfilePhoto)
            ? string.Empty
            : _amazonService.GeneratePreSignedUrl(user.ProfilePhoto, BucketType.ProfilePhotos);

        return new AuthenticateResponse
        {
            UserId = user.Id,
            FirstName = user.FirstName!,
            LastName = user.LastName!,
            DisplayName = user.DisplayName,
            ProfilePhotoUrl = photoSignedUrl,
            UserName = user.UserName!,
            UserNickname = user.UserNickname,
            UserRank = userRank,
            AccessToken = createTokenResponse.AccessToken,
            RefreshToken = createTokenResponse.RefreshToken
        };
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
