using MediatR;
using Microsoft.EntityFrameworkCore;
using Postgres;
using Postgres.Abstractions;
using Postgres.Models;
using Service.DataTransferObjects.Requests.Profile;
using Service.DataTransferObjects.Responses.Profile;
using Service.Helpers;

namespace Service.Handlers.Queries.Profile;

public class GetStreakByIdQueryHandler
    : IRequestHandler<GetStreakByIdRequest, GetStreakByIdResponse>
{
    private readonly LingualLoopContext _context;

    public GetStreakByIdQueryHandler(
        ILingualLoopGenericRepository<UserStreak> userStreakRepository)
    {
        _context = userStreakRepository.GetDbContext();
    }

    public async Task<GetStreakByIdResponse> Handle(
        GetStreakByIdRequest request,
        CancellationToken cancellationToken)
    {
        await UserDailyActivitySchemaHelper.EnsureCreatedAsync(_context, cancellationToken);

        var streak = await _context.UserStreaks
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.UserId == request.UserId, cancellationToken);

        var today = StreakRules.GetIstanbulDate(DateTime.UtcNow);

        // Giriş değil **oynama** aranıyor: satırın varlığı yalnızca uygulamanın
        // açıldığını söyler.
        var playedToday = await _context.UserDailyActivities
            .AsNoTracking()
            .AnyAsync(
                a => a.UserId == request.UserId &&
                     a.ActivityDate == today &&
                     a.Played,
                cancellationToken);

        return new GetStreakByIdResponse
        {
            CurrentStreak = streak?.CurrentStreak ?? 0,
            LongestStreak = streak?.LongestStreak ?? 0,
            FreezeCount = streak?.FreezeCount ?? 0,
            PlayedToday = playedToday,
            Week = await StreakWeekBuilder.BuildAsync(
                _context, request.UserId, today, streak, cancellationToken),
        };
    }
}
