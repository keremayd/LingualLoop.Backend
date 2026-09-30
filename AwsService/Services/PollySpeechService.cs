using Amazon.Polly;
using Amazon.Polly.Model;
using AwsService.Abstractions;

namespace AwsService.Services;

/// <summary>
/// <see cref="ISpeechService"/>'in Amazon Polly uygulaması.
///
/// Polly seçildi çünkü uygulama zaten AWS'te: aynı kimlik bilgisi, aynı
/// bölge, aynı SDK ailesi. Ayrı bir sağlayıcı (ElevenLabs, Azure) tek
/// kelimelik telaffuzda duyulur bir fark yaratmadan ikinci bir fatura ve
/// ikinci bir kimlik yönetimi getirirdi.
///
/// Gereken IAM izni yalnızca <c>polly:SynthesizeSpeech</c>
/// (`AmazonPollyReadOnlyAccess` bunu içerir).
/// </summary>
public class PollySpeechService : ISpeechService
{
    private readonly IAmazonPolly _polly;

    public PollySpeechService(IAmazonPolly polly)
    {
        _polly = polly;
    }

    public async Task<Stream> SynthesizeMp3Async(
        string text,
        string languageCode,
        string voiceId,
        CancellationToken cancellationToken = default)
    {
        var response = await _polly.SynthesizeSpeechAsync(
            new SynthesizeSpeechRequest
            {
                Text = text,
                TextType = TextType.Text,
                OutputFormat = OutputFormat.Mp3,
                // **Neural** zorunlu. Standard motor Almanca'da belirgin
                // biçimde robotik; tek kelimelik telaffuzda fark hemen
                // duyuluyor ve bu özelliğin bütün amacı telaffuz.
                Engine = Engine.Neural,
                LanguageCode = languageCode,
                VoiceId = voiceId,
                // 24 kHz tek kelime için fazlasıyla yeterli ve dosyayı
                // küçük tutuyor (~10-15 KB).
                SampleRate = "24000",
            },
            cancellationToken);

        // Polly akışı yanıt nesnesine bağlı; yanıt dispose edilince kapanır.
        // Bu yüzden belleğe kopyalanıyor — dosyalar birkaç KB.
        var buffer = new MemoryStream();
        await response.AudioStream.CopyToAsync(buffer, cancellationToken);
        buffer.Position = 0;
        return buffer;
    }
}
