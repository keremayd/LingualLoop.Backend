using System.Net;
using Common.Enums;
using Common.Exceptions;
using Common.Extensions;
using MediatR;
using Postgres;
using Postgres.Abstractions;
using Postgres.Models;
using Service.DataTransferObjects.Requests;
using Service.DataTransferObjects.Responses;
using Service.Helpers;

namespace Service.Handlers.Commands;

public class UpdateScoreCommandHandler : IRequestHandler<UpdateScoreRequest, UpdateScoreResponse>
{
    private readonly ILingualLoopGenericRepository<User> _userRepository;
    private readonly ILingualLoopGenericRepository<UserScore> _userScoreRepository;
    private readonly LingualLoopContext _context;
    
    public UpdateScoreCommandHandler(ILingualLoopGenericRepository<User> userRepository, ILingualLoopGenericRepository<UserScore> userScoreRepository)
    {
        _userScoreRepository = userScoreRepository;
        _userRepository = userRepository;
        _context = userRepository.GetDbContext();
    }
    
    public async Task<UpdateScoreResponse> Handle(UpdateScoreRequest request, CancellationToken cancellationToken)
    {
        var user = await _userRepository.FirstAsync(u => u.Id == request.UserId,
            user => new User() { Id = user.Id, UserNickname = user.UserNickname, UserScore = user.UserScore});
        if (user is null)
            throw new LingualLoopException(ErrorCode.NoDataInUsers.CreateMessage(request.UserId),
                ErrorCode.NoDataInUsers.GetDescription(request.UserId), HttpStatusCode.BadRequest);

        await UserScoreSchemaHelper.EnsureCreatedAsync(_context, cancellationToken);

        var userScore = await _userScoreRepository.FirstAsync(u => u.UserId == request.UserId,
            response => new UserScore() { UserScoreId = response.UserScoreId, Score = response.Score, Experience = response.Experience, UserId = response.UserId });
        if (userScore == null)
            throw new LingualLoopException(ErrorCode.NoDataInUserScores.CreateMessage(request.UserId),
                ErrorCode.NoDataInUserScores.GetDescription(request.UserId), HttpStatusCode.BadRequest);

        var utcNow = DateTime.UtcNow;

        var isCorrect = request.Point > 0;
        var rewardPoints = isCorrect ? (request.BoostActive ? 3 : 1) : 0;

        // İlerleme sayacı: yalnızca kazanç yönünde işler; yanlış cevap
        // kullanıcının kazandığını geri almaz.
        //
        // `score` (eski zorluk termostatı) burada artık oynatılmıyor
        // (4 Kas 2026). Kart bandını seçen termostat, yeni kelimeler Karty
        // Yolu ünitelerinin sırasıyla gelmeye başlayınca anlamını yitirdi:
        // içerik öğrenmeyle açılıyor (`KartyUnitRules`), puanla değil.
        userScore.Experience += rewardPoints;

        await UserLeagueProgressSchemaHelper.EnsureCreatedAsync(_context, cancellationToken);

        var seasonKey = LeagueRules.GetCurrentSeasonKey(utcNow);
        var leagueProgress = await LeagueProgressSeeder.GetOrCreateAsync(
            _context,
            request.UserId,
            seasonKey,
            utcNow,
            cancellationToken);

        // Lig puanı da XP gibi tek yönlüdür: yanlış cevap kazanılmış
        // sıralamayı geri almaz. İki yönlü olduğunda kullanıcı aktif
        // oynarken lig sıralamasında gerileyebiliyordu — "oynadım ve
        // geriye gittim" en demoralize edici geri bildirim.
        //
        // Rastgele oynayıp puan biriktirme riski bilet ekonomisiyle sınırlanır:
        // giriş 1 bilet + her yanlış 1 bilet, tavan 15.
        //
        // Boost çarpanı **burada** anlamını buluyor: lig bir rekabet ölçüsü,
        // yani hızlanmanın karşılığının görüldüğü yer.
        if (rewardPoints > 0)
        {
            leagueProgress.Points += rewardPoints;
            leagueProgress.UpdatedAt = utcNow;
        }

        var becameConsolidated = false;
        if (isCorrect && request.KartyId.HasValue)
        {
            await KartyLearningSchemaHelper.EnsureCreatedAsync(_context, cancellationToken);
            var record = await KartyLearningRecorder.RecordAsync(
                _context,
                request.UserId,
                request.KartyId.Value,
                cancellationToken);
            becameConsolidated = record.BecameConsolidated;
        }

        // Karty'de verilen her cevap — doğru ya da yanlış — günü "oynandı"
        // yapar ve seriyi ilerletir. Seri artık açılışta değil burada artıyor.
        await StreakActivityRecorder.MarkPlayedAsync(
            _context, request.UserId, DateTime.UtcNow, cancellationToken);

        _userScoreRepository.Update(userScore);
        await _context.SaveChangesAsync(cancellationToken);

        // Kelime pekiştiyse bir ünite hedefe ulaşmış olabilir: açılma anı
        // kaydedilir (ünite bir daha kilitlenmez, patika bir kez kutlar).
        if (becameConsolidated)
        {
            await KartyUnitUnlockRecorder.CaptureAsync(_context, request.UserId, cancellationToken);
        }
        
        return new UpdateScoreResponse()
        {
            UserId = request.UserId,
            Score = userScore.Score,
            Experience = userScore.Experience,
            League = LeagueRules.BuildProgress(leagueProgress.Points, seasonKey)
        };
    }
}
