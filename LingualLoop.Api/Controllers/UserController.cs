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
using Service.DataTransferObjects.Requests.Profile;
using Service.DataTransferObjects.Responses;
using Service.DataTransferObjects.Responses.Profile;
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

    [HttpGet("{id}/learning-stats")]
    public async Task<ActionResult<ApiResponse<GetProfileLearningStatsResponse>>> GetProfileLearningStats(
        [FromRoute] string id)
    {
        var response = await _mediator.Send(new GetProfileLearningStatsRequest() { UserId = id });

        return Ok(new ApiResponse<GetProfileLearningStatsResponse>()
        {
            Data = response
        });
    }

    [HttpPost("{id}/daily-activity")]
    public async Task<ActionResult<ApiResponse<RecordDailyActivityResponse>>> RecordDailyActivity(
        [FromRoute] string id)
    {
        var response = await _mediator.Send(new RecordDailyActivityRequest() { UserId = id });

        return Ok(new ApiResponse<RecordDailyActivityResponse>()
        {
            Data = response
        });
    }
    
    [HttpPost("{id}/streak/resolve-freeze")]
    public async Task<ActionResult<ApiResponse<RecordDailyActivityResponse>>> ResolveStreakFreeze(
        [FromRoute] string id,
        [FromQuery] bool useFreeze)
    {
        var response = await _mediator.Send(new ResolveStreakFreezeRequest
        {
            UserId = id,
            UseFreeze = useFreeze
        });

        return Ok(new ApiResponse<RecordDailyActivityResponse>()
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
            BoostActive = request.BoostActive,
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
                    Experience = updateScoreResponse.Experience,
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
                Experience = updateScoreResponse.Experience,
            }
        });
    }
    
    [HttpPost("update-lives")]
    public async Task<ActionResult<ApiResponse<UpdateLivesResponse>>> UpdateLivesById([FromBody] UpdateLivesRequest request)
    {
        var updateLivesResponse = await _mediator.Send(new UpdateLivesRequest() { UserId = request.UserId });

        // Bu uç oyuna giriş bedelidir: bilet düşülemediyse oyun açılmamalı.
        // VideoController.GetRandomQuestionByUserId ile aynı kalıp.
        if (!updateLivesResponse.Spent)
        {
            throw new LingualLoopException(ErrorCode.TheUserHasNoLives.CreateMessage(updateLivesResponse.Lives),
                ErrorCode.TheUserHasNoLives.GetDescription(updateLivesResponse.Lives), HttpStatusCode.BadRequest);
        }

        return Ok(new ApiResponse<UpdateLivesResponse>()
        {
            Data = updateLivesResponse
        });
    }
    
    [HttpGet("{id}/score-with-lives")]
    public async Task<ActionResult<ApiResponse<GetScoreWithLivesByIdResponse>>> GetScoreWithLivesById([FromRoute] string id)
    {
        var getScoreByIdResponse = await _mediator.Send(new GetScoreByIdRequest() { UserId = id });
        
        var getLivesByIdResponse = await _mediator.Send(new GetLivesByIdRequest() { UserId = id });

        var getLeagueByUserIdResponse = await _mediator.Send(new GetLeagueByUserIdRequest() { UserId = id });

        var getStreakResponse = await _mediator.Send(new GetStreakByIdRequest() { UserId = id });

        return Ok(new ApiResponse<GetScoreWithLivesByIdResponse>()
        {
            Data = new GetScoreWithLivesByIdResponse()
            {
                UserId = id,
                UserNickname = getScoreByIdResponse.UserNickname,
                Score = getScoreByIdResponse.Score,
                Experience = getScoreByIdResponse.Experience,
                Level = getScoreByIdResponse.Level,
                LevelProgress = getScoreByIdResponse.LevelProgress,
                LevelBandSize = getScoreByIdResponse.LevelBandSize,
                Lives = getLivesByIdResponse.Lives,
                MaxLives = getLivesByIdResponse.MaxLives,
                // Tavandayken beklenen bir bilet yok; zamanlayıcı ancak
                // tavandan ilk düşüşte kurulduğu için o durumda anlamsız.
                NextTicketAt = getLivesByIdResponse.Lives < getLivesByIdResponse.MaxLives
                    ? getLivesByIdResponse.LastLivesResetTime
                    : null,
                League = getLeagueByUserIdResponse,
                Streak = getStreakResponse.CurrentStreak,
                FreezeCount = getStreakResponse.FreezeCount,
                PlayedToday = getStreakResponse.PlayedToday,
                StreakWeek = getStreakResponse.Week
            }
        });
    }

    [HttpGet("{id}/league")]
    public async Task<ActionResult<ApiResponse<LeagueProgressResponse>>> GetLeagueByUserId([FromRoute] string id)
    {
        var response = await _mediator.Send(new GetLeagueByUserIdRequest() { UserId = id });

        return Ok(new ApiResponse<LeagueProgressResponse>()
        {
            Data = response
        });
    }

    [HttpPost("{id}/league/promotion/acknowledge")]
    public async Task<ActionResult<ApiResponse<AcknowledgeLeaguePromotionResponse>>>
        AcknowledgeLeaguePromotion([FromRoute] string id)
    {
        var response = await _mediator.Send(new AcknowledgeLeaguePromotionRequest
        {
            UserId = id
        });

        return Ok(new ApiResponse<AcknowledgeLeaguePromotionResponse>
        {
            Data = response
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
