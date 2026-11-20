using System.Net;
using System.Security.Cryptography;
using AwsService.Abstractions;
using Common.Enums;
using Common.Exceptions;
using Common.Extensions;
using Common.Options;
using Stelinda.Api.Extensions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Service.DataTransferObjects.Requests.Karty;
using Service.DataTransferObjects.Responses;
using Service.DataTransferObjects.Responses.Karty;

namespace Stelinda.Api.Controllers;

[ApiController]
[Route("api/karty")]
public class KartyController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly IAwsService _amazonService;
    private readonly KartyAdminOptions _adminOptions;
    private readonly IWebHostEnvironment _environment;

    public KartyController(
        IMediator mediator,
        IAwsService amazonService,
        IOptions<KartyAdminOptions> adminOptions,
        IWebHostEnvironment environment)
    {
        _mediator = mediator;
        _amazonService = amazonService;
        _adminOptions = adminOptions.Value;
        _environment = environment;
    }

    /// <summary>
    /// Sıradaki kart. <paramref name="unit"/> ve <paramref name="deck"/>
    /// oyunun adımı (patikadaki şu anki deste; deste yoksa yalnız tekrar):
    /// yeni kelime yalnız o desteden gelir. <paramref name="reviews"/> oyunda
    /// şimdiye kadar verilen tekrar kartı sayısı; tekrarlar oyunun payı kadar,
    /// yeni kelimelerle karışık gelir (<c>KartyScheduler</c>).
    /// </summary>
    [Authorize]
    [HttpGet("random/{userId}")]
    public async Task<ActionResult<ApiResponse<GetKartyByScoreResponse>>> GetRandomQuestionByUserId(
        [FromRoute] string userId,
        [FromQuery] int? unit,
        [FromQuery] int? deck,
        [FromQuery] int? reviews)
    {
        var kartyQuestion = await _mediator.Send(new GetKartyByScoreRequest()
        {
            UserId = userId,
            UnitSequence = unit,
            Deck = deck,
            Reviews = reviews,
            Locale = Request.NativeLanguage(),
        });

        // Yanıt **yeniden kurulmuyor**, yalnız imzalı URL yazılıyor.
        //
        // Eskiden burada alan alan yeni bir nesne oluşturuluyordu; handler'a
        // eklenen `Mode` alanı kopyalanmadığı için istemciye hep varsayılan
        // değer gidiyordu ve tanışma kartı hiç görünmüyordu. Alan kopyalayan
        // her controller aynı sessiz hatayı üretmeye adaydır.
        SignUrls(kartyQuestion);

        return Ok(new ApiResponse<GetKartyByScoreResponse>()
        {
            Data = kartyQuestion
        });
    }

    private void SignUrls(GetKartyByScoreResponse question)
    {
        question.KartyUrl =
            _amazonService.GeneratePreSignedUrl(question.KartyUrl, BucketType.KartyAssets);
        // Ses üretilmemiş kartta anahtar null; imzalanacak bir şey yok ve
        // istemci butonu göstermiyor.
        question.AudioUrl = string.IsNullOrEmpty(question.AudioUrl)
            ? null
            : _amazonService.GeneratePreSignedUrl(question.AudioUrl, BucketType.KartyAssets);
        foreach (var option in question.Options)
        {
            option.KartyUrl =
                _amazonService.GeneratePreSignedUrl(option.KartyUrl, BucketType.KartyAssets);
        }
    }

    /// <summary>
    /// Karty Yolu: üniteler ve desteleri (bitmiş, şu anki, sıradaki,
    /// kilitli). Yalnız token sahibi kendi yolunu okuyabilir.
    /// </summary>
    [Authorize]
    [HttpGet("path/{userId}")]
    public async Task<ActionResult<ApiResponse<GetKartyPathResponse>>> GetPath([FromRoute] string userId)
    {
        if (!string.Equals(User.Identity?.Name, userId, StringComparison.Ordinal))
            return Forbid();

        var response = await _mediator.Send(new GetKartyPathRequest
        {
            UserId = userId,
            Locale = Request.NativeLanguage(),
        });

        string Sign(string key) =>
            _amazonService.GeneratePreSignedUrl(key, BucketType.KartyAssets);

        foreach (var unit in response.Units)
        {
            if (!string.IsNullOrEmpty(unit.EmblemUrl)) unit.EmblemUrl = Sign(unit.EmblemUrl);
            foreach (var step in unit.Steps)
            {
                foreach (var word in step.Words)
                {
                    if (!string.IsNullOrEmpty(word.KartyUrl)) word.KartyUrl = Sign(word.KartyUrl);
                }
                step.UpcomingImageUrls = step.UpcomingImageUrls
                    .Where(key => !string.IsNullOrEmpty(key))
                    .Select(Sign)
                    .ToList();
            }
        }

        return Ok(new ApiResponse<GetKartyPathResponse> { Data = response });
    }

    /// <summary>
    /// Oyunun durumu: destesi bitti mi, tekrar payı doldu mu
    /// (<paramref name="reviews"/> = oyunda cevaplanan tekrar). Oyun her
    /// cevaptan sonra sorar; bitince özeti gösterir. Yalnız token sahibi.
    /// </summary>
    [Authorize]
    [HttpGet("session/{userId}")]
    public async Task<ActionResult<ApiResponse<GetKartySessionStatusResponse>>> GetSessionStatus(
        [FromRoute] string userId,
        [FromQuery] int unit,
        [FromQuery] int? deck,
        [FromQuery] int reviews = 0)
    {
        if (!string.Equals(User.Identity?.Name, userId, StringComparison.Ordinal))
            return Forbid();

        var response = await _mediator.Send(new GetKartySessionStatusRequest
        {
            UserId = userId,
            UnitSequence = unit,
            Deck = deck,
            ReviewsAnswered = Math.Max(0, reviews),
            Locale = Request.NativeLanguage(),
        });
        foreach (var word in response.Words)
        {
            if (!string.IsNullOrEmpty(word.KartyUrl))
                word.KartyUrl = _amazonService.GeneratePreSignedUrl(word.KartyUrl, BucketType.KartyAssets);
        }

        return Ok(new ApiResponse<GetKartySessionStatusResponse> { Data = response });
    }

    /// <summary>
    /// Ünite açılış kutlaması görüldü; patika onu bir daha göstermez. Yalnız
    /// token sahibi.
    /// </summary>
    [Authorize]
    [HttpPost("path/{userId}/unlock/acknowledge")]
    public async Task<ActionResult<ApiResponse<AcknowledgeKartyUnitUnlockResponse>>> AcknowledgeUnitUnlock(
        [FromRoute] string userId)
    {
        if (!string.Equals(User.Identity?.Name, userId, StringComparison.Ordinal))
            return Forbid();

        var response = await _mediator.Send(new AcknowledgeKartyUnitUnlockRequest { UserId = userId });
        return Ok(new ApiResponse<AcknowledgeKartyUnitUnlockResponse> { Data = response });
    }

    [Authorize]
    [HttpGet("wrong/random/{userId}")]
    public async Task<ActionResult<ApiResponse<GetKartyByScoreResponse>>> GetRandomWrongKartyByUserId([FromRoute] string userId)
    {
        var kartyQuestion = await _mediator.Send(new GetWrongKartyReviewRequest
        {
            UserId = userId,
            Locale = Request.NativeLanguage(),
        });

        // Yukarıdaki uçla aynı gerekçe: alan alan kopyalamak yerine dönen
        // nesne iletiliyor. Rövanşta tanışma modu zaten oluşmaz (kullanıcı o
        // kelimeyi görmüş ve yanlış bilmiş) ama alan kopyalayan kod ileride
        // eklenen her alanı sessizce düşürür.
        SignUrls(kartyQuestion);

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
            throw new StelindaException(
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
                UnitSequence = form.UnitSequence,
                UnitOrder = form.UnitOrder,
                OverwriteExisting = form.OverwriteExisting,
                Meanings = form.Meanings ?? new(),
                ImageFileName = form.Image.FileName,
                ImageContent = buffer.ToArray(),
            },
            cancellationToken);

        return Ok(new ApiResponse<CreateKartyResponse> { Data = response });
    }

    /// <summary>
    /// Karty Yolu ünitesini açar ya da adlarını (dile göre, <c>titles</c>) /
    /// başlık görselini değiştirir.
    /// Başlık görseli ünitenin kartlarından birinin görseli
    /// (<c>emblemKartyId</c>); ayrı yükleme yok. Kimlik: `X-Karty-Admin-Key`.
    /// </summary>
    [AllowAnonymous]
    [HttpPut("admin/units/{sequence:int}")]
    public async Task<ActionResult<ApiResponse<UpsertKartyUnitResponse>>> UpsertUnit(
        [FromRoute] int sequence,
        [FromBody] UpsertKartyUnitBody body,
        CancellationToken cancellationToken)
    {
        EnsureAdminKey();

        // Ad 16 Kas 2026'dan beri yalnız dile göre geliyor. Eski "title"
        // alanı yok sayılsaydı yeniden adlandırma sessizce hiçbir şey yapmazdı.
        if (body.Title is not null)
        {
            const string problem =
                "'title' alanı kalktı; ad dile göre verilir: {\"titles\": {\"tr\": \"Evde\"}}";
            throw new StelindaException(
                ErrorCode.KartyPayloadInvalid.CreateMessage(problem),
                ErrorCode.KartyPayloadInvalid.GetDescription(problem),
                HttpStatusCode.BadRequest);
        }

        var response = await _mediator.Send(
            new UpsertKartyUnitRequest
            {
                Sequence = sequence,
                EmblemKartyId = body.EmblemKartyId,
                Titles = body.Titles ?? new(),
            },
            cancellationToken);

        return Ok(new ApiResponse<UpsertKartyUnitResponse> { Data = response });
    }

    /// <summary>
    /// İçerik girişi: bir dildeki kart anlamlarını toplu yazar. Gövde
    /// <c>{"Tür": "derë", "Fenster": "dritare"}</c>: anahtar kartın Almanca adı,
    /// boş değer o dildeki anlamı siler. Kimlik: `X-Karty-Admin-Key`.
    /// </summary>
    [AllowAnonymous]
    [HttpPut("admin/meanings/{locale}")]
    public async Task<ActionResult<ApiResponse<UpsertKartyMeaningsResponse>>> UpsertMeanings(
        [FromRoute] string locale,
        [FromBody] Dictionary<string, string?> meanings,
        CancellationToken cancellationToken)
    {
        EnsureAdminKey();

        var response = await _mediator.Send(
            new UpsertKartyMeaningsRequest { Locale = locale, Meanings = meanings },
            cancellationToken);

        return Ok(new ApiResponse<UpsertKartyMeaningsResponse> { Data = response });
    }

    /// <summary>
    /// İçerik girişi: bütün kartlar ve bir dildeki anlamları, patika sırasıyla;
    /// anlamı olmayanda <c>null</c>. Çeviriyi kontrole göndermek ve eksikleri
    /// görmek için. Kimlik: `X-Karty-Admin-Key`.
    /// </summary>
    [AllowAnonymous]
    [HttpGet("admin/meanings/{locale}")]
    public async Task<ActionResult<ApiResponse<GetKartyMeaningsResponse>>> GetMeanings(
        [FromRoute] string locale,
        CancellationToken cancellationToken)
    {
        EnsureAdminKey();

        var response = await _mediator.Send(
            new GetKartyMeaningsRequest { Locale = locale },
            cancellationToken);

        return Ok(new ApiResponse<GetKartyMeaningsResponse> { Data = response });
    }

    /// <summary>
    /// İçerik girişi: yanlış ya da hatalı üretilmiş kartı satırı ve S3'teki
    /// görsel + telaffuzuyla kalıcı olarak siler. Kullanıcı kaydı olan kart
    /// (biri öğrenmiş ya da cevaplamış) yalnız <c>?force=true</c> ile silinir;
    /// o kayıtlar da silinir. Kimlik: `X-Karty-Admin-Key`.
    ///
    /// **Yalnız Development'ta açık, yayından önce kaldırılacak** (CLAUDE.md
    /// §8 "Yayın öncesi yapılacaklar"). Başka ortamda uç yokmuş gibi gövdesiz
    /// 404 döner; anahtar sızsa bile canlıda kart silinemez.
    /// </summary>
    [AllowAnonymous]
    [HttpDelete("admin/{kartyId:int}")]
    public async Task<ActionResult<ApiResponse<DeleteKartyResponse>>> DeleteKarty(
        [FromRoute] int kartyId,
        [FromQuery] bool force,
        CancellationToken cancellationToken)
    {
        if (!_environment.IsDevelopment()) return NotFound();

        EnsureAdminKey();

        var response = await _mediator.Send(
            new DeleteKartyRequest { KartyId = kartyId, Force = force },
            cancellationToken);

        return Ok(new ApiResponse<DeleteKartyResponse> { Data = response });
    }

    /// <summary>
    /// Anahtar yapılandırmada yoksa uç **kapalıdır**. Fail-open olsaydı
    /// yapılandırmayı unutmak ucu herkese açardı.
    /// </summary>
    private void EnsureAdminKey()
    {
        if (string.IsNullOrWhiteSpace(_adminOptions.ApiKey))
        {
            throw new StelindaException(
                ErrorCode.KartyAdminKeyInvalid.CreateMessage(),
                "KartyAdmin:ApiKey yapılandırılmamış; içerik girişi ucu kapalı.",
                HttpStatusCode.ServiceUnavailable);
        }

        var provided = Request.Headers["X-Karty-Admin-Key"].ToString();
        if (!CryptographicOperations.FixedTimeEquals(
                System.Text.Encoding.UTF8.GetBytes(provided),
                System.Text.Encoding.UTF8.GetBytes(_adminOptions.ApiKey)))
        {
            throw new StelindaException(
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
    public int? UnitSequence { get; set; }
    public int? UnitOrder { get; set; }
    public bool OverwriteExisting { get; set; }
    public IFormFile? Image { get; set; }

    /// <summary>
    /// Ana dillerdeki anlam; form alanı olarak <c>meanings[tr]=kapı</c>,
    /// <c>meanings[sq]=derë</c>. İsteğe bağlı.
    /// </summary>
    public Dictionary<string, string?>? Meanings { get; set; }
}

/// <summary>Ünite açma / yeniden adlandırma gövdesi.</summary>
public class UpsertKartyUnitBody
{
    public int? EmblemKartyId { get; set; }

    /// <summary>Dillerdeki ad: <c>{"tr": "Evde", "sq": "Në shtëpi"}</c>.</summary>
    public Dictionary<string, string?>? Titles { get; set; }

    /// <summary>Kalktı (16 Kas 2026); gönderilirse 400 döner, bkz. <see cref="Titles"/>.</summary>
    public string? Title { get; set; }
}
