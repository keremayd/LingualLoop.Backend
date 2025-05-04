using MediatR;
using Microsoft.AspNetCore.Mvc;
using Service.DataTransferObjects.Requests.Badge;
using Service.DataTransferObjects.Responses.Badge;

namespace LingualLoop.Api.Controllers;

[ApiController]
[Route("ll-api/badges")]
public class BadgeController : ControllerBase
{
    private readonly IMediator _mediator;

    public BadgeController(IMediator mediator)
    {
        _mediator = mediator;
    }
    
    /// <summary>
    /// Belirtilen kullanıcının kazandığı rozetleri getirir.
    /// </summary>
    [HttpGet("{userId}")]
    public async Task<ActionResult<ApiResponse<List<GetBadgesByIdResponse>>>> GetBadgesById([FromRoute] string userId)
    {
        var response = await _mediator.Send(new GetBadgesByIdRequest() { UserId = userId });
        
        return Ok(new ApiResponse<List<GetBadgesByIdResponse>>
        {
            Data = response
        });
    }

    /// <summary>
    /// Kullanıcıya rozet ekler.
    /// </summary>
    [HttpPost("{badgeId:int}/earn/{userId}")]
    public async Task<ActionResult<ApiResponse<EarnBadgeByIdResponse>>> EarnBadgeById([FromRoute] int badgeId, string userId)
    {
        var response = await _mediator.Send(new EarnBadgeByIdRequest(){ UserId = userId, BadgeId = badgeId });
        
        return Ok(new ApiResponse<EarnBadgeByIdResponse>()
        {
            Data = response
        });
    }
}