using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Service.DataTransferObjects.Requests;
using Service.DataTransferObjects.Responses;

namespace Stelinda.Api.Controllers;

/// Engelleme ve bildirim (App Store kuralı 1.2: kullanıcıların birbirine
/// gösterdiği içerik için bildirme ve engelleme). Yalnız token sahibi kendi
/// adına engeller ve bildirir.
[ApiController]
[Authorize]
[Route("api/users")]
public class ModerationController : ControllerBase
{
    private readonly IMediator _mediator;

    public ModerationController(IMediator mediator)
    {
        _mediator = mediator;
    }

    public class BlockBody
    {
        public string BlockedUserId { get; set; } = string.Empty;
    }

    public class ReportBody
    {
        public string ReportedUserId { get; set; } = string.Empty;
        public string Reason { get; set; } = string.Empty;
        public string Context { get; set; } = "league";
    }

    [HttpPost("{id}/blocks")]
    public async Task<ActionResult<ApiResponse<BlockUserResponse>>> Block(
        [FromRoute] string id, [FromBody] BlockBody body)
    {
        if (!IsOwner(id)) return Forbid();

        var response = await _mediator.Send(new BlockUserRequest
        {
            UserId = id,
            BlockedUserId = body.BlockedUserId,
            Block = true,
        });
        return Ok(new ApiResponse<BlockUserResponse> { Data = response });
    }

    [HttpDelete("{id}/blocks/{blockedUserId}")]
    public async Task<ActionResult<ApiResponse<BlockUserResponse>>> Unblock(
        [FromRoute] string id, [FromRoute] string blockedUserId)
    {
        if (!IsOwner(id)) return Forbid();

        var response = await _mediator.Send(new BlockUserRequest
        {
            UserId = id,
            BlockedUserId = blockedUserId,
            Block = false,
        });
        return Ok(new ApiResponse<BlockUserResponse> { Data = response });
    }

    [HttpPost("{id}/reports")]
    public async Task<ActionResult<ApiResponse<ReportUserResponse>>> Report(
        [FromRoute] string id, [FromBody] ReportBody body)
    {
        if (!IsOwner(id)) return Forbid();

        var response = await _mediator.Send(new ReportUserRequest
        {
            UserId = id,
            ReportedUserId = body.ReportedUserId,
            Reason = body.Reason,
            Context = body.Context,
        });
        return Ok(new ApiResponse<ReportUserResponse> { Data = response });
    }

    private bool IsOwner(string id) =>
        string.Equals(User.Identity?.Name, id, StringComparison.Ordinal);
}
