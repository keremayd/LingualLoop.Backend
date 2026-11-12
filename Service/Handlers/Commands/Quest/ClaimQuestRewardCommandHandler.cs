using MediatR;
using Microsoft.EntityFrameworkCore;
using Postgres;
using Postgres.Abstractions;
using Postgres.Models;
using Service.DataTransferObjects.Requests;
using Service.DataTransferObjects.Responses;
using Service.Helpers;

namespace Service.Handlers.Commands;

public class ClaimQuestRewardCommandHandler : IRequestHandler<ClaimQuestRewardRequest, ClaimQuestRewardResponse>
{
    private readonly LingualLoopContext _context;

    public ClaimQuestRewardCommandHandler(ILingualLoopGenericRepository<User> userRepository)
    {
        _context = userRepository.GetDbContext();
    }

    public async Task<ClaimQuestRewardResponse> Handle(ClaimQuestRewardRequest request, CancellationToken cancellationToken)
    {
        await UserQuestClaimSchemaHelper.EnsureCreatedAsync(_context, cancellationToken);

        var utcNow = DateTime.UtcNow;
        var dayKey = QuestRules.GetDayKey(utcNow);

        var userLives = await UserLivesSeeder.GetOrCreateAsync(
            _context, request.UserId, utcNow, cancellationToken);

        var claimedToday = await _context.UserQuestClaims
            .AsNoTracking()
            .Where(c => c.UserId == request.UserId && c.DayKey == dayKey)
            .Select(c => c.QuestKey)
            .ToListAsync(cancellationToken);
        if (claimedToday.Contains(request.QuestKey))
        {
            return new ClaimQuestRewardResponse
            {
                QuestKey = request.QuestKey,
                Claimed = false,
                RewardTickets = 0,
                Lives = userLives.Lives,
            };
        }

        // Hedef ve ödül, listeyi üreten seçimden okunur: rövanş görevinin
        // hedefi kullanıcıya göre değişiyor. Bugünün görevi olmayan anahtar
        // (istemcide eski liste) ödül almaz.
        var day = await QuestRules.ReadDayStateAsync(_context, request.UserId, utcNow, cancellationToken);
        var definition = QuestRules.ChooseForDay(day, claimedToday)
            .FirstOrDefault(q => q.Key == request.QuestKey);
        var progress = definition is null
            ? 0
            : await QuestRules.GetProgressAsync(
                _context, request.UserId, request.QuestKey, utcNow, cancellationToken);
        if (definition is null || progress < definition.Target)
        {
            return new ClaimQuestRewardResponse
            {
                QuestKey = request.QuestKey,
                Claimed = false,
                RewardTickets = 0,
                Lives = userLives.Lives,
            };
        }

        // Bakiye tavandaysa ödülden hiçbir şey verilemez. Bu durumda talep
        // **tüketilmez**: görev alınabilir kalır ve kullanıcı yer açınca alır.
        //
        // Eskiden burada talep kaydedilip sıfır bilet veriliyordu; kullanıcı
        // düğmeye basıyor, "0 bilet" görüyor ve ödülü kalıcı olarak
        // kaybediyordu. Tavan kuralı korunuyor, kayıp ortadan kalkıyor.
        if (userLives.Lives >= userLives.MaxLives)
        {
            return new ClaimQuestRewardResponse
            {
                QuestKey = request.QuestKey,
                Claimed = false,
                RewardTickets = 0,
                Lives = userLives.Lives,
                MaxLives = userLives.MaxLives,
                CappedTickets = definition.RewardTickets,
                BlockedByCap = true,
            };
        }

        _context.UserQuestClaims.Add(new UserQuestClaim
        {
            UserId = request.UserId,
            QuestKey = request.QuestKey,
            DayKey = dayKey,
            ClaimedAt = utcNow,
        });

        // Tavan bakiyenin üst sınırıdır; yer varsa ödül tavana kadar verilir.
        var livesBeforeReward = userLives.Lives;
        userLives.Lives = Math.Min(userLives.Lives + definition.RewardTickets, userLives.MaxLives);
        var grantedTickets = userLives.Lives - livesBeforeReward;

        try
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            // Unique index yarışı: aynı gün aynı görev iki kez talep edildi;
            // ödülü ikinci kez verme.
            return new ClaimQuestRewardResponse
            {
                QuestKey = request.QuestKey,
                Claimed = false,
                RewardTickets = 0,
                Lives = livesBeforeReward,
            };
        }

        return new ClaimQuestRewardResponse
        {
            QuestKey = request.QuestKey,
            Claimed = true,
            RewardTickets = grantedTickets,
            Lives = userLives.Lives,
            MaxLives = userLives.MaxLives,
            CappedTickets = definition.RewardTickets - grantedTickets,
        };
    }
}
