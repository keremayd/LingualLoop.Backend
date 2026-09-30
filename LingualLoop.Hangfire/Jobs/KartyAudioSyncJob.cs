using AwsService.Abstractions;
using Common.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Postgres;
using Postgres.Abstractions;
using Postgres.Models;
using Service.Helpers;

namespace LingualLoop.Hangfire.Jobs;

/// <summary>
/// Sesi olmayan Karty kartları için telaffuz üretir: Polly ile seslendirir,
/// S3'e yükler, anahtarı satıra yazar.
///
/// **Neden Hangfire, neden elle çalıştırılan bir betik değil:** içerik
/// tarafına yeni kelime eklendiğinde sesin kendiliğinden üretilmesi gerekiyor.
/// Elle çalıştırılan bir betik olsaydı eklenen her kelime sessiz kalır ve
/// bunu kimse fark etmezdi — kart görünür, yalnız telaffuz butonu çıkmaz.
///
/// İş **parti parti** çalışıyor: tek turda tüm havuzu üretmeye kalkmak hem
/// Polly hız sınırına takılır hem de bir hata bütün turu çöpe atardı. Her
/// tur küçük bir dilim alıp yazıyor; yarıda kalırsa sonraki tur kaldığı
/// yerden devam eder çünkü ölçüt "anahtarı boş olanlar".
/// </summary>
public class KartyAudioSyncJob
{
    /// <summary>
    /// Tur başına üretilecek ses sayısı. Polly neural motorunun eşzamanlılık
    /// sınırı var; ayrıca küçük partiler bir hatanın etkisini küçük tutuyor.
    /// </summary>
    private const int BatchSize = 25;

    private readonly LingualLoopContext _context;
    private readonly ISpeechService _speechService;
    private readonly IAwsService _awsService;
    private readonly ILogger<KartyAudioSyncJob> _logger;

    public KartyAudioSyncJob(
        ILingualLoopGenericRepository<Karty> kartyRepository,
        ISpeechService speechService,
        IAwsService awsService,
        ILogger<KartyAudioSyncJob> logger)
    {
        _context = kartyRepository.GetDbContext();
        _speechService = speechService;
        _awsService = awsService;
        _logger = logger;
    }

    public async Task Execute()
    {
        var cancellationToken = CancellationToken.None;
        await KartyAudioSchemaHelper.EnsureCreatedAsync(_context, cancellationToken);
        // Eski adlandırmayla yazılmış yolları yalnız **buradan** temizliyoruz;
        // okuma yolu veri değiştirmemeli.
        await KartyAudioSchemaHelper.NormalizeLegacyPathsAsync(
            _context, cancellationToken);

        var pending = await _context.Karty
            .Where(karty => karty.AudioUrl == null && karty.NounText != "")
            .OrderBy(karty => karty.KartyId)
            .Take(BatchSize)
            .ToListAsync(cancellationToken);

        if (pending.Count == 0) return;

        foreach (var karty in pending)
        {
            var text = KartyAudioRules.TextFor(karty.Article, karty.NounText);
            if (string.IsNullOrWhiteSpace(text)) continue;

            try
            {
                await using var audio = await _speechService.SynthesizeMp3Async(
                    text,
                    KartyAudioRules.LanguageCode,
                    KartyAudioRules.VoiceId,
                    cancellationToken);

                var key = KartyAudioRules.KeyFor(karty.KartyId);
                await _awsService.UploadFileAsync(
                    key,
                    audio,
                    KartyAudioRules.ContentType,
                    BucketType.KartyAssets);

                // Anahtar **yükleme başarılı olduktan sonra** yazılıyor.
                // Önce yazılsaydı ve yükleme düşseydi kart "sesi var" diye
                // işaretlenir, istemci de var olmayan bir dosyayı isterdi.
                karty.AudioUrl = key;
            }
            catch (Exception exception)
            {
                // Tek kartın hatası partiyi durdurmuyor: anahtarı boş kaldığı
                // için sonraki turda yeniden denenecek.
                _logger.LogError(
                    exception,
                    "Karty {KartyId} için telaffuz üretilemedi.",
                    karty.KartyId);
            }
        }

        await _context.SaveChangesAsync(cancellationToken);
    }
}
