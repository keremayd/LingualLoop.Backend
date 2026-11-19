using System.Net;
using Common.Enums;
using Common.Exceptions;
using Common.Extensions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Postgres;
using Postgres.Abstractions;
using Postgres.Models;
using Service.DataTransferObjects.Requests.Karty;
using Service.DataTransferObjects.Responses.Karty;
using Service.Helpers;

namespace Service.Handlers.Commands.Karty;

/// <summary>
/// Karty Yolu ünitesini açar ya da adlarını (dile göre) ve başlık görselini
/// değiştirir.
/// Geçiş döneminde puan bantları geçici adlarla ünite oldu; içerik kararı
/// verilince adlar SQL'e inmeden buradan değişir.
/// </summary>
public class UpsertKartyUnitCommandHandler
    : IRequestHandler<UpsertKartyUnitRequest, UpsertKartyUnitResponse>
{
    private readonly LingualLoopContext _context;

    public UpsertKartyUnitCommandHandler(
        ILingualLoopGenericRepository<KartyUnit> unitRepository)
    {
        _context = unitRepository.GetDbContext();
    }

    public async Task<UpsertKartyUnitResponse> Handle(
        UpsertKartyUnitRequest request,
        CancellationToken cancellationToken)
    {
        await KartyUnitSchemaHelper.EnsureCreatedAsync(_context, cancellationToken);
        await KartyTranslationSchemaHelper.EnsureCreatedAsync(_context, cancellationToken);

        if (request.Sequence < 1) Fail("Ünite sırası 1'den başlar.");
        var titles = KartyTranslationWriter.Normalize(request.Titles);

        // Yeni ünite ile adları aynı işlemde: ad yazılamazsa adsız ünite kalmasın.
        await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);

        var unit = await _context.KartyUnits
            .FirstOrDefaultAsync(item => item.Sequence == request.Sequence, cancellationToken);
        var created = unit is null;
        if (unit is null)
        {
            if (!titles.Values.Any(title => title.Length > 0))
                Fail("Yeni ünite için en az bir dilde ad gerekli: {\"titles\": {\"tr\": \"Evde\"}}.");
            unit = new KartyUnit
            {
                Sequence = request.Sequence,
                CreatedDate = DateTime.UtcNow,
            };
            _context.KartyUnits.Add(unit);
        }

        if (request.EmblemKartyId is int emblemId)
        {
            var exists = await _context.Karty
                .AnyAsync(karty => karty.KartyId == emblemId, cancellationToken);
            if (!exists) Fail($"{emblemId} numaralı kart yok.");
            unit.EmblemKartyId = emblemId;
        }

        // Adlar ünitenin id'sine bağlı; yeni ünite önce kaydediliyor.
        await _context.SaveChangesAsync(cancellationToken);
        var allTitles = await KartyTranslationWriter.SetUnitTitlesAsync(
            _context, unit, titles, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return new UpsertKartyUnitResponse
        {
            Sequence = unit.Sequence,
            Titles = allTitles,
            EmblemKartyId = unit.EmblemKartyId,
            WordCount = await _context.Karty
                .CountAsync(karty => karty.UnitId == unit.KartyUnitId, cancellationToken),
            Created = created,
        };
    }

    private static void Fail(string problem) =>
        throw new LingualLoopException(
            ErrorCode.KartyPayloadInvalid.CreateMessage(problem),
            ErrorCode.KartyPayloadInvalid.GetDescription(problem),
            HttpStatusCode.BadRequest);
}
