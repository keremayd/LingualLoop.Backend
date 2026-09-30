using Postgres.Abstractions;
using Postgres;
using Postgres.Models;
using Service.Helpers;

namespace LingualLoop.Hangfire.Jobs;

/// <summary>
/// Bilet yenileme işi: zamanı gelmiş ve tavanın altındaki her kullanıcıya
/// bir bilet ekler. Yenilenme aralığı ve tavan LivesRules'tan okunur, burada
/// ayrı sabit tutulmaz.
/// </summary>
public class UpdateLivesSyncJob
{
    private readonly LingualLoopContext _context;

    public UpdateLivesSyncJob(ILingualLoopGenericRepository<UserLives> userLivesRepository)
    {
        _context = userLivesRepository.GetDbContext();
    }

    public async Task Execute()
    {
        // Aynı atomik hesap kullanıcı sorgusunda da çalışır. İş gecikmişse
        // geçmiş bütün iki saatlik aralıklar tek turda, tavana kadar telafi
        // edilir; kullanıcı sorgusuyla yarışırsa çift bilet oluşmaz.
        await LivesRegeneration.MaterializeAllDueAsync(
            _context,
            DateTime.UtcNow,
            CancellationToken.None);
    }
}
