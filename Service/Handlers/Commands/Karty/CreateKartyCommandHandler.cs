using System.Net;
using AwsService.Abstractions;
using Common.Enums;
using Common.Exceptions;
using Common.Extensions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Postgres;
using Postgres.Abstractions;
using Service.DataTransferObjects.Requests.Karty;
using Service.DataTransferObjects.Responses.Karty;
using Service.Helpers;
using KartyModel = Postgres.Models.Karty;

namespace Service.Handlers.Commands.Karty;

/// <summary>
/// Tek çağrıda tam bir kart üretir: satır → görsel → telaffuz.
///
/// ## Sıra neden bu
///
/// S3 anahtarları `karty_id`'yi içeriyor (<see cref="KartyAssetRules"/>), ama
/// id'yi veritabanı üretiyor. Yani dosya adını bilmek için satırın **önce**
/// açılması gerekiyor. Bu, arada bir adım düşerse yarım kayıt bırakma riski
/// doğuruyor; iki farklı biçimde ele alınıyor:
///
/// | Adım düşerse | Davranış | Neden |
/// |---|---|---|
/// | Görsel yükleme | Satır **silinir**, hata fırlatılır | Görselsiz kart oynanamaz: oyun kartın resmini gösteriyor. Yarım satır bırakmak havuzu bozar. |
/// | Ses üretimi | Satır **kalır**, uyarı döner | Sessiz kart oynanabilir, yalnız telaffuz butonu çıkmaz. Üstelik `KartyAudioSyncJob` ölçütü "anahtarı boş olanlar" olduğu için kendiliğinden tamamlıyor. |
///
/// Yani ayrım keyfi değil: **kartı kullanılamaz kılan** eksik geri alınıyor,
/// **kendiliğinden kapanan** eksik uyarıya çevriliyor.
///
/// ## Yazım sorusu burada üretilmiyor
///
/// `question_text` / `correct_text` / `is_correct` kolonları duruyor ama
/// okunmuyor: soru her elde <see cref="KartySpellingChallengeBuilder"/> ile
/// `noun_text`'ten türetiliyor. Bu yüzden kart oluşturmak için gereken tek
/// dilbilgisi verisi **isim + artikel**. Kolonlara sabit değer yazılıyor ki
/// eski satırlarla aynı biçimde kalsınlar.
/// </summary>
public class CreateKartyCommandHandler
    : IRequestHandler<CreateKartyRequest, CreateKartyResponse>
{
    private const int MaxImageBytes = 8 * 1024 * 1024;

    private static readonly HashSet<string> AllowedArticles =
        new(StringComparer.OrdinalIgnoreCase) { "der", "die", "das" };

    private readonly LingualLoopContext _context;
    private readonly IAwsService _awsService;
    private readonly ISpeechService _speechService;

    public CreateKartyCommandHandler(
        ILingualLoopGenericRepository<KartyModel> kartyRepository,
        IAwsService awsService,
        ISpeechService speechService)
    {
        _context = kartyRepository.GetDbContext();
        _awsService = awsService;
        _speechService = speechService;
    }

    public async Task<CreateKartyResponse> Handle(
        CreateKartyRequest request,
        CancellationToken cancellationToken)
    {
        await KartyAudioSchemaHelper.EnsureCreatedAsync(_context, cancellationToken);

        var nounText = (request.NounText ?? string.Empty).Trim();
        var article = (request.Article ?? string.Empty).Trim().ToLowerInvariant();
        var extension = Path.GetExtension(request.ImageFileName ?? string.Empty);

        Validate(request, nounText, article, extension);

        var existing = await _context.Karty
            .FirstOrDefaultAsync(
                item => item.NounText.ToLower() == nounText.ToLower(),
                cancellationToken);

        if (existing is not null && !request.OverwriteExisting)
        {
            throw new LingualLoopException(
                ErrorCode.KartyAlreadyExists.CreateMessage(nounText),
                ErrorCode.KartyAlreadyExists.GetDescription(nounText),
                HttpStatusCode.Conflict);
        }

        var isUpdate = existing is not null;
        var karty = existing ?? new KartyModel { CreatedDate = DateTime.UtcNow };

        karty.NounText = nounText;
        karty.Article = article;
        karty.MinScore = request.MinScore;
        karty.MaxScore = request.MaxScore;
        // Okunmayan ama şemada duran kolonlar; eski satırlarla aynı biçim.
        karty.QuestionText = nounText;
        karty.CorrectText = nounText;
        karty.IsCorrect = true;

        if (!isUpdate)
        {
            // Anahtarlar id'ye bağlı olduğu için satır **önce** açılıyor.
            _context.Karty.Add(karty);
            await _context.SaveChangesAsync(cancellationToken);
        }

        var imageKey = KartyAssetRules.ImageKeyFor(karty.KartyId, extension);

        try
        {
            await using var imageStream = new MemoryStream(request.ImageContent);
            await _awsService.UploadFileAsync(
                imageKey,
                imageStream,
                KartyAssetRules.ContentTypeForImage(extension),
                BucketType.KartyAssets);
        }
        catch (Exception exception)
        {
            if (!isUpdate)
            {
                // Yeni açılan satır geri alınıyor: görselsiz kart oynanamaz.
                // Güncellemede satır silinmiyor — eski görsel yerinde duruyor
                // ve kart çalışmaya devam ediyor.
                _context.Karty.Remove(karty);
                await _context.SaveChangesAsync(cancellationToken);
            }

            throw new LingualLoopException(
                ErrorCode.KartyAssetUploadFailed.CreateMessage(exception.Message),
                ErrorCode.KartyAssetUploadFailed.GetDescription(exception.Message),
                HttpStatusCode.BadGateway);
        }

        karty.KartyUrl = imageKey;

        var response = new CreateKartyResponse
        {
            KartyId = karty.KartyId,
            NounText = nounText,
            Article = article,
            MinScore = request.MinScore,
            MaxScore = request.MaxScore,
            KartyUrl = imageKey,
            Updated = isUpdate,
        };

        await SynthesizeAudioAsync(karty, response, cancellationToken);

        await _context.SaveChangesAsync(cancellationToken);

        response.AudioUrl = karty.AudioUrl;
        response.ImagePreviewUrl =
            _awsService.GeneratePreSignedUrl(imageKey, BucketType.KartyAssets);
        response.AudioPreviewUrl = string.IsNullOrEmpty(karty.AudioUrl)
            ? null
            : _awsService.GeneratePreSignedUrl(karty.AudioUrl, BucketType.KartyAssets);

        return response;
    }

    /// <summary>
    /// Telaffuzu üretir. Düşerse **hata fırlatmaz**: anahtar boş kalır ve
    /// <c>KartyAudioSyncJob</c> sonraki turunda tamamlar. Sessiz kalmasın
    /// diye sebep yanıttaki uyarıya yazılıyor.
    /// </summary>
    private async Task SynthesizeAudioAsync(
        KartyModel karty,
        CreateKartyResponse response,
        CancellationToken cancellationToken)
    {
        var text = KartyAudioRules.TextFor(karty.Article, karty.NounText);
        if (string.IsNullOrWhiteSpace(text))
        {
            response.Warning = "Seslendirilecek metin boş; telaffuz üretilmedi.";
            return;
        }

        try
        {
            await using var audio = await _speechService.SynthesizeMp3Async(
                text,
                KartyAudioRules.LanguageCode,
                KartyAudioRules.VoiceId,
                cancellationToken);

            var audioKey = KartyAudioRules.KeyFor(karty.KartyId);
            await _awsService.UploadFileAsync(
                audioKey,
                audio,
                KartyAudioRules.ContentType,
                BucketType.KartyAssets);

            // Anahtar **yükleme başarılı olduktan sonra** yazılıyor; önce
            // yazılsaydı kart "sesi var" diye işaretlenir ve istemci var
            // olmayan bir dosyayı isterdi.
            karty.AudioUrl = audioKey;
            response.AudioGenerated = true;
        }
        catch (Exception exception)
        {
            karty.AudioUrl = null;
            response.AudioGenerated = false;
            response.Warning =
                $"Telaffuz üretilemedi ({exception.Message}). Kart kullanılabilir; " +
                "Hangfire'daki KartyAudioSyncJob sonraki turunda tamamlayacak.";
        }
    }

    private static void Validate(
        CreateKartyRequest request,
        string nounText,
        string article,
        string extension)
    {
        string? problem = null;

        if (nounText.Length == 0)
            problem = "nounText boş olamaz.";
        else if (!AllowedArticles.Contains(article))
            problem = "article yalnız der/die/das olabilir.";
        else if (request.ImageContent.Length == 0)
            problem = "Görsel boş.";
        else if (request.ImageContent.Length > MaxImageBytes)
            problem = $"Görsel {MaxImageBytes / (1024 * 1024)} MB sınırını aşıyor.";
        else if (!KartyAssetRules.IsAllowedImageExtension(extension))
            problem = $"Desteklenmeyen görsel türü: '{extension}'. " +
                      $"İzin verilenler: {string.Join(", ", KartyAssetRules.AllowedImageTypes.Keys)}";
        else if (request.MinScore < 0 || request.MaxScore < request.MinScore)
            problem = "Skor bandı geçersiz (minScore >= 0 ve maxScore >= minScore olmalı).";

        if (problem is null) return;

        throw new LingualLoopException(
            ErrorCode.KartyPayloadInvalid.CreateMessage(problem),
            ErrorCode.KartyPayloadInvalid.GetDescription(problem),
            HttpStatusCode.BadRequest);
    }
}
