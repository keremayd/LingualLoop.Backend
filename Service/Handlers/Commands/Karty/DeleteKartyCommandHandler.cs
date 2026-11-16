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
using Service.DataTransferObjects.Requests.Karty;
using Service.DataTransferObjects.Responses.Karty;
using Service.Helpers;
using KartyModel = Postgres.Models.Karty;

namespace Service.Handlers.Commands.Karty;

/// <summary>
/// İçerik girişinin geri alma yolu: yanlış ya da hatalı üretilmiş kartı
/// satırı ve S3'teki görsel + telaffuzuyla kalıcı olarak siler
/// (<see cref="CreateKartyCommandHandler"/>'ın tersi). Yalnız Development'ta
/// açık; yayından önce kaldırılacak.
///
/// ## Kullanıcı kaydı olan kart
///
/// Kartı gören her kullanıcının <c>user_karty_learning</c> satırı (kutu,
/// tekrar vakti), cevap verdiyse <c>user_karty_history</c> satırları var.
/// Geçmişin bağı silmeyi yaymıyor; satırları silinmeden kart silinemiyor.
/// Yanlış eklenip hemen silinen kartta kayıt olmaz; kayıt varsa biri o kartla
/// oynamış demektir ve karar insana kalır. Bu yüzden böyle kart **varsayılan
/// olarak silinmez**: hata kaç kayıt olduğunu söyler, silmek için
/// <c>force</c> gerekir ve kayıtlar da silinir.
///
/// ## Sıra
///
/// Veritabanı tek işlemde silinir; S3 işleme katılamadığı için sonra. Ters
/// sırada S3 silinip veritabanı düşseydi görseli olmayan bir kart kalırdı. Bu
/// sırada en kötü durum S3'te sahipsiz dosya: anahtar kartın id'sine bağlı
/// ve id bir daha verilmiyor, yanıt uyarı taşıyor.
///
/// Ünite amblemi bu kartsa bağ veritabanında boşalır (ON DELETE SET NULL);
/// patika ünitenin ilk kartının görselini gösterir.
/// </summary>
public class DeleteKartyCommandHandler : IRequestHandler<DeleteKartyRequest, DeleteKartyResponse>
{
    private readonly LingualLoopContext _context;
    private readonly IAwsService _awsService;
    private readonly ILogger<DeleteKartyCommandHandler> _logger;

    public DeleteKartyCommandHandler(
        ILingualLoopGenericRepository<KartyModel> kartyRepository,
        IAwsService awsService,
        ILogger<DeleteKartyCommandHandler> logger)
    {
        _context = kartyRepository.GetDbContext();
        _awsService = awsService;
        _logger = logger;
    }

    public async Task<DeleteKartyResponse> Handle(
        DeleteKartyRequest request,
        CancellationToken cancellationToken)
    {
        await KartyUnitSchemaHelper.EnsureCreatedAsync(_context, cancellationToken);

        var kartyId = request.KartyId;
        var karty = await _context.Karty
            .AsNoTracking()
            .Where(item => item.KartyId == kartyId)
            .Select(item => new { item.NounText, item.Article, item.UnitId, item.UnitOrder })
            .FirstOrDefaultAsync(cancellationToken);
        if (karty is null) throw NotFound(kartyId);

        var learningCount = await _context.UserKartyLearnings
            .CountAsync(item => item.KartyId == kartyId, cancellationToken);
        var historyCount = await _context.UserKartyHistories
            .CountAsync(item => item.KartyId == kartyId, cancellationToken);
        if ((learningCount > 0 || historyCount > 0) && !request.Force)
        {
            throw new LingualLoopException(
                ErrorCode.KartyHasUserProgress.CreateMessage(kartyId, learningCount, historyCount),
                ErrorCode.KartyHasUserProgress.GetDescription(kartyId, learningCount, historyCount),
                HttpStatusCode.Conflict);
        }

        var response = new DeleteKartyResponse
        {
            KartyId = kartyId,
            NounText = karty.NounText,
            Article = karty.Article,
            UnitOrder = karty.UnitOrder,
            UnitSequence = karty.UnitId is int unitId
                ? await _context.KartyUnits
                    .Where(unit => unit.KartyUnitId == unitId)
                    .Select(unit => (int?)unit.Sequence)
                    .FirstOrDefaultAsync(cancellationToken)
                : null,
            ClearedEmblemUnits = await _context.KartyUnits
                .Where(unit => unit.EmblemKartyId == kartyId)
                .OrderBy(unit => unit.Sequence)
                .Select(unit => unit.Sequence)
                .ToListAsync(cancellationToken),
        };

        await using (var transaction = await _context.Database.BeginTransactionAsync(
                         IsolationLevel.ReadCommitted,
                         cancellationToken))
        {
            var database = _context.Database;
            // Kullanıcı kayıtlarına yalnız force ile dokunulur. Force yokken
            // sayım ile silme arasında biri kartı oynarsa geçmişin bağı silmeyi
            // durdurur, işlem geri alınır.
            if (request.Force)
            {
                response.DeletedHistoryCount = await database.ExecuteSqlInterpolatedAsync(
                    $"DELETE FROM user_karty_history WHERE karty_id = {kartyId}", cancellationToken);
                response.DeletedLearningCount = await database.ExecuteSqlInterpolatedAsync(
                    $"DELETE FROM user_karty_learning WHERE karty_id = {kartyId}", cancellationToken);
            }

            var deleted = await database.ExecuteSqlInterpolatedAsync(
                $"DELETE FROM karty WHERE karty_id = {kartyId}", cancellationToken);
            // Arada başka bir istek silmiş: işlem kaydedilmeden kapanır.
            if (deleted == 0) throw NotFound(kartyId);

            await transaction.CommitAsync(cancellationToken);
        }

        try
        {
            await _awsService.DeleteFolderAsync(
                $"{KartyAssetRules.FolderFor(kartyId)}/",
                BucketType.KartyAssets);
            response.AssetsDeleted = true;
        }
        catch (Exception error)
        {
            _logger.LogError(error,
                "Silinen kartın S3 varlıkları kaldırılamadı. kartyid = {KartyId}", kartyId);
            response.Warning =
                $"Kart silindi ama S3'teki görsel ve ses kaldırılamadı ({error.Message}). " +
                "Kart uygulamada artık görünmez; dosyalar sahipsiz kalır.";
        }

        return response;
    }

    private static LingualLoopException NotFound(int kartyId) =>
        new(ErrorCode.KartyNotFound.CreateMessage(kartyId),
            ErrorCode.KartyNotFound.GetDescription(kartyId),
            HttpStatusCode.NotFound);
}
