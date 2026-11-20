using Stelinda.Api.Extensions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Service.DataTransferObjects.Requests;
using Service.DataTransferObjects.Responses;

namespace Stelinda.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/quests")]
public class QuestController : ControllerBase
{
    private readonly IMediator _mediator;

    public QuestController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet("{userId}")]
    public async Task<ActionResult<ApiResponse<GetDailyQuestsResponse>>> GetDailyQuests(
        [FromRoute] string userId)
    {
        var response = await _mediator.Send(new GetDailyQuestsRequest { UserId = userId, Locale = Request.NativeLanguage() });

        return Ok(new ApiResponse<GetDailyQuestsResponse>()
        {
            Data = response
        });
    }

    [HttpPost("claim")]
    public async Task<ActionResult<ApiResponse<ClaimQuestRewardResponse>>> ClaimReward(
        [FromBody] ClaimQuestRewardRequest request)
    {
        var response = await _mediator.Send(request);

        return Ok(new ApiResponse<ClaimQuestRewardResponse>()
        {
            Data = response
        });
    }

    /// <summary>Günün bütün görevleri bitince seri korumasını toplar.</summary>
    [HttpPost("freeze")]
    public async Task<ActionResult<ApiResponse<ClaimQuestFreezeResponse>>> ClaimFreeze(
        [FromBody] ClaimQuestFreezeRequest request)
    {
        var response = await _mediator.Send(request);

        return Ok(new ApiResponse<ClaimQuestFreezeResponse>()
        {
            Data = response
        });
    }
}
