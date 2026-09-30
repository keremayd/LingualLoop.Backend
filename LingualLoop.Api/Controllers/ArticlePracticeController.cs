using AwsService.Abstractions;
using Common.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Service.DataTransferObjects.Requests.ArticlePractice;
using Service.DataTransferObjects.Responses.ArticlePractice;

namespace LingualLoop.Api.Controllers;

[ApiController]
[Authorize]
[Route("ll-api/article-practice")]
public class ArticlePracticeController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly IAwsService _amazonService;

    public ArticlePracticeController(IMediator mediator, IAwsService amazonService)
    {
        _mediator = mediator;
        _amazonService = amazonService;
    }

    [HttpGet("next/{userId}")]
    public async Task<ActionResult<ApiResponse<GetArticlePracticeTaskResponse>>> GetNext(
        [FromRoute] string userId,
        [FromQuery] int? preferredKartyId)
    {
        var response = await _mediator.Send(new GetArticlePracticeTaskRequest
        {
            UserId = userId,
            PreferredKartyId = preferredKartyId,
        });
        if (response.Task is not null)
        {
            response.Task.KartyUrl = _amazonService.GeneratePreSignedUrl(
                response.Task.KartyUrl,
                BucketType.KartyAssets);
        }
        if (response.NextTask is not null)
        {
            response.NextTask.KartyUrl = _amazonService.GeneratePreSignedUrl(
                response.NextTask.KartyUrl,
                BucketType.KartyAssets);
        }

        return Ok(new ApiResponse<GetArticlePracticeTaskResponse> { Data = response });
    }

    [HttpPost("answer")]
    public async Task<ActionResult<ApiResponse<AnswerArticlePracticeResponse>>> Answer(
        [FromBody] AnswerArticlePracticeRequest request)
    {
        var response = await _mediator.Send(request);
        return Ok(new ApiResponse<AnswerArticlePracticeResponse> { Data = response });
    }
}
