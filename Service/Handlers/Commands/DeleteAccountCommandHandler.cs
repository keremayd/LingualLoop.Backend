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
using Service.Helpers;

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
///
/// Apple ile bağlı hesapta önce Apple'daki izin geri alınır (bkz.
/// <see cref="AppleTokenRevoker"/>). İstek Apple'ın yetki kodunu taşımıyorsa
/// silme yapılmaz, <see cref="ErrorCode.AppleConsentRequired"/> döner: uygulama
/// Apple penceresini açıp kodla yeniden ister.
/// </summary>
public class DeleteAccountCommandHandler : IRequestHandler<DeleteAccountRequest, DeleteAccountResponse>
{
    private readonly LingualLoopContext _context;
    private readonly IAwsService _awsService;
    private readonly AppleTokenRevoker _appleRevoker;
    private readonly ILogger<DeleteAccountCommandHandler> _logger;

    public DeleteAccountCommandHandler(
        ILingualLoopGenericRepository<User> userRepository,
        IAwsService awsService,
        AppleTokenRevoker appleRevoker,
        ILogger<DeleteAccountCommandHandler> logger)
    {
        _context = userRepository.GetDbContext();
        _awsService = awsService;
        _appleRevoker = appleRevoker;
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

        await RevokeAppleAsync(request, cancellationToken);

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

    /// <summary>
    /// Apple ile bağlı hesapta Apple'daki izni geri alır. Silme Apple'ın
    /// cevabına bağlı kalmaz: kullanıcı silmeyi onayladı; Apple reddederse ya
    /// da ulaşılamazsa hesap yine silinir, durum loga düşer.
    /// </summary>
    private async Task RevokeAppleAsync(DeleteAccountRequest request, CancellationToken cancellationToken)
    {
        var userId = request.UserId;
        var appleLinked = await _context.UserLogins.AnyAsync(
            login => login.UserId == userId &&
                     login.LoginProvider == AppleLoginCommandHandler.LoginProvider,
            cancellationToken);
        if (!appleLinked) return;

        if (!_appleRevoker.IsConfigured)
        {
            _logger.LogWarning(
                "Apple anahtarı yapılandırılmamış; Apple izni geri alınmadan siliniyor. userid = {UserId}",
                userId);
            return;
        }
        if (request.SkipAppleRevocation)
        {
            _logger.LogWarning(
                "İstemci Apple penceresini açamıyor; Apple izni geri alınmadan siliniyor. userid = {UserId}",
                userId);
            return;
        }
        if (string.IsNullOrWhiteSpace(request.AppleAuthorizationCode))
        {
            throw new LingualLoopException(ErrorCode.AppleConsentRequired.CreateMessage(userId),
                ErrorCode.AppleConsentRequired.GetDescription(userId), HttpStatusCode.PreconditionRequired);
        }

        try
        {
            await _appleRevoker.RevokeAsync(request.AppleAuthorizationCode, cancellationToken);
        }
        catch (Exception error) when (!cancellationToken.IsCancellationRequested)
        {
            _logger.LogError(error,
                "Apple izni geri alınamadı; hesap yine siliniyor. userid = {UserId}", userId);
        }
    }
}
