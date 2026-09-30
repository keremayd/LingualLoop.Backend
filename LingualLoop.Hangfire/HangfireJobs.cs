using Hangfire;
using LingualLoop.Hangfire.Jobs;

namespace LingualLoop.Hangfire;

public class HangfireJobs
{
    public static void ConfigureJobs()
    {
        // Yenilenme aralığı 2 saat; beş dakikada bir yoklamak en fazla beş
        // dakikalık gecikme demektir ve dakikalık taramanın on iki katı
        // sorgusunu ortadan kaldırır.
        RecurringJob.AddOrUpdate<UpdateLivesSyncJob>(nameof(UpdateLivesSyncJob), x => x.Execute(), "*/5 * * * *");

        // Telaffuz üretimi: içerik tarafına yeni kelime eklendiğinde sesin
        // kendiliğinden oluşması için. Saatte bir yeterli — yeni kelime
        // eklemek nadir bir olay ve kartın sesi bir saat gecikmeli gelmesi
        // sorun değil, kart o sürede de oynanabiliyor.
        RecurringJob.AddOrUpdate<KartyAudioSyncJob>(nameof(KartyAudioSyncJob), x => x.Execute(), "0 * * * *");
    }
}