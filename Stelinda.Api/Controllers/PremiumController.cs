using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Service.DataTransferObjects.Requests;
using Service.DataTransferObjects.Responses;

namespace Stelinda.Api.Controllers;

/// Premium abonelik: App Store satın almasının doğrulanması ve App Store sunucu
/// bildirimleri. Bildirim adresi App Store Connect'te bu uca verilir:
/// https://<sunucu>/api/premium/apple/notifications
[ApiController]
[Route("api/premium")]
public class PremiumController : ControllerBase
{
    private readonly IMediator _mediator;

    public PremiumController(IMediator mediator)
    {
        _mediator = mediator;
    }

    public class ApplePurchaseBody
    {
        public string SignedTransaction { get; set; } = string.Empty;
    }

    public class AppleNotificationBody
    {
        public string SignedPayload { get; set; } = string.Empty;
    }

    [Authorize]
    [HttpPost("apple")]
    public async Task<ActionResult<ApiResponse<PremiumStatusResponse>>> VerifyApplePurchase(
        [FromBody] ApplePurchaseBody body)
    {
        var userId = User.Identity?.Name;
        if (string.IsNullOrEmpty(userId)) return Forbid();

        var response = await _mediator.Send(new VerifyApplePurchaseRequest
        {
            UserId = userId,
            SignedTransaction = body.SignedTransaction,
        });
        return Ok(new ApiResponse<PremiumStatusResponse> { Data = response });
    }

    [AllowAnonymous]
    [HttpPost("apple/notifications")]
    public async Task<IActionResult> AppleNotification([FromBody] AppleNotificationBody body)
    {
        var response = await _mediator.Send(new AppleNotificationRequest { SignedPayload = body.SignedPayload });
        return response.Valid ? Ok() : BadRequest();
    }
}
