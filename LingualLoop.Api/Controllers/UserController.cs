using System.Net;
using AwsService.Abstractions;
using Common.Enums;
using Common.Exceptions;
using Common.Extensions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Service.DataTransferObjects.Requests;
using Service.DataTransferObjects.Requests.Karty;
using Service.DataTransferObjects.Responses;
using Service.Handlers.Commands;

namespace LingualLoop.Api.Controllers;

//[Authorize]
[ApiController]
[Route("ll-api/users")]
public class UserController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly IAwsService _amazonService;

    public UserController(IMediator mediator, IAwsService amazonService)
    {
        _mediator = mediator;
        _amazonService = amazonService;
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
    
    [HttpPost("update-score")]
    public async Task<ActionResult<ApiResponse<UpdateScoreResponse>>> UpdateScoreById([FromBody] UpdateScoreRequest request)
    {
        var updateScoreResponse = await _mediator.Send(new UpdateScoreRequest()
        {
            UserId = request.UserId,
            Point = request.Point,
            KartyId = request.KartyId
        });

        if (request.Point == -1)
        {
            if (request.KartyId.HasValue)
            {
                await _mediator.Send(new RecordWrongKartyRequest()
                {
                    UserId = request.UserId,
                    KartyId = request.KartyId.Value
                });
            }

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
    
    [HttpPost("update-lives")]
    public async Task<ActionResult<ApiResponse<UpdateLivesResponse>>> UpdateLivesById([FromBody] UpdateLivesRequest request)
    {
        var updateLivesResponse = await _mediator.Send(new UpdateLivesRequest() { UserId = request.UserId });
        
        return Ok(new ApiResponse<UpdateScoreResponse>()
        {
            Data = new UpdateScoreResponse()
            {
                Lives = updateLivesResponse.Lives
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
    
    [HttpPost("{id}/upload-profile-photo")]
    [Consumes("multipart/form-data")]
    public async Task<ActionResult<ApiResponse<UploadUserFileResponse>>> UploadUserFile([FromRoute] string id, [FromForm] IFormFile file)
    {
        if (file == null || file.Length == 0)
            return BadRequest(new ApiResponse<string> { Message = "Dosya boş." });

        var user = await _mediator.Send(new GetUserByIdRequest() { UserId = id });

        var extension = Path.GetExtension(file.FileName);
        var key = $"profile-photos/{id}/profile{extension}";

        await _amazonService.UploadFileAsync(key, file.OpenReadStream(), file.ContentType);

        await _mediator.Send(new UploadPhotoByIdRequest() { Id = user.UserId, PhotoUrl = key });

        var photoSignedUrl = _amazonService.GeneratePreSignedUrl(key, BucketType.ProfilePhotos);
        
        return Ok(new ApiResponse<UploadUserFileResponse> 
        { 
            Data = new UploadUserFileResponse()
            {
                Id = user.UserId,
                ProfilePhoto = key,
                SignedUrl = photoSignedUrl
            } 
        });
    }
}
