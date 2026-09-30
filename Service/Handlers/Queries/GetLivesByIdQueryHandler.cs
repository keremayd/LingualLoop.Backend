using System.Net;
using Common.Enums;
using Common.Exceptions;
using Common.Extensions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Postgres;
using Postgres.Abstractions;
using Postgres.Models;
using Service.DataTransferObjects.Requests;
using Service.DataTransferObjects.Responses;
using Service.Helpers;

namespace Service.Handlers.Queries;

public class GetLivesByIdQueryHandler : IRequestHandler<GetLivesByIdRequest, GetLivesByIdResponse>
{
    private readonly ILingualLoopGenericRepository<UserLives> _userLivesRepository;
    private readonly LingualLoopContext _context;

    public GetLivesByIdQueryHandler(ILingualLoopGenericRepository<UserLives> userLivesRepository)
    {
        _userLivesRepository = userLivesRepository;
        _context = userLivesRepository.GetDbContext();
    }

    public async Task<GetLivesByIdResponse> Handle(GetLivesByIdRequest request, CancellationToken cancellationToken)
    {
        // Kullanıcı tam yenilenme anında ekranı açık tutuyor olabilir.
        // Beş dakikalık Hangfire turunu bekletmeden, zamanı gelmiş bileti
        // sorgunun parçası olarak gerçekten bakiyeye ekle.
        await LivesRegeneration.MaterializeForUserAsync(
            _context,
            request.UserId,
            DateTime.UtcNow,
            cancellationToken);

        var userLives = await _userLivesRepository.FirstAsync(u => u.UserId == request.UserId,
            user => new UserLives() { UserLivesId = user.UserLivesId, UserId = user.UserId, Lives = user.Lives, MaxLives = user.MaxLives, LastLivesResetTime = user.LastLivesResetTime});
        if (userLives is null)
            throw new LingualLoopException(ErrorCode.NoDataFoundInUserLives.CreateMessage(request.UserId),
                ErrorCode.NoDataFoundInUserLives.GetDescription(request.UserId), HttpStatusCode.BadRequest);

        return new GetLivesByIdResponse()
        {
            UserId = userLives.UserId,
            Lives = userLives.Lives,
            MaxLives = userLives.MaxLives,
            // Kolon `timestamp without time zone`, yani Npgsql
            // `Kind=Unspecified` döndürüyor ve System.Text.Json onu **`Z`
            // olmadan** yazıyor. Dart tarafı `Z`'siz metni yerel saat sayıyor,
            // widget ise UTC varsayıp fark alıyordu — fark hep negatif çıkıp
            // sıfıra kırpılıyor, bilet bitti penceresindeki geri sayım
            // kalıcı olarak "birazdan" gösteriyordu.
            LastLivesResetTime = DateTime.SpecifyKind(
                userLives.LastLivesResetTime,
                DateTimeKind.Utc)
        };
    }
}
