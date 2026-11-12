using System.Data;
using System.Net;
using AwsService.Abstractions;
using Common.Enums;
using Common.Exceptions;
using Common.Extensions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Postgres;
using Postgres.Abstractions;
using Postgres.Models;
using Service.DataTransferObjects.Requests;
using Service.DataTransferObjects.Responses;

namespace Service.Handlers.Commands;

/// <summary>
/// Hesabı ve bütün verisini kalıcı olarak siler. App Store yalnız
/// pasifleştirmeyi yeterli saymıyor (5.1.1(v)); KVKK da silmeyi istiyor.
///
/// Kullanıcı iki tabloda duruyor: Identity'nin <c>AspNetUsers</c>'ı ve
/// eskiden kalan <c>users</c> (bir tetikleyici AspNetUsers'a eklenen her
/// satırı oraya kopyalıyor). Yeni tablolar AspNetUsers'a, eskiler users'a
/// bağlı. Eskilerin bir kısmının bağı silmeyi yaymıyor (NO ACTION); onlar
/// önce elle silinir, kalanlar iki satır silinince veritabanında kendiliğinden
/// gider. Hepsi tek işlemde: biri silinemezse hiçbiri silinmez.
/// </summary>
public class DeleteAccountCommandHandler : IRequestHandler<DeleteAccountRequest, DeleteAccountResponse>
{
    private readonly LingualLoopContext _context;
    private readonly IAwsService _awsService;
    private readonly ILogger<DeleteAccountCommandHandler> _logger;

    public DeleteAccountCommandHandler(
        ILingualLoopGenericRepository<User> userRepository,
        IAwsService awsService,
        ILogger<DeleteAccountCommandHandler> logger)
    {
        _context = userRepository.GetDbContext();
        _awsService = awsService;
        _logger = logger;
    }

    public async Task<DeleteAccountResponse> Handle(DeleteAccountRequest request, CancellationToken cancellationToken)
    {
        var userId = request.UserId;
        if (string.IsNullOrWhiteSpace(userId) ||
            !await _context.Users.AnyAsync(user => user.Id == userId, cancellationToken))
        {
            throw new LingualLoopException(ErrorCode.NoDataInUsers.CreateMessage(userId),
                ErrorCode.NoDataInUsers.GetDescription(userId), HttpStatusCode.NotFound);
        }

        await using (var transaction = await _context.Database.BeginTransactionAsync(
                         IsolationLevel.ReadCommitted,
                         cancellationToken))
        {
            var database = _context.Database;
            // users'a bağlı ama silmeyi yaymayan eski tablolar.
            await database.ExecuteSqlInterpolatedAsync(
                $"DELETE FROM user_karty_history WHERE user_id = {userId}", cancellationToken);
            await database.ExecuteSqlInterpolatedAsync(
                $"DELETE FROM user_lives WHERE user_id = {userId}", cancellationToken);
            await database.ExecuteSqlInterpolatedAsync(
                $"DELETE FROM user_scores WHERE user_id = {userId}", cancellationToken);
            await database.ExecuteSqlInterpolatedAsync(
                $"DELETE FROM user_video_history WHERE user_id = {userId}", cancellationToken);
            await database.ExecuteSqlInterpolatedAsync(
                $"DELETE FROM user_videos WHERE user_id = {userId}", cancellationToken);
            // users satırı rozetleri götürür; AspNetUsers satırı seriyi,
            // öğrenmeyi, ligi, görevleri, premiumu ve Google/Apple giriş
            // bağlantılarını.
            await database.ExecuteSqlInterpolatedAsync(
                $"DELETE FROM users WHERE user_id = {userId}", cancellationToken);
            await database.ExecuteSqlInterpolatedAsync(
                $"""DELETE FROM "AspNetUsers" WHERE "Id" = {userId}""", cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }

        // Dosya deposu veritabanı işlemine katılamıyor; fotoğraflar hesap
        // silindikten sonra kaldırılır. Olmazsa hesap yine silinmiş sayılır,
        // kalan dosya loga düşer.
        try
        {
            await _awsService.DeleteFolderAsync($"profile-photos/{userId}/");
        }
        catch (Exception error)
        {
            _logger.LogError(error,
                "Silinen hesabın profil fotoğrafları kaldırılamadı. userid = {UserId}", userId);
        }

        return new DeleteAccountResponse { Deleted = true };
    }
}
