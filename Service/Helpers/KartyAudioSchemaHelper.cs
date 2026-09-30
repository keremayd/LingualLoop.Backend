using Microsoft.EntityFrameworkCore;
using Postgres;

namespace Service.Helpers;

/// <summary>
/// <c>karty.audio_url</c> kolonunu garanti eder.
///
/// Migration yerine runtime helper kullanılıyor — depodaki mevcut desen bu
/// (`KartyLearningSchemaHelper`, `UserQuestClaimSchemaHelper`). Kolon
/// **nullable**: sesi henüz üretilmemiş kart normal çalışmaya devam eder,
/// yalnız telaffuz butonu görünmez.
/// </summary>
public static class KartyAudioSchemaHelper
{
    private static bool _ensured;
    private static bool _normalized;

    /// <summary>
    /// Kolonu açar. **Veriye dokunmaz** — idempotent, yalnız şema.
    /// </summary>
    public static async Task EnsureCreatedAsync(
        LingualLoopContext context,
        CancellationToken cancellationToken)
    {
        if (_ensured) return;

        // Kolon bir dönem `audio_key` adıyla açılmıştı. Sakladığı şey bir S3
        // yolu ve istemciye `audioUrl` olarak gidiyor; iki farklı ad aynı
        // veriyi anlatınca hangi katmanda ne olduğu karışıyordu. Var olan
        // kolon yeniden adlandırılıyor, yoksa doğru adla açılıyor.
        await context.Database.ExecuteSqlRawAsync(
            """
            DO $$
            BEGIN
                IF EXISTS (
                    SELECT 1 FROM information_schema.columns
                    WHERE table_name = 'karty' AND column_name = 'audio_key'
                ) AND NOT EXISTS (
                    SELECT 1 FROM information_schema.columns
                    WHERE table_name = 'karty' AND column_name = 'audio_url'
                ) THEN
                    ALTER TABLE karty RENAME COLUMN audio_key TO audio_url;
                END IF;
            END $$;
            """,
            cancellationToken);

        await context.Database.ExecuteSqlRawAsync(
            "ALTER TABLE karty ADD COLUMN IF NOT EXISTS audio_url text;",
            cancellationToken);

        _ensured = true;
    }

    /// <summary>
    /// Güncel adlandırma sözleşmesine uymayan yolları boşaltır ki üretim işi
    /// onları yeniden yazsın.
    ///
    /// **Yalnızca üretim işi çağırmalı.** Bu kod bir dönem
    /// <see cref="EnsureCreatedAsync"/> içindeydi ve o yardımcı okuma
    /// sorgusundan da (`GetKartyByScoreQueryHandler`) çağrılıyordu — yani
    /// kart isteyen bir sorgu, veri **silen** bir göç tetikliyordu. Sonuç:
    /// API her yeniden başladığında yollar siliniyor, Hangfire onları geri
    /// yazana kadar hiçbir kartta telaffuz görünmüyordu. Belirti "ses
    /// gelmiyor"du; sebep okuma yolundaki bir yazmaydı.
    ///
    /// **Kural: okuma yolu veri değiştirmez.** Şema yardımcısı kolon açabilir
    /// (idempotent, veriye dokunmaz); satır güncellemesi yazan tarafın işi.
    ///
    /// Ölçüt "güncel desene uymayan": eski desenleri tek tek saymak, bir
    /// sonraki ad değişikliğinde burayı güncellemeyi unutturur ve eski
    /// yollar sessizce yerinde kalırdı.
    ///
    /// S3'teki eski dosyalar **elle** silinmeli — kod kendi yazmadığı
    /// dosyaları silmez.
    /// </summary>
    public static async Task NormalizeLegacyPathsAsync(
        LingualLoopContext context,
        CancellationToken cancellationToken)
    {
        if (_normalized) return;

        await context.Database.ExecuteSqlRawAsync(
            "UPDATE karty SET audio_url = NULL " +
            "WHERE audio_url IS NOT NULL " +
            "AND audio_url <> 'assets/' || karty_id || '/karty_' || karty_id || '_listening.mp3';",
            cancellationToken);

        _normalized = true;
    }
}
