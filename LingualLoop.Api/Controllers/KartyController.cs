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
using Service.DataTransferObjects.Responses.Karty;

namespace LingualLoop.Api.Controllers;

[ApiController]
[Route("ll-api/karty")]
public class KartyController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly IAwsService _amazonService;

    public KartyController(IMediator mediator, IAwsService amazonService)
    {
        _mediator = mediator;
        _amazonService = amazonService;
    }

    [Authorize]
    [HttpGet("random/{userId}")]
    public async Task<ActionResult<ApiResponse<GetKartyByScoreResponse>>> GetRandomQuestionByUserId([FromRoute] string userId)
    {
        var userScoreResponse = await _mediator.Send(new GetScoreByIdRequest() { UserId = userId });

        var kartyQuestion = await _mediator.Send(new GetKartyByScoreRequest() { UserScore = userScoreResponse.Score, UserId = userScoreResponse.UserId });
        
        var signedUrl = _amazonService.GeneratePreSignedUrl(kartyQuestion.KartyUrl, BucketType.KartyAssets);
        
        return Ok(new ApiResponse<GetKartyByScoreResponse>()
        {
            Data = new GetKartyByScoreResponse()
            {
                KartyId = kartyQuestion.KartyId,
                QuestionText = kartyQuestion.QuestionText,
                CorrectText = kartyQuestion.CorrectText,
                KartyUrl = signedUrl,
                IsCorrect = kartyQuestion.IsCorrect,
                MinScore = kartyQuestion.MinScore,
                MaxScore = kartyQuestion.MaxScore
            }
        });
    }
}