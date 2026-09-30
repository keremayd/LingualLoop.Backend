using System.Net;
using System.Security.Cryptography;
using AwsService.Abstractions;
using Common.Enums;
using Common.Exceptions;
using Common.Extensions;
using Common.Options;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
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
    private readonly KartyAdminOptions _adminOptions;

    public KartyController(
        IMediator mediator,
        IAwsService amazonService,
        IOptions<KartyAdminOptions> adminOptions)
    {
        _mediator = mediator;
        _amazonService = amazonService;
        _adminOptions = adminOptions.Value;
    }

    [Authorize]
    [HttpGet("random/{userId}")]
    public async Task<ActionResult<ApiResponse<GetKartyByScoreResponse>>> GetRandomQuestionByUserId([FromRoute] string userId)
    {
        var userScoreResponse = await _mediator.Send(new GetScoreByIdRequest() { UserId = userId });

        var kartyQuestion = await _mediator.Send(new GetKartyByScoreRequest() { UserScore = userScoreResponse.Score, UserId = userScoreResponse.UserId });
        
        // Yanıt **yeniden kurulmuyor**, yalnız imzalı URL yazılıyor.
        //
        // Eskiden burada alan alan yeni bir nesne oluşturuluyordu; handler'a
        // eklenen `Mode` alanı kopyalanmadığı için istemciye hep varsayılan
        // değer gidiyordu ve tanışma kartı hiç görünmüyordu. Alan kopyalayan
        // her controller aynı sessiz hatayı üretmeye adaydır.
        kartyQuestion.KartyUrl =
            _amazonService.GeneratePreSignedUrl(kartyQuestion.KartyUrl, BucketType.KartyAssets);
        // Ses üretilmemiş kartta anahtar null; imzalanacak bir şey yok ve
        // istemci butonu göstermiyor.
        kartyQuestion.AudioUrl = string.IsNullOrEmpty(kartyQuestion.AudioUrl)
            ? null
            : _amazonService.GeneratePreSignedUrl(kartyQuestion.AudioUrl, BucketType.KartyAssets);

        return Ok(new ApiResponse<GetKartyByScoreResponse>()
        {
            Data = kartyQuestion
        });
    }

    [Authorize]
    [HttpGet("wrong/random/{userId}")]
    public async Task<ActionResult<ApiResponse<GetKartyByScoreResponse>>> GetRandomWrongKartyByUserId([FromRoute] string userId)
    {
        var kartyQuestion = await _mediator.Send(new GetWrongKartyReviewRequest { UserId = userId });

        // Yukarıdaki uçla aynı gerekçe: alan alan kopyalamak yerine dönen
        // nesne iletiliyor. Rövanşta tanışma modu zaten oluşmaz (kullanıcı o
        // kelimeyi görmüş ve yanlış bilmiş) ama alan kopyalayan kod ileride
        // eklenen her alanı sessizce düşürür.
        kartyQuestion.KartyUrl =
            _amazonService.GeneratePreSignedUrl(kartyQuestion.KartyUrl, BucketType.KartyAssets);
        // Ses üretilmemiş kartta anahtar null; imzalanacak bir şey yok ve
        // istemci butonu göstermiyor.
        kartyQuestion.AudioUrl = string.IsNullOrEmpty(kartyQuestion.AudioUrl)
            ? null
            : _amazonService.GeneratePreSignedUrl(kartyQuestion.AudioUrl, BucketType.KartyAssets);

        return Ok(new ApiResponse<GetKartyByScoreResponse>()
        {
            Data = kartyQuestion
        });
    }

    [Authorize]
    [HttpPost("wrong")]
    public async Task<ActionResult<ApiResponse<RecordWrongKartyResponse>>> RecordWrongKarty([FromBody] RecordWrongKartyRequest request)
    {
        var response = await _mediator.Send(request);

        return Ok(new ApiResponse<RecordWrongKartyResponse>()
        {
            Data = response
        });
    }

    [Authorize]
    [HttpPost("learned")]
    public async Task<ActionResult<ApiResponse<RecordLearnedKartyResponse>>> RecordLearnedKarty(
        [FromBody] RecordLearnedKartyRequest request)
    {
        var response = await _mediator.Send(request);
        return Ok(new ApiResponse<RecordLearnedKartyResponse> { Data = response });
    }

    /// <summary>
    /// Tanışma kartı onaylandı. Kelime bundan sonra yazım sorusu olarak
    /// gelmeye başlar.
    /// </summary>
    [Authorize]
    [HttpPost("introduced")]
    public async Task<ActionResult<ApiResponse<RecordKartyIntroductionResponse>>> RecordKartyIntroduction(
        [FromBody] RecordKartyIntroductionRequest request)
    {
        var response = await _mediator.Send(request);
        return Ok(new ApiResponse<RecordKartyIntroductionResponse> { Data = response });
    }

    [Authorize]
    [HttpPost("wrong/review")]
    public async Task<ActionResult<ApiResponse<ResolveWrongKartyReviewResponse>>> ResolveWrongKartyReview([FromBody] ResolveWrongKartyReviewRequest request)
    {
        var response = await _mediator.Send(request);

        return Ok(new ApiResponse<ResolveWrongKartyReviewResponse>()
        {
            Data = response
        });
    }

    /// <summary>
    /// İçerik girişi: **tek çağrıda** kart satırı, S3 görseli ve telaffuz.
    ///
    /// `multipart/form-data` çünkü aynı istekte hem dosya hem alan taşınıyor;
    /// görseli base64'e çevirip JSON'a gömmek yükü ~%33 büyütür ve elle
    /// çağrılabilirliği (curl) bozar.
    ///
    /// Kimlik: JWT **değil**, `X-Karty-Admin-Key` başlığı. Gerekçe
    /// <see cref="KartyAdminOptions"/> içinde.
    /// </summary>
    [AllowAnonymous]
    [HttpPost("admin/create")]
    [RequestSizeLimit(16 * 1024 * 1024)]
    public async Task<ActionResult<ApiResponse<CreateKartyResponse>>> CreateKarty(
        [FromForm] CreateKartyForm form,
        CancellationToken cancellationToken)
    {
        EnsureAdminKey();

        if (form.Image is null || form.Image.Length == 0)
        {
            throw new LingualLoopException(
                ErrorCode.KartyPayloadInvalid.CreateMessage("Görsel gönderilmedi."),
                ErrorCode.KartyPayloadInvalid.GetDescription("Görsel gönderilmedi."),
                HttpStatusCode.BadRequest);
        }

        // Dosya burada okunuyor: handler `Service` katmanında ve ASP.NET'i
        // tanımıyor (bkz. CreateKartyRequest).
        using var buffer = new MemoryStream();
        await form.Image.CopyToAsync(buffer, cancellationToken);

        var response = await _mediator.Send(
            new CreateKartyRequest
            {
                NounText = form.NounText,
                Article = form.Article,
                MinScore = form.MinScore,
                MaxScore = form.MaxScore,
                OverwriteExisting = form.OverwriteExisting,
                ImageFileName = form.Image.FileName,
                ImageContent = buffer.ToArray(),
            },
            cancellationToken);

        return Ok(new ApiResponse<CreateKartyResponse> { Data = response });
    }

    /// <summary>
    /// Anahtar yapılandırmada yoksa uç **kapalıdır**. Fail-open olsaydı
    /// yapılandırmayı unutmak ucu herkese açardı.
    /// </summary>
    private void EnsureAdminKey()
    {
        if (string.IsNullOrWhiteSpace(_adminOptions.ApiKey))
        {
            throw new LingualLoopException(
                ErrorCode.KartyAdminKeyInvalid.CreateMessage(),
                "KartyAdmin:ApiKey yapılandırılmamış; içerik girişi ucu kapalı.",
                HttpStatusCode.ServiceUnavailable);
        }

        var provided = Request.Headers["X-Karty-Admin-Key"].ToString();
        if (!CryptographicOperations.FixedTimeEquals(
                System.Text.Encoding.UTF8.GetBytes(provided),
                System.Text.Encoding.UTF8.GetBytes(_adminOptions.ApiKey)))
        {
            throw new LingualLoopException(
                ErrorCode.KartyAdminKeyInvalid.CreateMessage(),
                ErrorCode.KartyAdminKeyInvalid.GetDescription(),
                HttpStatusCode.Unauthorized);
        }
    }
}

/// <summary>
/// `multipart/form-data` gövdesi. `IFormFile` yalnız API katmanında
/// yaşıyor; handler byte dizisi alıyor.
/// </summary>
public class CreateKartyForm
{
    public string NounText { get; set; } = string.Empty;
    public string Article { get; set; } = string.Empty;
    public int MinScore { get; set; }
    public int MaxScore { get; set; } = 100;
    public bool OverwriteExisting { get; set; }
    public IFormFile? Image { get; set; }
}
