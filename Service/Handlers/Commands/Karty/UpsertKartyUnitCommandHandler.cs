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
/// Karty Yolu ünitesini açar ya da adını ve başlık görselini değiştirir.
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

        var title = request.Title?.Trim();
        if (request.Sequence < 1) Fail("Ünite sırası 1'den başlar.");

        var unit = await _context.KartyUnits
            .FirstOrDefaultAsync(item => item.Sequence == request.Sequence, cancellationToken);
        var created = unit is null;
        if (unit is null)
        {
            if (string.IsNullOrEmpty(title)) Fail("Yeni ünite için başlık gerekli.");
            unit = new KartyUnit
            {
                Sequence = request.Sequence,
                Title = title!,
                CreatedDate = DateTime.UtcNow,
            };
            _context.KartyUnits.Add(unit);
        }
        else if (!string.IsNullOrEmpty(title))
        {
            unit.Title = title;
        }

        if (request.EmblemKartyId is int emblemId)
        {
            var exists = await _context.Karty
                .AnyAsync(karty => karty.KartyId == emblemId, cancellationToken);
            if (!exists) Fail($"{emblemId} numaralı kart yok.");
            unit.EmblemKartyId = emblemId;
        }

        await _context.SaveChangesAsync(cancellationToken);

        return new UpsertKartyUnitResponse
        {
            Sequence = unit.Sequence,
            Title = unit.Title,
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
