namespace AwsService.Abstractions;

/// <summary>
/// Metinden konuşma üretir. Depolamadan **ayrı** bir sorumluluk: ses üretmek
/// ile dosya saklamak farklı işler, farklı sağlayıcılarla değiştirilebilir
/// olmalı. Bugün Polly kullanılıyor; yarın başka bir motora geçilirse
/// dokunulacak tek yer bu arayüzün uygulaması olur.
/// </summary>
public interface ISpeechService
{
    /// <summary>
    /// Verilen metni seslendirip **MP3** akışı döner. Akışın sahibi çağıran
    /// taraftır.
    /// </summary>
    Task<Stream> SynthesizeMp3Async(
        string text,
        string languageCode,
        string voiceId,
        CancellationToken cancellationToken = default);
}
