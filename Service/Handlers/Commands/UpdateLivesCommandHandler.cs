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

public class UpdateLivesCommandHandler : IRequestHandler<UpdateLivesRequest, UpdateLivesResponse>
{
    private readonly ILingualLoopGenericRepository<User> _userRepository;
    private readonly LingualLoopContext _context;

    public UpdateLivesCommandHandler(ILingualLoopGenericRepository<User> userRepository)
    {
        _userRepository = userRepository;
        _context = userRepository.GetDbContext();
    }

    public async Task<UpdateLivesResponse> Handle(UpdateLivesRequest request, CancellationToken cancellationToken)
    {
        var user = await _userRepository.FirstAsync(u => u.Id == request.UserId,
            user => new User() { Id = user.Id, UserNickname = user.UserNickname, UserScore = user.UserScore});
        if (user is null)
            throw new LingualLoopException(ErrorCode.NoDataInUsers.CreateMessage(request.UserId),
                ErrorCode.NoDataInUsers.GetDescription(request.UserId), HttpStatusCode.BadRequest);

        var utcNow = DateTime.UtcNow;
        var userLives = await UserLivesSeeder.GetOrCreateAsync(
            _context, request.UserId, utcNow, cancellationToken);

        // Npgsql, Kind'ı Unspecified olan bir değeri timestamptz kolonuna
        // yazmayı reddediyor; okunan değer her durumda UTC işaretlenir.
        userLives.LastLivesResetTime = DateTime.SpecifyKind(userLives.LastLivesResetTime, DateTimeKind.Utc);

        // Bilet yoksa harcama yapılmaz. Eskiden koşulsuz "Lives -= 1"
        // uygulanıyordu; bakiye eksiye düşüyor ve biletsiz oyun oynanabiliyordu.
        if (userLives.Lives <= 0)
        {
            return new UpdateLivesResponse()
            {
                UserId = request.UserId,
                Lives = userLives.Lives,
                MaxLives = userLives.MaxLives,
                Spent = false,
                NextTicketAt = userLives.LastLivesResetTime,
            };
        }

        var livesBeforeSpend = userLives.Lives;
        userLives.Lives -= 1;
        LivesRules.StartRegenTimerIfNeeded(userLives, livesBeforeSpend, utcNow);

        await _context.SaveChangesAsync(cancellationToken);

        return new UpdateLivesResponse()
        {
            UserId = request.UserId,
            Lives = userLives.Lives,
            MaxLives = userLives.MaxLives,
            Spent = true,
            NextTicketAt = userLives.Lives < userLives.MaxLives ? userLives.LastLivesResetTime : null,
        };
    }
}
